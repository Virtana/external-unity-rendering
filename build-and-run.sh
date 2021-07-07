#!/bin/bash

printfn() {
	printf "$1\n"
}

# from https://stackoverflow.com/a/28333938
trap_with_arg() {
	local func="$1"
	shift
	for sig in "$@"; do
		trap "$func $sig" "$sig"
	done
}

stop() {
	trap - SIGINT EXIT
	printfn '\n%s\n' "received $1, killing child processes"
	kill -s SIGINT 0
}

trap_with_arg 'stop' EXIT SIGINT SIGTERM SIGHUP


# get arguments
while getopts i:e:d:h:w:o:r:s:O:tlb flag; do
	case "$flag" in
	i) projectFolder=${OPTARG} ;;
	e) exportCount=${OPTARG} ;;
	d) delayms=${OPTARG} ;;
	s) totalTime=${OPTARG} ;;
	h) renderHeight=${OPTARG} ;;
	w) renderWidth=${OPTARG} ;;
	r) renderPath=${OPTARG} ;;
	o) jsonFolder=${OPTARG} ;;
	t) transmit="--transmit" ;;
	l) logJson="--logExport" ;;
	b) build=true ;;
	O) buildDir=${OPTARG} ;;
	\?)
		printfn "Invalid arg ${OPTARG}" >&2
		exit 1
		;;
	esac
done

# validate project folder
if [ ! -d "$projectFolder" ]; then
	printfn "Invalid render folder provided.\n > $projectFolder" >&2
	exit 1
fi

# check if output folder provided and if it is, validate it
if [ -n "$jsonFolder" ] && [ ! -d "$jsonFolder" ]; then
	printfn "Invalid render folder provided.\n > $jsonFolder" >&2
	exit 1
elif [ -n "$jsonFolder" ]; then
	# prepend argument switch
	jsonFolder="--writeToFile $jsonFolder"
fi

# validate renderpath
if [ -n "$renderPath" ] && [ ! -d "$renderPath" ]; then
	printfn "Invalid render folder provided.\n > $renderPath" >&2
	exit 1
fi
renderPath="-r $renderPath"

# validate renderheight
if [ -z "$renderHeight" ] && [[ $renderHeight == +([0-9]) ]]; then
	renderHeight=720
fi
renderHeight="-h $renderHeight"

# validate renderwidth
if [ -z "$renderWidth" ] || [[ ! $renderWidth == +([0-9]) ]]; then
	renderHeight=1280
fi
renderWidth="-w $renderWidth"

# validate delay
if [ -n "$delayms" ] || [[ $delayms == +([0-9])@(s|m|) ]]; then
	delayms="--delay $delayms"
else
	delayms=
fi

# validate export count
if [ -n "$exportCount" ] && [[ $exportCount == +([0-9]) ]]; then
	exportCount="--export $exportCount"
else
	exportCount=
fi

# val1idate totalTime
if [ -n "$totalTime" ] && [[ $totalTime == +([0-9])@(s|m|) ]]; then
	totalTime="--time $totalTime"
else
	totalTime=
fi

printfn "Arguments:
\t projectFolder=\e[1;4;35m${projectFolder}\e[0m
\t exportCount=\e[1;4;35m${exportCount}\e[0m
\t delayms=\e[1;4;35m${delayms}\e[0m
\t totalTime=\e[1;4;35m${totalTime}\e[0m
\t renderHeight=\e[1;4;35m${renderHeight}\e[0m
\t renderWidth=\e[1;4;35m${renderWidth}\e[0m
\t renderPath=\e[1;4;35m${renderPath}\e[0m
\t jsonFolder=\e[1;4;35m${jsonFolder}\e[0m
\t transmit=\e[1;4;35m${transmit}\e[0m
\t logJson=\e[1;4;35m${logJson}\e[0m
\t buildDir=\e[1;4;35m${buildDir}\e[0m
"

# clear src
#rm -r ./src/*
cp -r "$projectFolder/Assets" ./src
cp -r "$projectFolder/ProjectSettings" ./src
cp -r "$projectFolder/Packages" ./src

if [ "$build" = true ]; then
	# build physics
	~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath ./src -executeMethod BuildScript.Build -physics

	# quit if fail
	if [[ $? -ne 0 ]]; then
		printfn '\e[41;1;37mPHYSICS: Failed to build successfully.\e[0m' >&2
		exit $?
	fi

	# build renderer
	~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath ./src -executeMethod BuildScript.Build -renderer

	# quit if fail
	if [[ $? -ne 0 ]]; then
		printfn '\e[41;1;37mRENDERER: Failed to build successfully.\e[0m' >&2
		exit $?
	fi
fi

if [ -n "$transmit" ]; then
	DISPLAY=:0 nohup ~/Virtana/builds/Renderer/Renderer -batchmode -logFile /dev/stdout &
	renderer_pid=$!
	while true; do tail -f nohup.out | xargs -I {} printf "\e[30;46m{}\e[0m\n"; done &
fi

# VERY BIG HACK TO ENSURE PHYSICS LAUNCHES AFTER RENDERER IS READY
# option, try pinging the socket?
sleep 1

# run physics
~/Virtana/builds/Physics/Physics \
	-batchmode -nographics \
	$jsonFolder $renderPath \
	$renderHeight $renderWidth \
	$transmit $logJson \
	$exportCount $delayms $totalTime

if [ -n "$transmit" ] && [[ $? -ne 0 ]]; then
	kill -EXIT $renderer_pid
fi

wait $renderer_pid
