# External Unity Rendering  

Performing image rendering/saving in a second instance of Unity, allowing the first instance to be unblocked.  

## Project Goals  

Run two instances of Unity in parallel. The “main” instance is responsible for physical calculations, and the “secondary” instance is responsible for rendering images. This entails:

- Picking/developing a format/method capable of saving and loading the state of a scene in Unity. (See [Milestone 2](#milestone-2--identify-method-for-describing-scene-state)).
- Setting up a communication channel to transfer scene state between two Unity instances (possibly using TCP/IP) (See [Milestone 3](#milestone-3--implement-this-method-to-transfer-scenes-between-unity-instances)).
- Rendering and saving to disk an image via one instance, that reflects the physical state of a scene from another instance (See [Milestone 4](#milestone-4--render-an-image-from-one-unity-instance-using-another-instance)).

## Progress

1. [X] [Milestone 1](#milestone-1--make-a-custom-camera-in-unity) : Make a custom camera.
2. [X] [Milestone 2](#milestone-2--identify-method-for-describing-scene-state) : Identify options for saving/communicating scene state.
3. [X] [Milestone 3](#milestone-3--implement-this-method-to-transfer-scenes-between-unity-instances) : Implement the chosen option.  
    1. [X] [Milestone 3a](#milestone-3a--save-a-scene-to-a-file) : Save the scene to file.
    2. [X] [Milestone 3b](#milestone-3b--from-a-saved-file-successfully-load-a-scene-in-unity) : Load the saved file in Unity.
    3. [X] [Milestone 3c](#milestone-3c--compare-the-scenes) : Compare the original scene to the loaded scene
    4. [X] [Milestone 3d](#milestone-3d--send-a-scene-from-one-instance-to-another) : Programmatically communicate the scene.
4. [X] [Milestone 4](#milestone-4--render-an-image-from-one-unity-instance-using-another-instance) : Using the communication method developed in Milestone 3, use the camera from Milestone 1 to render an image.
5. [X] [Milestone 5](#milestone-5--merge-unity-projects) : Merge Unity projects.
6. [X] [Milestone 6](#milestone-6--automate-the-process) : Automate the process.
7. [ ] [Milestone 7](#milestone-7--upgrade-the-design) : Upgrade the design.

## Prerequisites
- [Unity Editor](https://unity3d.com/get-unity/download/archive). Tested versions include: 
  - 2020.1.8f1
  - 2020.3.11f1
  - Powershell (Windows) or Bash (GNU/Linux) (Optional for script usage)

## Usage
Currently the project only runs a test scene with some random forces applied at runtime. The exporter and importer are accessible in the editor, and automatically run in a standalone build. The project can be built for Windows or Linux. Current implementation requires building and running with specific command line arguments, and scripts are provided which simplify this process. 

### Using the scripts
#### The Build Script

`Usage: ./build.sh [options]` or `.\build.ps1 [options]`

| Option| Option | Argument | Description |
|----|----|---|---|
| `‑ProjectPath`| `‑p` | `<path-to-unity-project>` | Path to the Unity project to be built. |
| `‑BuildPath`| `‑o` | `<path-to-build-files-to>` | Path to where the built standalone executables should be saved. |
| | `‑u` | `<path-to-the-unity-executable>` | Path to the Unity Editor executable |
| `‑TempPath` | | `<path-to-copy-unity-project-to>` | (Optional) Path to copy Unity project files to. To be used in case write access is unavailable, or another editor is open with the project. |
| `‑BuildOptions`| `‑b` |  `<unity-build-options>` | (Optional) Options for the Build Script. See the [Unity Script Reference](https://docs.unity3d.com/ScriptReference/BuildOptions.html) for valid options. The format is "Option1, Option2, Option3, ..." |
| `‑BuildWindows`| `‑w` | `-` | Build a Windows Standalone instead of a Linux Standalone. |
| `‑v` | `-` | Enable Verbose Logging. Redirects Unity build logs to console. |

In the current directory, two log files are created, `physics_build.log` and `renderer_build.log` (unless -v in the bash script is provided). These files have the output of the Unity Editor, which can be checked if errors occur during the build process.

The Powershell build script returns a PSCustomObject with the Properties PhysicsPath and RendererPath, which are paths to the built executables.

The bash build script will:
- check if write access to the project is missing, or a UnityLockfile is present, and will copy all the required project files to a temp folder, and delete it after completion.
- check the unity editor version, and if an untested version is used, will ask for confirmation to continue. Using an untested Unity Editor version may cause issues during the build process or with the standalone executable.

#### The Run Scripts

`Usage: ./run.sh [options]` or `.\build.ps1 [options]`

| Powershell Options | Bash Options | Argument | Description |
|--------------------|--------------|----------|-------------|
| `‑ExecutablePath`| | `<path-object>` | A PSCustomObject with the Properties PhysicsPath and RenderPath, which are the paths to the physics and renderer executables. (This is returned by the powershell build script, and the build script can be piped to this script. ) |
| | `‑b` | `<path-to-executables>` | Where the built executables are. Should have subfolders named Physics and Renderer holding the respective executables. This should be the same directory specified to the build script. |
| `‑Transmit`| `‑t` | `-` | (Optional) Whether to launch renderer and transmit scene states from physics instance. |
| `‑LogJson`| `‑l` | `-` | (Optional) Whether the physics instance should print the serialized state to the console/log. |
| `‑JsonPath`| `‑j` | `<path-to-json-export-directory>` | (Optional) The path to where the the serialized json should be exported to. If not specified, no files are saved. |
| `‑RenderPath`| `‑r` | `<path-to-render-output>` | (Optional) The path to where renders are to be made. Required if the transmit option is set. |
| `‑RenderHeight `| `‑h ` | `<height-of-rendered-image>` | (Optional) The pixel height of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑RenderWidth`| `‑w` | `<width-of-rendered-image>` | (Optional) The pixel width of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑ExportDelay` | `‑d` | `<time-between-exports>`[<sup>*</sup>](#time-note) | The delay between exports. Must be used with one of the other export specifiers. |
| `‑ExportCount `| `‑e ` | `<n-exports>` | The number of exports to make. Must be used with one of the other export specifiers. |
| `‑TotalExportTime` | `‑s` | `<time-to-spend-executing>`[<sup>*</sup>](#time-note) | The total amount of time to export for. Equal to delay * export count. Must be used with one of the other export specifiers. |
| | `‑v` | `-` | Enable Verbose Logging. Redirects Unity logs to console. |


### Manually Building and Running

#### Building using the Unity Editor

`Usage: Unity -quit -batchmode -nographics -executeMethod BuildScript.Build [options]` or `Unity.exe -quit -batchmode -nographics -executeMethod BuildScript.Build [options]`

(See the [Unity Docs](https://docs.unity3d.com/Manual/CommandLineArguments.html) for an explanation of the arguments specified above. BuildScript.Build is the included script which manages building the two instances of unity. Currently not optional but may be in future.)

| Option | Argument | Description |
|--------|----------|-------------|
| `‑b` or `-‑‑build` | `<path-to-build>`| The output directory for the project. |
| `‑c` or `‑‑config` | `<configuration>`| Whether to build physics or a renderer instance. (Default is Renderer.) |
| `‑t` or `‑‑buildTarget` | `<target>`| See the [Unity Script Reference](https://docs.unity3d.com/ScriptReference/BuildTarget.html) for valid options. |
| `‑-options` | `<options>`| (Optional) Build options for Unity. See the [Unity Script Reference](https://docs.unity3d.com/ScriptReference/BuildOptions.html) for valid options. The format is "Option1, Option2, Option3, ...". Whitespace optional. |

#### Running the Executables Directly

The renderer requires no special arguments other than `-batchmode` to run in headless mode. `-nographics` cannot be used, as rendering requires a GPU. 

`Usage: <path-to-the-renderer-executable> -batchmode [unity-standalone-options]`

The physics instance can be run as follows:

`Usage: <path-to-physics-executable> -batchmode [options]`

| Bash Options | Argument | Description |
|--------------|----------|-------------|
| `‑t` or `‑‑transmit` | `-` | (Optional) Whether to launch renderer and transmit scene states from physics instance. |
| `‑‑logExport` | `-` | (Optional) Whether the physics instance should print the serialized state to the console/log. |
| `‑‑writeToFile` | `<path-to-json-export-directory>` | (Optional) The path to where the the serialized json should be exported to. If not specified, no files are saved. |
| `‑r` or `‑‑renderPath` | `<path-to-render-output>` | (Optional) The path to where renders are to be made. Required if the transmit option is set. |
| `‑h ` or `‑‑renderHeight` | `<height-of-rendered-image>` | (Optional) The pixel height of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑w` or `‑‑renderWidth` | `<width-of-rendered-image>` | (Optional) The pixel width of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑d` or `‑‑delay` | `<time-between-exports>`[<sup>*</sup>](#time-note) | The delay between exports. Must be used with one of the other export specifiers. |
| `‑e ` or `‑‑exportCount` | `<n-exports>` | The number of exports to make. Must be used with one of the other export specifiers. |
| `‑s` or `‑‑totalTime`  | `<time-to-spend-executing>`[<sup>*</sup>](#time-note) | The total amount of time to export for. Equal to delay * export count. Must be used with one of the other export specifiers. |

If transmit is specified, and a renderer is not launched, the physics instance may export all the scene states and queue them to be sent until renderer is launched.

<a class="anchor" id="time-note"></a>
`*` Time can be specified in the format `^[0-9]+(s|m)?$`, a non-negative integer, optionally followed by an s or m. An input x will be read as x milliseconds, xs as x seconds and xm as x minutes.

## Milestones

### Milestone 1 : Make a custom camera in Unity

Create a simple scene in Unity with a custom camera. This involves writing a script which makes use of the built in Unity “Camera” object to generate images at some regular time interval or on demand. These images should be saved to disk.

### Milestone 2 : Identify method for describing scene state

Find/develop a method of communicating and loading the state of a scene in Unity.This could mean developing a custom method of communicating the state of a scene via a TCP/IP socket.

Current options include:

- ~~Pixar USD~~
- ~~Khronos Group glFX~~
- Custom JSON + Unity Scene
    - Current method has a custom class representing the Scene state, which is serialized to and from JSON, using [Newtonsoft.JSON for Unity](https://github.com/jilleJr/Newtonsoft.Json-for-Unity) and [Unity Converters for Newtonsoft.JSON](https://github.com/jilleJr/Newtonsoft.Json-for-Unity.Converters)

### Milestone 3 : Implement this method to transfer scenes between Unity instances

Transfer a scene from the main instance to the secondary one, then have the secondary instance render the scene. This could mean saving the scene to disk in a specific format and loading it with the secondary instance. Alternatively, it could mean transferring the data over TCP/IP to the secondary instance.

#### Milestone 3a : Save a scene to a file

Use the method chosen to save the state of a current scene to disk.

#### Milestone 3b : From a saved file, successfully load a scene in Unity

From the file saved on disk, load the state of a scene into Unity and ensure all objects are loaded in correctly.

#### Milestone 3c : Compare the scenes

Compare the scene from which the file was saved, and the scene that represents the loaded file. Confirm they are identical. This can be done visually at first, but also needs to be checked programmatically.

#### Milestone 3d : Send a scene from one instance to another

Transfer the state of a scene from one instance to another while they are both running. This should ideally be done via TCP/IP communication of some kind.

### Milestone 4 : Render an image from one Unity instance, using another instance

This milestone involves the method implemented in Milestone 3, as well as the camera/scene constructed in Milestone 1. Communicate the state of one Unity instance to another, and render/save an image reflecting this state in the other instance.

### Milestone 5 : Merge Unity Projects

Integrate the two projects into one such that the project can be built for the two configurations (physics or renderer).

### Milestone 6 : Automate the process

The process of periodically saving the physical state of the scene and rendering/saving it externally should be made automatic. Ideally, this involves writing a bash script which, given a Linux build of the Unity project, starts and manages the process.

### Milestone 6.5 : Merge instances

The current method of building and running involves using preprocessor definitions to build two instances, which only change what init script is run. This should be changed to a single init script, which:

1. Ensures that is not being run in the editor.
2. Parses command line arguments, checks if a renderer flag is set

### Milestone 7 : Upgrade the design

Design improvements may include (but are not limited to):

- Convert to a unity package, and change build scripts to point to a project without the renderer options, and have it build with the package.

- Run multiple physics and renderer instances, and deploy them using either:
    - [Docker](https://www.docker.com/) + [Kubernetes](https://kubernetes.io/) or
    - [AWS Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_docker_ecs.html)

- Extending the SceneState and ObjectState classes (which are used to store the scene info) by:
    - Adding support for Components (only necessary components under the Mesh, Rendering and Effects categories.)
    - Adding Components and GameObjects that don't exist.
    - Add Handling of materials and shaders.

- ~~Rendering on Multiple Cameras in a multithreaded manner (possibly using Jobs)~~ (According to investigations, may be impractical to properly implement, as well as slightly counters the purpose of the project.)

- Time Acceleration or using Physics.Simulate to reduce simulation time.

- Better/More Rendering Options.

- Better Logger.
