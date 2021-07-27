#!/bin/bash
# 
# Run Physics 

#KNOWN issues

set -euo pipefail

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

function usage() {
    local usage_info="Arguments:
    -i <ip_address>
        IP address to listen on for.
    -p <port_number>
        Port to listen on.
    -v
        Activate verbose logging."
    printf "%s\n" "${usage_info}"
}

function ctrl_c_intercepted() {
    kill -SIGKILL -$$
    exit 1
}

trap ctrl_c_intercepted SIGINT

#################################################
# Arguments
#################################################

VERBOSE='false'
port=''
interface=''
renderer=''
render_path=''

while getopts :b:i:p:r:vh flag; do
    case "$flag" in
        i) interface="${OPTARG}" ;;
        p) port="${OPTARG}";;
        b) renderer="${OPTARG}" ;;
        r) render_path="${OPTARG}";;
        v)  
            VERBOSE='true' 
            set -x
            ;;
        h)
            usage
            exit 0
            ;;
        \?) 
            usage
            err "Invalid option: ${OPTARG}." ;;
        : ) 
            usage
            err "Invalid option: ${OPTARG} requires an argument." ;;
    esac
done

readonly VERBOSE
readonly port
readonly interface

#################################################
# Validate arguments
#################################################
if [[ -z $renderer ]]; then
    err "Missing path to executable. Ensure that this is the path to the built unity executable"
elif [[ ! -f $renderer ]] || [[ ! -x $renderer ]]; then
    err "Cannot run executable at ${renderer}. Ensure path is valid and you\
have the correct permissions."
fi

# validate port
if [ -n "$port" ] && { [[ ! $port == +([0-9]) ]] || [[ $port -lt 1024 ]] \
    || [[ $port -gt 65535 ]]; }; then
	err "Invalid port value (${port}) provided. Ensure that it is an \
integer between 1024 and 65535."
fi

# validate render path
if [ -n "$render_path" ]; then
    if ! readlink -m "${render_path}" > /dev/null; then
        err "Invalid render path \"${render_path}\""

    # Try to create the directory if it does not exist
    elif ! readlink -e "${render_path}" > /dev/null \
        && ! mkdir -p "${render_path}"; then
        err "Could not create render output directory."

    # Check permissions for path
    elif [[ ! -r $render_path ]] || [[ ! -w $render_path ]]; then
        err "Missing permissions for render output directory. \
    Please ensure that you have the required permissions for this directory."

    # Resolve the path
    else
        render_path=$(readlink -e "${render_path}")
    fi
fi

#################################################
# Unparse args and run
#################################################

run_renderer=("${renderer}" "-batchmode")
run_renderer+=("-logFile")
if [[ $VERBOSE != 'true' ]]; then
    run_renderer+=("\"./renderer.log\"")
else
    run_renderer+=("/dev/stdout")
fi
run_renderer+=("renderer")

if [ -n "$port" ]; then
    run_renderer+=('--port' "${port}")
fi

if [ -n "$interface" ]; then
    run_renderer+=('--interface' \""${interface}\"")
fi

if [ -n "$render_path" ]; then
    run_renderer+=('--renderPath' \""${render_path}\"")
fi

log verbose "Renderer arguments:" "${run_renderer[@]}"

DISPLAY=:0 eval "${run_renderer[@]}" &
renderer_pid=$!

while [[ -d "/proc/${renderer_pid}" ]]; do
    sleep 0.25
done

if wait "${renderer_pid}"; then
    log normal "Renderer has completed successfully."
else
    err "Renderer failed to complete successfully"
fi