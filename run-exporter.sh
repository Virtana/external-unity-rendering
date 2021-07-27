#!/bin/bash
# 
# Run Physics 

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
    local usage_info=" Arguments:
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
# Variables
#################################################

VERBOSE='false'
exporter=''
batch_mode='false'
render_path=''
render_height=''
render_width=''
transmit='false'
log_json='false'
json_path=''
export_count=''
export_delay=''
total_export_time=''
port=''
interface=''

#################################################
# Collecting args
#################################################

while getopts :e:r:h:w:j:c:d:s:p:i:btlv flag; do
    case "$flag" in
        e) exporter="${OPTARG}" ;;
        b) batch_mode='true' ;;
        r) render_path="${OPTARG}" ;;
        h) render_height="${OPTARG}" ;;
        w) render_width="${OPTARG}" ;;
        t) transmit='true' ;;
        l) log_json='true' ;;
        j) json_path="${OPTARG}" ;;
        c) export_count="${OPTARG}" ;;
        d) export_delay="${OPTARG}" ;;
        s) total_export_time="${OPTARG}" ;;
        p) port="${OPTARG}" ;;
        i) interface="${OPTARG}" ;;
        v)  
            VERBOSE='true'
            set -x
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
readonly batch_mode
readonly render_height
readonly render_width
readonly transmit
readonly log_json
readonly export_count
readonly export_delay
readonly total_export_time
readonly port
readonly interface

#################################################
# Vadating Args
#################################################
if [[ -z $exporter ]]; then
    err "Missing executable path. Must include the path to the folder containing \
the built executable."
fi

# validate that the exporter path exists
if ! readlink -e "${exporter}" > /dev/null; then
    err "Missing executable file. Ensure that ${exporter} \
is the path to the built executable."
elif [[ ! -r $exporter ]] || [[ ! -w $exporter ]] || \
    [[ ! -x $exporter ]]; then
    err "Missing permissions for executable. Ensure that the file is executable."
else 
    exporter=$(readlink -e "${exporter}")
fi

# validate renderheight
if [ -n "$render_height" ] && [[ ! $render_height == +([0-9]) ]]; then
	err "Non-integer render height (${render_height}) provided."
fi

# validate renderwidth
if [ -n "$render_width" ] && [[ ! $render_width == +([0-9]) ]]; then
	err "Non-integer render width (${render_width}) provided."
fi

# validate export count
if [ -n "$export_count" ] && [[ $export_count != +([0-9]) ]]; then
	err "Invalid export count (${export_count}) provided. Provide an integer \
above 0."
fi

# validate delay
if [ -n "$export_delay" ] && [[ $export_delay != +([0-9])@(s|m|) ]]; then
	err "Invalid export delay (${export_delay}) provided. Provide an integer \
with the optional modifiers s(for seconds) or m(for minutes). No modifier \
represents milliseconds. Minimum delay is 10ms."
fi

# val1idate totalTime
if [ -n "$total_export_time" ] \
    && [[ $total_export_time != +([0-9])@(s|m|) ]]; then
	err "Invalid total export time (${total_export_time}) provided. Provide \
an integer with the optional modifiers s(for seconds) or m(for minutes). No \
modifier represents milliseconds. Minimum delay is 10ms."
fi

# validate port
if [ -n "$port" ] && { [[ ! $port == +([0-9]) ]] || [[ $port -lt 1024 ]] \
    || [[ $port -gt 65535 ]]; }; then
	err "Invalid port value (${port}) provided. Ensure that it is an \
integer between 1024 and 65535."
fi

#################################################
# Unparse args and run
#################################################

run_exporter=("${exporter}")
if [[ $batch_mode == 'true' ]]; then
    run_exporter+=('-batchmode' "-nographics")
fi

if [[ $VERBOSE == 'true' ]]; then
    run_exporter+=("-logFile" "/dev/stdout")
else
    run_exporter+=("-logFile" "./exporter.log")
fi

run_exporter+=("export")

if [[ -n $render_path ]]; then
    run_exporter+=("--renderPath" "${render_path}")
fi

if [[ -n "$render_height" ]]; then
    run_exporter+=("-h ${render_height}")
fi

if [[ -n "$render_width" ]]; then
    run_exporter+=("-w ${render_width}")
fi

if [[ $transmit == 'true' ]]; then
    run_exporter+=("--transmit")
fi

if [[ $log_json == 'true' ]]; then
    run_exporter+=("--logExport")
fi

if [[ -n "$json_path" ]]; then
    run_exporter+=("--writeToFile ${json_path}")
fi

if [[ -n "$export_count" ]]; then
    run_exporter+=("--exportCount ${export_count}")
fi

if [[ -n "$export_delay" ]]; then
    run_exporter+=("--delay ${export_delay}")
fi

if [[ -n "$total_export_time" ]]; then
    run_exporter+=("--totalTime ${total_export_time}")
fi

if [ -n "$port" ]; then
    run_exporter+=('--port' "${port}")
fi

if [ -n "$interface" ]; then
    run_exporter+=('--interface' \""${interface}\"")
fi

log verbose "Exporter arguments:" "${run_exporter[@]}"

eval "${run_exporter[@]}" &
exporter_pid=$!

while [[ -d "/proc/${exporter_pid}" ]]; do
    sleep 0.25
done

if wait "${exporter_pid}"; then
    log normal "Renderer has completed successfully."
else
    err "Renderer failed to complete successfully"
fi