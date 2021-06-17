# TODO

# REMOVE ALL TODO COMMENTS

## Debugging 

For debugging, approaches may include:
1. Build detection (investigate [this link](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html#:~:text=You%20use%20the,%E2%80%9D%20option%20enabled.) for using #define). 

### Common

1. Add file/folder path validation for all provided directories and file paths. Additionally add safety for two renders being completed in same second (e.g. screenshot-now.png, screenshot-now (1).png). 

2. Proper Exception Handling. No Pokemon Exceptions unless to protect against possibly unexpected exceptions.

3. Hook into JSONSerializationSettings for Newtonsoft.JSON that registers errors. Possibly correct the error in Exporter and in the Importer request the server to resend (for transmission errors as well).

4. Contain all calls to Debug.Log and derivatives within the Debug options. LogError options can be:

   1. Removed and replaced with dispatcher messaging (in a situation where builds are deployed in pairs by a dispatcher). Server connection & configuration may be necessary.

   2. Left as is, to save warnings to the [Unity Log file](https://docs.unity3d.com/Manual/LogFiles.html).

   3. A combination of both, where log files and Server messaging is used.

5. Validate multi-cam operations. Also add dynamic naming (depending on the camera). May need to implement some sort of parent name scheme, or folders for each camera (if more than 1).

6. Determine folder structure for all files. E.g.:
    - Physics
      - Scene States
      - Renders (No subfolders used if )
        - Cam1
        - Cam2
    - External Renderer
      - Recieved Data
      - Renders
        - Cam1
        - Cam2

    Also, folders need to be reset/zipped and renamed each time the system is regenerated. 
    
7. Replace all public variables with properties. Any variables which need to be serialized in the Unity Editor must be replaced with:
   > ```C#
   > [SerializeField]
   > private T _backingStore = default;
   > public T property { get; private set }
   > ```
    Any restrictions must be handled with Unity Attributes or separate Unity Editor checks in code.

8. Update all XML Documentation and remove extraneous comments.

9. Refactor for Asynchronous Transmission. May need to add a custom check for whether it is data receipt or a closing signal.

10. Add IP search/control options. Investigate how to make/use IPEndpoints and RemoteIPEndpoints.

11. Add single instance checking for ExportScene and ImportScene.
12. Move Editor only code to scripts in the editor folder.
13. Do more testing. 
 
### Physics/ExportScene

1. Add Writing to file as debug options.
2. Add options to disable transmission.
3. Add response handling for the sender response. Probably add some sort of enum of states which can be read to understand the status. E.g.
   ```C#
     enum ImportResponse 
     {
        OK = 0,
        TransmissionError = 1,
        ParseError = 2,
        ConnectionEndedEarly = 3,
     } // etc
   ```
4. Switch byte array to `IList<ArraySegment<byte>>` and getting the socket error code. If dispatcher is used, could be communicated.

### ImportScene

1. Add reading to file as debug option.
2. Investigate the need for using the ExportTimestamp/where it will be used. Currently unused.
3. Investigate the need for running importer as a job. Ensure Unity does not seize.
4. Test Multicam support.

---

## General Improvements

List of non-urgent improvements. May be implemented now or in a future branch depending on complexity.

- Determine possible replacements/extensions for JSON for more efficiency. Options include but are not limited to:
  - BSON - Will need to override Newtonsoft.JSON.BSON included with Newtonsoft.JSON for Unity.
  - UBJSON - May be an alternative to BSON.
- If some sort of dispatcher method is used, for example a program that spins up docker contatiners, implement options to have it communicate with the physics instance to have it export automatically at certain timestamps. May need to add a Update function which reads a list of timestamps and optionally target objects to keep in frame (could be implemented with lookat or aim constraint).

---

## Possible CommandLine Options

Options defined here may need to be taken into account for updates to code leading up to the eventual implementation.

1. `--no-tcp` [DEBUG]
    > Set the instance to not use TCP/IP transmission.

2. `--debug` 
    > Enable debug options. If this flag is not set, ignore the debug commands.

3. `-o folder | --output folder` 
   > Read/write files to this folder. Default is folder is the [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).

4. `--physics` 
   > Run as physics instance. Not combinable with `--physics` and physics-related command line options.

5. `--renderer` 
   > Run as renderer instance. Not combinable with `--physics` and physics-related command line options. 

6. `-p port` 
   > Set local communication port to _port_.

7. `-i address` 
   > Set communication address to _address_.


#### Physics Only

1. `--generate-screenshot` [DEBUG]
   > Generate a screenshot on export. For comparing the expected vs. actual states of the scene. Will add a camera to the scene, and render to the output path.
2. `--save-state [json | _others_]`
   > Saves the state to the folder specified using the `-o` switch. The argument will determine the file format used to save with. If no other file formats are chosen (like BSON or UBJSON), the argument option will be removed.

#### Renderer Only

1. `--render-path folder` [DEBUG]
   > Save renders to _folder_. If not set, defaults to [output folder]/Renders.

2. `--single-camera`
   > Only render images from a the first camera found in the scene. If not set and there is more than 1 camera in the scene, the renderer will generate images from each camera in the format [To be determined]. 

3. `--write-import` [DEBUG]
   > Writes the data received by the renderer instance to file.