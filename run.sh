#!/bin/bash
# 
# Run Physics 

set -euxo pipefail

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
t)
    Whether to launch renderer and transmit scene states from physics 
    instance.
l)
    Whether the physics instance should print the serialized state to the 
    console/log
b) <directory>
    Where the built executables are. Should have subfolders named Physics
    Renderer holding the respective executables. This should be the same 
    directory specified to the build script.
j) <directory>
    The path to where the the serialized json should be exported to. If not 
    specified, no files are saved.
e) <integer>
    The number of exports to make. 
d) <time = integer(s|m|)>
    The delay between exports.
s) <time = integer(s|m|)>
    The total amount of time to export for. Equal to delay * export count.
r) <directory>
    The path to where renders are to be made. Required if the transmit option
    is set.
h) <integer>
    The pixel height of the renders. Minimum of 300. Extremely large values
    can cause an out of VRAM issue.
w) <integer>
    The pixel width of the renders. Minimum of 300. Extremely large values
    can cause an out of VRAM issue.
v)
    Activate verbose logging."
    printf "%s\n" "${usage_info}"
}

function cleanup() {
    true
}

# register the cleanup function to be called on the EXIT signal
trap cleanup EXIT

#################################################
# Variables
#################################################

VERBOSE='false'
build_path=''
transmit='false'
log_json='false'
json_path=''
render_path=''
render_height=''
render_width=''
export_count=''
export_delay=''
total_export_time=''

#################################################
# Collecting args
# t)
#     Whether to launch renderer and transmit scene states from physics 
#     instance.
# l)
#     Whether the physics instance should print the serialized state to the 
#     console/log
# b) <directory>
#     Where the built executables are. Should have subfolders named Physics
#     Renderer holding the respective executables. This should be the same 
#     directory specified to the build script.
# j) <directory>
#     The path to where the the serialized json should be exported to. If not 
#     specified, no files are saved.
# e) <integer>
#     The number of exports to make. 
# d) <time = integer(s|m|)>
#     The delay between exports.
# s) <time = integer(s|m|)>
#     The total amount of time to export for. Equal to delay * export count.
# r) <directory>
#     The path to where renders are to be made. Required if the transmit option
#     is set.
# h) <integer>
#     The pixel height of the renders. Minimum of 300. Extremely large values
#     can cause an out of VRAM issue.
# w) <integer>
#     The pixel width of the renders. Minimum of 300. Extremely large values
#     can cause an out of VRAM issue.
# v)
#     Activate verbose logging.
#################################################

while getopts :b:j:e:d:s:r:h:w:tlv flag; do
    case "$flag" in
        t)  transmit='true' ;;
        l)  log_json='true' ;;
        b)  build_path=${OPTARG} ;;
        j)  json_path=${OPTARG} ;;
        e)  export_count=${OPTARG} ;;
        d)  export_delay=${OPTARG} ;;
        s)  total_export_time=${OPTARG} ;;
        r)  render_path=${OPTARG} ;;
        h)  render_height=${OPTARG} ;;
        w)  render_width=${OPTARG} ;;
        v)  VERBOSE='true' ;;
        \?) 
            usage
            err "Invalid option: ${OPTARG}." ;;
        : ) 
            usage
            err "Invalid option: ${OPTARG} requires an argument." ;;
    esac
done

readonly VERBOSE

#################################################
# Checking Missing Args
#################################################
if [[ -z $build_path ]]; then
    err "Missing build path. Must include the path to the folder containing \
the built Physics and Renderer instances."
elif [[ $transmit == 'true' ]] && [[ -z $render_path ]]; then
    err "Missing render path. If -t is set, render directory must be \
specified."
fi

#################################################
# Validate arguments
#################################################
# Check that directory exists
if ! readlink -e "${build_path}" > /dev/null; then
    err "Build folder is invalid."

