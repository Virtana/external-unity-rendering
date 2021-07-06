#!/bin/bash

# get arguments
while getopts i:e:d:h:w:o:r:t flag
do
	case "$flag" in
		i) projectFolder=${OPTARG};;
		e) exportCount=${OPTARG};;
		d) delayms=${OPTARG};;
		h) renderHeight=${OPTARG};;
		w) renderWidth=${OPTARG};;
		o) jsonFolder=${OPTARG};;
		r) renderPath=${OPTARG};;
		t) transmit="-transmit";;
	esac
done

# validate all folders
if [ ! -d "$projectFolder" ]; then
	echo "Not a project folder > $projectFolder" 
	exit 1
fi

if [ ! -d "$jsonFolder" ]; then
	echo "Not a json export folder > $jsonFolder" 
	exit 1
fi

if [ ! -d "$renderPath" ]; then
	echo "Not a render folder > $renderPath" 
	exit 1
fi

# clear src
#rm -r ./src/*
cp -r "$projectFolder/Assets" ./src
cp -r "$projectFolder/ProjectSettings" ./src
cp -r "$projectFolder/Packages" ./src

# build physics
~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath ./src -executeMethod BuildScript.Build -physics

# quit if fail
if [[ $? -ne 0 ]]; then
	echo -e '\033[41mPHYSICS: Failed to build successfully.\033[40m'
	exit $?
fi

# build renderer
~/Unity/Hub/Editor/2020.3.11f1/Editor/Unity -quit -batchmode -nographics -projectPath ./src -executeMethod BuildScript.Build -renderer

# quit if fail
if [[ $? -ne 0 ]]; then
	echo -e '\033[41mRENDERER: Failed to build successfully\033[40m'
	exit $?
fi

# run physics
~/Virtana/builds/Physics/Physics -batchmode -export $exportCount -delay $delayms -nographics -writeToFile ./Physics-States -logExport -r ./Renders -h $renderHeight -w $renderWidth $transmit
