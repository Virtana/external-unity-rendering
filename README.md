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
Currently the project only runs a test scene with some random forces applied at runtime. The exporter and importer are accessible in the editor, and automatically run in a standalone build. The project can be built for Windows or Linux, and then run using command line. Scripts are provided (Powershell and Bash) which help automate this process.

### Using the scripts
#### The Build Script

`Usage: ./build.sh [options]` or `.\build.ps1 [options]`

| Powershell Options | Bash Options | Argument | Description |
|--------------|--------------|----------|-------------|
| `‑ProjectPath` | `‑p` | `<path‑to‑project>` | (Required) Path to the unity project to build. Output executable is given the name of the project directory. |
| `‑BuildPath` | `‑o` | `<path‑to‑output‑directory>` | (Required) Path to the directory where the build should be saved. |
| `‑Unity` | `‑u` | `<path‑to‑unity‑editor>` | (Required) Path to the Unity Editor Executable to use to build the project. |
| `‑PurgeCaches` | `‑c` | `‑` | Whether to remove all non‑essential project files to force Unity to regenerate all necessary files. |
| `‑BuildLinux` | `‑` | `‑` | Build a linux64 executable instead of windows. |
| `‑` | `‑w` | `‑` | Build a win64 executable instead of linux. |
| `‑Verbose` | `‑v` | `‑` | Activate verbose mode. Also output unity build logs to console. |

#### The Main Instance Script

`Usage: ./run‑exporter.sh [options]` or `.\run-exporter.ps1 [options]`

| Powershell Options | Bash Options | Argument | Description |
|--------------|--------------|----------|-------------|
| `‑ExecutablePath` | `‑e` | `<path‑to‑exporter>` | (Required) Path the the built executable. |
| `‑BatchMode` | `‑b` | `-` | Whether to enable batchmode and nographics. Intended for automated exports. |
| `‑RenderPath` | `‑r` | `<path‑to‑render‑output>` | Path to where the renders should be saved. Path is stored to the serialised json. Can be overriden by the renderer instance. |
| `‑RenderHeight` | `‑h` | `<pixel‑height>` | Height of the image in pixels. |
| `‑RenderWidth` | `‑w` | `<pixel‑width>` | Width of the image in pixels. |
| `‑Transmit` | `‑t` | `-` | Whether the exporter should transmit the states to a renderer instance. |
| `‑LogJson` | `‑l` | `-` | Whether the serialized scene state should be logged to the console. |
| `‑JsonPath` | `‑j` | `<path‑to‑output‑json>` | Save the scene state as json files in the directory `<path‑to‑output‑json>`. |
| `‑ExportCount` | `‑c` | `<export‑count>` | The number of exports to make. Must be combined with either the export delay or the total export time to automatically export.  |
| `‑ExportDelay` | `‑d` | `<export‑delay>` | The delay between exports. Must be combined with either the export count or the total export time to automatically export. (Delay must be greater than at least 10ms.) |
| `‑TotalExportTime` | `‑s` | `<total‑export‑time>` | The total amount of time to export for. Must be combined with either the export delay or export count to automatically export. Equal to export delay * export count. |
| `‑Port` | `‑p` | `<port_number>` | IP address to transmit to. |
| `‑Interface` | `‑i` | `<ip_address>` | Port to transmit to. |
| `‑Verbose` | `‑v` | `‑` | Activate verbose mode. Also output unity logs to console. |

#### The Renderer Script

`Usage: ./run‑exporter.sh [options]` or `.\run-exporter.ps1 [options]`

| Powershell Options | Bash Options | Argument | Description |
|--------------|--------------|----------|-------------|
| `‑ExecutablePath` | `‑e` | `<path‑to‑exporter>` | (Required) Path the the built executable. |
| `‑RenderPath` | `‑r` | `<path‑to‑render‑output>` | Path to where the renders should be saved. Overrides the path saved in the json. |
| `‑Port` | `‑p` | `<port_number>` | IP address to listen on for. |
| `‑Interface` | `‑i` | `<ip_address>` | Port to listen on. |
| `‑Verbose` | `‑v` | `‑` | Activate verbose mode. Also output unity logs to console. |

### Manually Building and Running

#### Building using the Unity Editor

Building is performed using the untiy editor. This can be performed through the GUI editor or command line. From command line, the project can be built using: 
Usage: 

`Unity -quit -batchmode -nographics -projectPath <project-path> -logFile <path-to-logFile> [build-options] -(buildLinux64Player|buildLinux64Player) <path-to-output-executable>`

#### Running the Renderer

`Usage: <path-to-the-renderer-executable> render [options]`

| Bash Options | Argument | Description |
|--------------|----------|-------------|
| `‑t` or `‑‑transmit` | `‑` | Whether to launch renderer and transmit scene states from physics instance. |
| `‑r` or `‑‑renderPath` | `<path‑to‑render‑output>` | The path to where renders are to be made. |
| `‑p` or `‑‑port` | `<port‑to‑listen‑on>` | The port on which the renderer should listen on. Defaults to 11000. |
| `‑i` or `‑‑interface` | `<interface‑to‑listen‑on>` | The interface on which the renderer should listen on. Defaults to localhost. |

#### Running the Main Instance
The physics instance can be run as follows:

`Usage: <path‑to‑physics‑executable> export [options]`

| Bash Options | Argument | Description |
|--------------|----------|-------------|
| `‑t` or `‑‑transmit` | `‑` | Whether to launch renderer and transmit scene states from physics instance. |
| `‑‑logExport` | `‑` | Whether the physics instance should print the serialized state to the console/log. |
| `‑‑writeToFile` | `<path‑to‑json‑export‑directory>` | The path to where the the serialized json should be exported to. If not specified, no files are saved. |
| `‑r` or `‑‑renderPath` | `<path‑to‑render‑output>` | The path to where renders are to be made. |
| `‑h ` or `‑‑renderHeight` | `<height‑of‑rendered‑image>` | The pixel height of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑w` or `‑‑renderWidth` | `<width‑of‑rendered‑image>` | The pixel width of the renders. Minimum of 300. Extremely large values can cause an out of VRAM issue. |
| `‑d` or `‑‑delay` | `<time‑between‑exports>`[<sup>*</sup>](#time-note) | The delay between exports. Must be used with one of the other export specifiers. |
| `‑e ` or `‑‑exportCount` | `<n‑exports>` | The number of exports to make. Must be used with one of the other export specifiers. |
| `‑s` or `‑‑totalTime`  | `<time‑to‑spend‑executing>`[<sup>*</sup>](#time-note) | The total amount of time to export for. Equal to delay * export count. Must be used with one of the other export specifiers. |
| `‑p` or `‑‑port` | `<port‑to‑send‑to>` | The port on which the exporter should transmit to. Defaults to 11000. |
| `‑i` or `‑‑interface` | `<interface‑to‑send‑to>` | The interface on which the renderer should transmit to. Defaults to localhost. |

If transmit is specified, and a renderer is not launched, the physics instance may export all the scene states and queue them to be sent until renderer is launched.

The instances can be used with other unity standalone arguments, but the renderer cannot be run with nographics, as it currently requires a GPU to render.

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