# check that directory has the required subdirectories
elif ! readlink -e "${build_path}/Physics" > /dev/null || \
    ! readlink -e "${build_path}/Physics/Physics" > /dev/null || \
    ! readlink -e "${build_path}/Physics/Physics_Data" > /dev/null || \
    ! readlink -e "${build_path}/Renderer" > /dev/null || \
    ! readlink -e "${build_path}/Renderer/Renderer" > /dev/null || \
    ! readlink -e "${build_path}/Renderer/Renderer_Data" > /dev/null; then
    err "Build folder is missing files. Ensure that the folder is the correct \
folder."

# check that relevant permissions are available
elif [[ ! -r $build_path ]] || [[ ! -w $build_path ]] || \
    [[ ! -x $build_path ]] || [[ ! -x "${build_path}/Physics/Physics" ]] || \
    [[ ! -x "${build_path}/Renderer/Renderer" ]];then
    err "Missing permission for folders."
else 
    build_path=$(readlink -e "${build_path}")
fi

# validate render path
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

if [[ -n $json_path ]]; then
    if ! readlink -m "${json_path}" > /dev/null; then
        err "Invalid json path \"${render_path}\""

    # Try to create the directory if it does not exist
    elif ! readlink -e "${json_path}" > /dev/null \
        && ! mkdir -p "${json_path}"; then
        err "Could not create json output directory."

    # Check permissions for path
    elif [[ ! -r $json_path ]] || [[ ! -w $json_path ]]; then
        err "Missing permissions for json output directory. \
Please ensure that you have the required permissions for this directory."

    # Resolve the path
    else
        json_path=$(readlink -e "${json_path}")
    fi
fi

# validate renderheight
if [ -n "$render_height" ] && [[ ! $render_height == +([0-9]) ]]; then
	err "Non-integer render height (${render_height}) provided."
fi

# validate renderwidth
if [ -n "$renderWidth" ] && [[ ! $renderWidth == +([0-9]) ]]; then
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

log verbose "Transmit: " "$transmit"
log verbose "Log to Json: " "$log_json"
log verbose "Build Path: " "$build_path"

if [[ -n "$json_path" ]]; then
    log verbose "Json Path: " "$json_path"
fi

log verbose "Export Count: " "$export_count"
log verbose "Export Delay: " "$export_delay"
log verbose "Total Export Time: " "$total_export_time"
log verbose "Render Path: " "$render_path"
log verbose "Render Width: " "$render_width"
log verbose "Render Height: " "$render_height"

run_renderer=("${build_path}/Renderer/Renderer" "-batchmode")
run_physics=("${build_path}/Physics/Physics" "-batchmode" "-nographics"
"-r \"${render_path}\"")

if [[ -n "$json_path" ]]; then
    run_physics+=("--writeToFile ${json_path}")
fi

if [[ $transmit == 'true' ]]; then
    run_physics+=("--transmit")
fi

if [[ $log_json == 'true' ]]; then
    run_physics+=("--logExport")
fi

if [[ -n "$export_delay" ]]; then
    run_physics+=("--delay ${export_delay}")
fi

if [[ -n "$export_count" ]]; then
    run_physics+=("--exportCount ${export_count}")
fi

if [[ -n "$total_export_time" ]]; then
    run_physics+=("--totalTime ${total_export_time}")
fi

if [[ -n "$render_height" ]]; then
    run_physics+=("-h ${render_height}")
fi

if [[ -n "$render_width" ]]; then
    run_physics+=("-w ${render_width}")
fi

run_physics+=("-logFile")
run_renderer+=("-logFile")
if [[ $VERBOSE != 'true' ]]; then
    run_physics+=("\"./physics.log\"")
    run_renderer+=("\"./renderer.log\"")
else
    run_renderer+=("/dev/stdout")
fi

log verbose "Physics arguments:" "${run_physics[@]}"
if [[ $transmit == 'true' ]]; then
    log verbose "Renderer arguments:" "${run_renderer[@]}"
fi

if [[ $transmit == 'true' ]]; then
    DISPLAY=:0 eval "${run_renderer[@]}" &
    echo "$!"
    renderer_pid=%%
fi

if ! eval "${run_physics[@]}" && [[ -n "$transmit" ]]; then
    kill -EXIT $renderer_pid
fi

wait $renderer_pid