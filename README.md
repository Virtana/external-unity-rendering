# External Unity Rendering  

Performing image rendering/saving in a second instance of Unity, allowing the first instance to be unblocked.  

## Project Goals  

Run two instances of Unity in parallel. The “main” instance is responsible for physical calculations, and the “secondary” instance is responsible for rendering images. This entails:

- Picking/developing a format/method capable of saving and loading the state of a scene in unity. (A good option is to use Pixar's USD, due to its growing popularity) (See [Milestone 2](#milestone-2--identify-method-for-describing-scene-state)).
- Setting up a communication channel to transfer scene state between two Unity instances (possibly using TCP/IP) (See [Milestone 3](#milestone-3--implement-this-method-to-transfer-scenes-between-unity-instances)).
- Rendering and saving to disk an image via one instance, that reflects the physical state of a scene from another instance (See [Milestone 4](#milestone-4--render-an-image-from-one-unity-instance-using-another-instance)).

## Progress

1. [X] Milestone 1 : Make a custom camera.
2. [ ] Milestone 2 : Identify options for saving/communicating scene state.
3. [ ] Milestone 3 : Implement the chosen option.  
    1. [ ] Save the scene to file.
    2. [ ] Load the saved file in Unity.
    3. [ ] Compare the original scene to the loaded scene
    4. [ ] Programmatically communicate the scene.
4. [ ] Milestone 4 : Using the communication method developed in Milestone 3, use the camera from Milestone 1 to render an image.
5. [ ] Milestone 5 : Increase the scalability of the design.

## Milestones

### Milestone 1 : Make a custom camera in Unity

Create a simple scene in Unity with a custom camera. This involves writing a script which makes use of the built in Unity “Camera” object to generate images at some regular time interval or on demand. These images should be saved to disk.

### Milestone 2 : Identify method for describing scene state

Find/develop a method of communicating and loading the state of a scene in Unity.This could mean developing a custom method of communicating the state of a scene via a TCP/IP socket.

Current options include:

- Pixar USD
- Khronos Group glFX

### Milestone 3 : Implement this method to transfer scenes between Unity instances

Transfer a scene from the main instance to the secondary one, then have the secondary instance render the scene. This could mean saving the scene to disk in a specific format and loading it with the secondary instance. Alternatively, it could mean transferring the data over TCP/IP to the secondary instance.

#### Save a scene to a file

Use the method chosen to save the state of a current scene to disk.

#### From a saved file, successfully load a scene in Unity

From the file saved on disk, load the state of a scene into Unity and ensure all objects are loaded in correctly.

#### Compare the scenes

Compare the scene from which the file was saved, and the scene that represents the loaded file. Confirm they are identical. This can be done visually at first, but also needs to be checked programmatically.

#### Send a scene from one instance to another

Transfer the state of a scene from one instance to another while they are both running. This should ideally be done via TCP/IP communication of some kind.

### Milestone 4 : Render an image from one Unity instance, using another instance

This milestone involves the method implemented in Milestone 3, as well as the camera/scene constructed in Milestone 1. Communicate the state of one Unity instance to another, and render/save an image reflecting this state in the other instance.

### Milestone 5 : Upgrade the design to be more scalable

Options include (but are not limited to):

- Docker + [Kubernetes](https://kubernetes.io/) or [AWS Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_docker_ecs.html)
