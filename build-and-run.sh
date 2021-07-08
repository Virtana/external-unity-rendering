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

# maybe take out SIGHUP and make it do nothing
trap_with_arg 'stop' EXIT SIGINT SIGTERM SIGHUP


# get arguments
while getopts i:e:d:h:w:o:r:s:B:m:tlb flag; do
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
	B) buildDir=${OPTARG} ;;
	m) copyProject=${OPTARG} ;;
	\?)
		printfn "Invalid arg ${OPTARG}" >&2
		exit 1
		;;
	esac
done

# validate project folder
if [ ! -d "$projectFolder" ] && [ ! -r "$projectFolder" ]; then
	printfn "Invalid render folder provided.\n > $projectFolder" >&2
	exit 1
fi
projectFolder=$(readlink -f "$projectFolder")

# check if output folder provided and if it is, validate it
if [ -n "$jsonFolder" ] && [ ! -d "$jsonFolder" ] \
	&& [ ! -w "$jsonFolder" ] && [ ! -r "$jsonFolder" ]; then
	printfn "Invalid render folder provided.\n > $jsonFolder" >&2
	exit 1
elif [ -n "$jsonFolder" ]; then
	# prepend argument switch
	jsonFolder="--writeToFile $(readlink -f "$jsonFolder")"
fi

# validate renderpath
if [ -n "$renderPath" ] && [ ! -d "$renderPath" ] \
	&& [ ! -w "$renderPath" ] && [ ! -r "$renderPath" ]; then
	printfn "Invalid render folder provided.\n > $renderPath" >&2
	exit 1
fi
renderPath="-r $(readlink -f "$renderPath")"

# validate buildDir
if [ -n "$buildDir" ] && [ ! -d "$buildDir" ] \
	&& [ ! -w "$buildDir" ] && [ ! -r "$buildDir" ]; then
	printfn "Invalid build folder provided.\n > $buildDir" >&2
	exit 1
fi
buildDir=$(readlink -f "$buildDir")

# validate copyProjectFolder
if [ -n "$copyProject" ] && [ ! -d "$copyProject" ] \
	&& [ ! -w "$copyProject" ] && [ ! -r "$copyProject" ]; then

	printfn "Invalid copy project folder provided.\n > $copyProject" >&2
	exit 1
fi
copyProject=$(readlink -f "$copyProject")

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
\t copyProjectToLocation=\e[1;4;35m${copyProject}\e[0m
"

if [ -n "$copyProject" ]; then
	# clear src
	rm -r ./src/*
	cp -r "$projectFolder/Assets" "$copyProject"
	cp -r "$projectFolder/ProjectSettings" "$copyProject"
	cp -r "$projectFolder/Packages" "$copyProject"
else
	copyProject=$projectFolder
fi

if [ "$build" = true ]; then
	# build physics
	~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath "$copyProject" -executeMethod BuildScript.Build --physics --build "$buildDir"

	# quit if fail
	if [[ $? -ne 0 ]]; then
		printfn '\e[41;1;37mPHYSICS: Failed to build successfully.\e[0m' >&2
		exit $?
	fi

	# build renderer
	~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath "$copyProject" -executeMethod BuildScript.Build --renderer --build "$buildDir"

	# quit if fail
	if [[ $? -ne 0 ]]; then
		printfn '\e[41;1;37mRENDERER: Failed to build successfully.\e[0m' >&2
		exit $?
	fi
fi

rendererPath="$buildDir/Renderer/Renderer"
physicsPath="$buildDir/Physics/Physics"

if [ -n "$transmit" ]; then
	DISPLAY=:0 exec "$rendererPath" -batchmode -logFile /dev/stdout &
	renderer_pid=$!
fi

# VERY BIG HACK TO ENSURE PHYSICS LAUNCHES AFTER RENDERER IS READY
# option, try pinging the socket?
#sleep 0.5

# run physics
printf "%s" "$physicsPath" | xargs -I {} bash -c "{} \
	-batchmode -nographics \
	$jsonFolder $renderPath \
	$renderHeight $renderWidth \
	$transmit $logJson \
	$exportCount $delayms $totalTime"

if [ -n "$transmit" ] && [[ $? -ne 0 ]]; then
	kill -EXIT $renderer_pid
fi

wait
