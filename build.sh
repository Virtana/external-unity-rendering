#!/bin/bash
#
# Simplify building of Renderer and Physics instances, providing relevant
# arguments.

#################################################
# Variables
#################################################

unity=''
project_path=''
output_path=''
build_options=''
BUILD_WINDOWS='false'
VERBOSE='false'

#################################################
# Helper functions
#################################################

function err() {
    printf "\e[41;37m[%s]: %s\e[0m\n" "$(date +'%Y-%m-%dT%H:%M:%S%z')" "$*" >&2
    exit 1
}

function log() {
    local level="${1}"
    shift
    if [[ $level == 'verbose' ]] && [[ $VERBOSE != 'true' ]]; then
        return
    fi
    if [[ $level == 'warning' ]]; then
        printf "\e[43;30m"
    fi
    printf "[%s]: %s" "$(date +'%Y-%m-%dT%H:%M:%S%z')" "$*"
    if [[ $level == 'warning' ]]; then
        printf "\e[0m"
    fi
    printf "\n"
}

function cleanup() {
    if [[ -e $tmp_dir ]]; then
        rm -rf "${tmp_dir}"
    fi
}

# register the cleanup function to be called on the EXIT signal
trap cleanup EXIT

#################################################
# Getting and validating args
#################################################
while getopts :p:o:b:u:wv flag; do
    case "$flag" in
        p)
            project_path="${OPTARG}"
            ;;
        o)
            output_path="${OPTARG}"
            ;;
        b)
            build_options="${OPTARG}"
            ;;
        w)
            BUILD_WINDOWS='true'
            ;;
        v)
            VERBOSE='true'
            ;;
        u)
            unity="${OPTARG}"
            ;;
        \?)
            err "Invalid option: ${OPTARG}."
            ;;
        : )
            err "Invalid option: ${OPTARG} requires an argument."
            ;;
    esac
done

readonly VERBOSE
readonly BUILD_WINDOWS

if [[ -z $project_path ]]; then
    err "Missing Project Path."
elif [[ -z $output_path ]]; then
    err "Missing Build Output Path."
elif [[ -z $unity ]]; then
    err "Missing Unity Editor Path."
fi

# Validate that project folder exists
if ! readlink -e "${project_path}" > /dev/null; then
    err "Failed to validate project path \"${project_path}\"."

# Validate that Assets, ProjectSettings and Packages subfolders exist
elif ! readlink -e "${project_path}/Assets" > /dev/null \
    && ! readlink -e "${project_path}/ProjectSettings" > /dev/null \
    && ! readlink -e "${project_path}/Packages" > /dev/null; then
    err "Missing project folders in \"${project_path}\". Ensure that this is \
a Unity project."

# Ensure Read permissions exist
elif [[ ! -r $project_path ]]; then
    err "Cannot read from project path."

# If a lockfile is present or the project directory is not writable, then 
# use a temp path instead.
elif [[ -e "${project_path}/Temp/UnityLockfile" ]] \
    || [[ ! -w $project_path ]]; then
    log warning "Missing write permissions for project folder. A temporary \
folder will be used instead."
    tmp_dir=$(mktemp -d -t Unity-EUR-XXXXXXXX)
    cp -r "${project_path}/Assets" "${tmp_dir}"
	cp -r "${project_path}/ProjectSettings" "${tmp_dir}"
	cp -r "${project_path}/Packages" "${tmp_dir}"
    project_path=$tmp_dir

# Otherwise just use the path directly
else
    project_path=$(readlink -e "${project_path}")
fi

# Validate output path
if ! readlink -m "${output_path}" > /dev/null; then
    err "Invalid project path \"${output_path}\""

# Try to create the directory if it does not exist
elif ! readlink -e "${output_path}" > /dev/null \
    && ! mkdir -p "${output_path}"; then
    err "Could not create build output directory."

# Check permissions for path
elif [[ ! -r $output_path ]] || [[ ! -w $output_path ]] \
    || [[ ! -x $output_path ]]; then
    err "Missing permissions for build output directory. Please ensure that \
you have the required permissions for this directory."

# Resolve the path
else
    output_path=$(readlink -e "${output_path}")
fi

# Validate unity path
if ! readlink -e "${unity}" > /dev/null; then
    err "Path to Unity Editor is invalid. Ensure that ${unity} is the path to \
the Unity Editor executable."
fi

# Resolve the path
unity="$(readlink -e "${unity}")"

# Ensure the editor is executable
if [[ ! -x $unity ]]; then
    err "Cannot execute unity editor. Ensure that you have the appropriate \
permissions."

# Check version and ensure it is valid
elif [[ $($unity -version) != '2020.3.11f1' ]] \
    && [[ $($unity -version) != '2020.1.8f1' ]]; then
    log warning "Unity version is $("$unity" -version), which does not match \
the project version 2020.1.8f1. Are you sure you want to continue?"
    printf "(y/N) >"
    yn='N'
    while true; do
        read -r yn
        case $yn in
            [Yy]* )
                log verbose "Using Unity version $($unity --version)."
                break
                ;;
            [Nn]* )
                log warning "Exiting without building."
                exit 1
                ;;
            * ) 
                log warning "Please enter yes or no."
                printf "(y/N) >"
                ;;
        esac
    done
fi

# logging args
log verbose "Project Path: ${project_path}"
log verbose "Build Path: ${output_path}"
log verbose "Build Options: ${build_options}"
log verbose "Build Windows: ${BUILD_WINDOWS}"
log verbose "Verbose: ${VERBOSE}"

args="-quit -batchmode -nographics -projectPath \"${project_path}\" \
-executeMethod BuildScript.Build --build \"${output_path}\""
if [[ $BUILD_WINDOWS == 'true' ]]; then
    args="${args} --buildTarget \"StandaloneWindows64\""
fi


physics_args="--config Physics"
renderer_args="--config Renderer"

if [[ $VERBOSE == 'true' ]]; then
    physics_args="${physics_args} -logFile"
    renderer_args="${renderer_args} -logFile"
else
    physics_args="${physics_args} -logFile \"./physics_build.log\""
    renderer_args="${renderer_args} -logFile \"./renderer_build.log\""
fi

log verbose "Arguments for Physics instance:" "${physics_args}"
log verbose "Arguments for Renderer instance:" "${renderer_args}"

physics_build=("${unity}" "${args}" "${physics_args}")
renderer_build=("${unity}" "${args}" "${renderer_args}")

# Running 
log normal "Starting Physics Build"
if ! eval "${physics_build[@]}"; then
    err "Failed to build Physics instance successfully."
fi

log normal "Starting Renderer Build"
if ! eval "${renderer_build[@]}"; then
    err "Failed to build Renderer instance successfully."
fi

log normal "Completed builds successfully to ${output_path}/."