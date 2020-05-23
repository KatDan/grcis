# Extension: ScriptedAnimatedCamera

### Author: Jan Matějka

### Category: Animated Camera

### Namespace: Rendering.JanMatejka

### Class name: ScriptedAnimatedCamera : StaticCamera, ITimeDependent

### ITimeDependent: Yes

### Source file: ScriptedAnimatedCamera.cs, CameraScript.txt

### Project to use in: 062animation-script

This extension implements a simple animated camera which can be scripted using a script file ``CameraScript.txt``. 
This camera uses **Catmull-Rom interpolation spline** for both the camera path and "lookAt" points. Each point must have a time assigned - this means that all the points **do not have to be distributed uniformly over time**.
You can specify how smooth the interpolated path will be in the script file by setting the pointsPerSegment. More info in the Usage section.

When the constructor is called, ``CameraScript.txt`` is read and all path and "lookAt" points are loaded and then interpolated. After that every time the ``Time`` property is updated, a point from a list of interpolated points is chosen according to the new time and the camera position and look direction is updated.

#### Usage
##### Scene snippet example
```
using Rendering.JanMatejka;

...

context[PropertyName.CTX_START_ANIM]    =  0.0;
context[PropertyName.CTX_END_ANIM]      = 3.5; // Make sure the times are correct 
                                               // (e.g. END_ANIM has to be >= than the time of the last path point!)
context[PropertyName.CTX_FPS]           = 25.0;

...

// Camera.
ScriptedAnimatedCamera cam = new ScriptedAnimatedCamera(new Vector3d(0.7, -0.4,  0.0),
                                                        new Vector3d(0.7,  0.8, -6.0),
                                                        50.0 );
```
The constructor has following arguments: ``ScriptedAnimatedCamera (Vector3d lookat, Vector3d center, double ang)``

##### CameraScript.txt usage
```
pointsPerSegment=75
t=0.0,point=10.0;20.0;3.0,lookAt=0.7;-0.4;0.0
t=0.0,point=20.1;15.0;15.0,lookAt=0.7;-0.4;0.0
t=1.0,point=10.0;10.0;16.0,lookAt=1.7;-0.7;0.8
t=1.5,point=10.5;6.4;6.5,lookAt=1.5;-1.4;1.0
t=2.0,point=25.0;15.0;30.0,lookAt=5.5;-5.4;0.0
t=2.5,point=20.1;2.0;11.0,lookAt=10.5;-10.4;0.0
t=3.0,point=14.0;5.0;10.0,lookAt=10.5;-10.4;0.0
t=0.0,point=17.5;0.4;6.5,lookAt=10.5;-10.4;0.0
```
The first line has to contain ``pointsPerSegment=<int>``. It defines the number of generated points between each defined point. Too large number and low fps can cause that some generated points will be skipped.

Then there are definitions of points. Each  line has 3 arguments separated by ``,``.

First argument is ``t=<double>`` which defines the time when the camera should reach that point.

Next argument ``point=<double x;double y;double z>`` are the coordinates of the camera position.

Last argument ``lookAt=<double x;double y; double z>`` are the coordinates of the point for the camera to look at.

**The second and the last line** are definitions of points for open curve interpolation - the time argument is ignored! You can leave it to 0.0. These points are not actually in the path.

### Video example
[YouTube video 320x180](https://youtu.be/W_7T2Awhlpw)