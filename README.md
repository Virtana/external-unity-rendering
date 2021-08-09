# External Unity Rendering  

Performing image rendering/saving in a second instance of Unity, allowing the first instance to be unblocked.  

## Project Goals  

Run two instances of Unity in parallel. The “main” instance is responsible for physical calculations, and the “secondary” instance is responsible for rendering images. This entails:

- Picking/developing a format/method capable of saving and loading the state of a scene in Unity. (See [Milestone 2](#milestone-2--identify-method-for-describing-scene-state)).
- Setting up a communication channel to transfer scene state between two Unity instances (possibly using TCP/IP) (See [Milestone 3](#milestone-3--implement-this-method-to-transfer-scenes-between-unity-instances)).
- Rendering and saving to disk an image via one instance, that reflects the physical state of a scene from another instance (See [Milestone 4](#milestone-4--render-an-image-from-one-unity-instance-using-another-instance)).

## Prerequisites

- [Unity Editor](https://unity3d.com/get-unity/download/archive). Tested versions include: 
  - 2020.1.8f1
  - 2020.3.11f1
- Newtonsoft.Json. This package has the same requirements to Newtonsoft.Json as the following package. (See their readme.)
- [Newtonsoft.Json for Unity Converters](https://github.com/jilleJr/Newtonsoft.Json-for-Unity.Converters). The package will fail to install if this package is not installed beforehand.
- Powershell (Windows) or Bash (GNU/Linux) (for script usage)

## Installation (Currently in progress)

There is currently no release for the project, as it is still in a very early state, but the package can be added by cloning the repo and copying `<repo-root>/Packages/External Unity Rendering` to `<project-root>/Packages/External Unity Rendering`.

## Usage

Add the package to your project, build the project, and run the build. See below for how to run the built project in renderer or exporter mode. There is also an editor menu for exporting without building the entire project.

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

`Unity ‑quit ‑batchmode ‑nographics ‑projectPath <project‑path> ‑logFile <path‑to‑logFile> [build‑options] (‑buildLinux64Player|‑buildLinux64Player) <path‑to‑output‑executable>`

#### Running the Renderer

`Usage: <path‑to‑the‑renderer‑executable> render [options]`

| Options | Argument | Description |
|--------------|----------|-------------|
| `‑t` or `‑‑transmit` | `‑` | Whether to launch renderer and transmit scene states from physics instance. |
| `‑r` or `‑‑renderPath` | `<path‑to‑render‑output>` | The path to where renders are to be made. |
| `‑p` or `‑‑port` | `<port‑to‑listen‑on>` | The port on which the renderer should listen on. Defaults to 11000. |
| `‑i` or `‑‑interface` | `<interface‑to‑listen‑on>` | The interface on which the renderer should listen on. Defaults to localhost. |

#### Running the Main Instance

The physics instance can be run as follows:

`Usage: <path‑to‑physics‑executable> export [options]`

| Options | Argument | Description |
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

## Progress

1. [X] [Milestone 1](#milestone-1--make-a-custom-camera-in-unity) : Make a custom camera.
2. [X] [Milestone 2](#milestone-2--identify-method-for-describing-scene-state) : Identify options for saving/communicating scene state.
3. [X] [Milestone 3](#milestone-3--implement-this-method-to-transfer-scenes-between-unity-instances) : Implement the chosen option.  
    1. [X] [Milestone 3a](#milestone-3a--save-a-scene-to-a-file) : Save the scene to file.
    2. [X] [Milestone 3b](#milestone-3b--from-a-saved-file-successfully-load-a-scene-in-unity) : Load the saved file in Unity.
    3. [X] [Milestone 3c](#milestone-3c--compare-the-scenes) : Compare the original scene to the loaded scene
    4. [X] [Milestone 3d](#milestone-3d--send-a-scene-from-one-instance-to-another) : Programmatically communicate the scene.
4. [X] [Milestone 4](#milestone-4--render-an-image-from-one-unity-instance-using-another-instance) : Using the communication method developed in Milestone 3, use the camera from Milestone 1 to render an image.
5. [X] [Milestone 5](#milestone-5--merge-unity-projects) : Merge Unity projects
6. [X] [Milestone 6](#milestone-6--automate-the-process) : Automate the process
   1. [X] [Milestone 6.1](#milestone-61--merge-instances) : Merge Builds
   2. [X] [Milestone 6.2](#milestone-62--convert-from-project-to-unity-package) : Isolate exporter/renderer code into Unity Package
7. [ ] [Milestone 7](#milestone-7--upgrade-the-design) : Upgrade the design
   1. [ ] [Milestone 7a](#milestone-7a--container-deployment) : Container Deployment
   2. [ ] [Milestone 7b](#milestone-7b--multiple-renderers--render-farm) : Multiple Renderers / Render Farm
   3. [ ] [Milestone 7c](#milestone-7c--extending-the-serialized-states) : Extending the Serialized states
   4. [ ] [Milestone 7d](#milestone-7d--reduce-simulation-time) : Reduce simulation time
   5. [ ] [Milestone 7e](#milestone-7e--more-rendering-options) : More rendering options
   6. [ ] [Milestone 7f](#milestone-7f--better-logger) : Better Logger
   7. [ ] [Milestone 7g](#milestone-7g--add-tests-to-package) : Add Tests to the package

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

Integrate the two projects into ones such that the project can be built for the two configurations (physics or renderer).

### Milestone 6 : Automate the process

The process of periodically saving the physical state of the scene and rendering/saving it externally should be made automatic. Ideally, this involves writing a bash script which, given a Linux build of the Unity project, starts and manages the process.

### Milestone 6.1 : Merge Builds

The current method of building and running involves using preprocessor definitions to build two instances, which only change what init script is run. This should be changed to a single init script, which:

1. Ensures that is not being run in the editor.
2. Parses command line arguments, checks if a renderer flag is set, and runs appropriately.

### Milestone 6.2 : Isolate exporter/renderer code into Unity Package

[Milestone 5](#milestone-5--merge-unity-projects) and [Milestone 6.1](#milestone-61--merge-instances) involved having a unity project with the exporter capability, but they still involve an entire unity project. This milestone reduces the project to a package, so it can be easily added to a project using the Unity Package Manager or when the project is being built from command line.

### Milestone 7 : Upgrade the design

The following Milestones include possible improvements to the EUR code.

#### Milestone 7a : Container Deployment

Run multiple physics and renderer instances, and deploy them using either:
    - [Docker](https://www.docker.com/) + [Kubernetes](https://kubernetes.io/) or
    - [AWS Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_docker_ecs.html) 

#### Milestone 7b : Multiple Renderers / Render Farm

This can be achieved by:
- Setting up a script or executable to launch renderers and manage them. It will intercept the states from the single physics instance and route them to the various renderers. 
- Adding multiple sender functionality to the physics instance. The Physics instance will accept multiple interfaces and/or ports to connect to, and will send scene states to the renderers on these ports.

Optionally this program/script can hook into their logs to determine which  renderers are completed rendering or are still rendering.

#### Milestone 7c : Extending the Serialized states.

Functions to be added for the serialized state:
    - Support for necessary components such as Mesh Filter, Mesh Renderer, Camera.
    - Automatically delete or add components and gameobjects as they are specified in the state file.
    - Handle materials and shaders.

#### Milestone 7d : Reduce simulation time

During batchmode, some time is wasted while the exporter waits for the delay time. This time may be reduced by reducing simulation time, possibly using a combination of tweaking `Application.targetFrameRate` (to influence `Time.deltaTime`) and `Physics.Simulate` or `Time.fixedDeltaTime` to reduce some of the time spent simulating. 

#### Milestone 7e : More rendering options

The renderer instance should be able to either:
1. Read from a config file for rendering settings.
2. Read general rendering settings from the state json.
3. Accept commands from command line (e.g. `--render-options 'fov=120;msaa=0;clippingplanes=0.1,1500`).

#### Milestone 7f : Better Logger

The default unity logger using `Debug.Log` adds a lot of extra lines and unnecessary stack traces. The logs also do not allow differentiating between verbose information (such as individual camera render logs) and general logs (such as completed render batch). A custom logger would be useful with the following features:
- Log Levels: verbose, debug, info, warning, error. Default = info.
- Write to console and file (Need to find a way to consistently have unity write logs to console).

#### Milestone 7g : Add Tests to Package

Adding Tests to the Package will allow more guarantees as to proper functionality of the package.

#### Milestone 7f : Export complete notification

Add some sort of notification for export complete.