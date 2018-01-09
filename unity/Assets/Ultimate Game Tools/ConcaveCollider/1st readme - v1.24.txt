________________________________________________________________________________________
                                    Concave Collider
                        Copyright © 2012-2015 Ultimate Game Tools
                            http://www.ultimategametools.com
                             contact@ultimategametools.com
________________________________________________________________________________________
Version 1.24


________________________________________________________________________________________
Introduction

The concave collider is a component for Unity3D that allows complex dynamic objects to
interact with precise collisions.
In Unity3D two mesh colliders can't collide unless at least one of them is marked as
convex, otherwise they would just go through each other. Most of the times this is a
serious limitation, as usually the scenario already is non-convex. This means all other
objects that interact with the scenario and with each other need to be marked as convex.
There are times when a convex collider is accurate enough for a given object, but many
other times it is not and in order to overcome this we have to approximate its volume by
grouping multiple convex colliders together (boxes, spheres, convex meshes...). Doing
this process by hand takes a lot of time to get acceptable results.

Unity3D documents this here (on "compound colliders"):
http://docs.unity3d.com/Documentation/Components/class-Rigidbody.html

The concave collider component analyzes the object's mesh and automatically generates
and assigns a set of convex meshes (hulls) to fit the object. This process is done only
once at authoring time and takes just a few seconds.
Add the concave collider component to a gameobject and with just one click it will
compute and assign the resulting set of colliders, it is that simple.
The output quality and complexity can be adjusted from the component itself, so it is
possible to create a simple version for mobile devices or a more complex and accurate
one when targeting a desktop configuration.

________________________________________________________________________________________
Requirements

Unity 3.5/4.x Pro, Unity 5 for Windows or Mac.
Products build with it will run on any platform.
Sample scenes have been created using Unity 3.5.5f3.


________________________________________________________________________________________
Help

For up to date help: http://www.ultimategametools.com/products/concave_collider/help
For additional support contact us at http://www.ultimategametools.com/contact


________________________________________________________________________________________
Acknowledgements

-3D Models:
   Axe model by Create4You: http://www.turbosquid.com/FullPreview/Index.cfm/ID/539662
   Spaceship and gun models by psionic: http://www.psionic3d.co.uk/?page_id=25

-The Convex Collider uses the great convex decomposition algorithms by:
   Khaled Mamou: http://khaledmammou.com/
   Julio Jerez from Newton Dynamics: http://www.newtondynamics.com
   John Ratcliff: http://www.codesuppository.blogspot.com


________________________________________________________________________________________
Version history

v1.24 - 31/10/2015

[FIX] Fixed .dll build settings where other platforms complained about colliding
      ConvexDecomposition dlls.

v1.23 - 04/05/2015

[FIX] Removed the VC 2013 runtime dependency.

v1.22 - 20/04/2015

[NEW] Reuploaded using Unity 5.0.0 to switch the minimum required Unity 5 version from
      5.0.1 to 5.0.0.

v1.20 - 02/04/2015

[NEW] Added full Unity 5 support by porting the dll to 64 bit platforms (Win & Mac).

v1.13 - 19/11/2013

[NEW] Changed "Create Mesh Assets" parameter name to "Enable Prefab Use" for better
      understanding.
[NEW] Changed "Create Hull Mesh" parameter name to "Add Hull Meshfilter" for better
      understanding.

v1.12 - 19/11/2013

[NEW] The generated hulls now get assigned the same layer as the source object.
[FIX] The convex decomposition dll (Windows) now prints error messages instead of
      crashing when a critical exception is thrown.

V1.11 - 08/11/2012

[NEW] Added a license section in this file.
[FIX] Fixed a bug introduced in version 1.1 that generated errors when creating a build.
	  A define was added to remove the UnityEditor dependency unless working from the
	  editor itself.
[CHG] Changed the version release dates, which were incorrect.

V1.1 - 25/10/2012:

[NEW] Added Mac support! A ConvexDecompositionDll.bundle has been added to \Plugins
[NEW] Added new "Algorithm" parameter.
      -Normal: Uses the same algorithm as in version 1.0
      -Fast:   The new default algorithm. Way faster and sometimes more accurate.
      -Legacy: The legacy algorithm checkbox that was in 1.0 has been moved here.
[NEW] Added new "Internal Scale" parameter:
      We found out that the convex decomposition algorithm is quite sensible to the
      scale of the mesh. This parameter forces the convex decomposition computation to
      be done using the mesh rescaled to fit a sphere with radius = Internal Scale.
      Playing with this value can improve the final results.
      If you still want to use the original mesh with no processing, set it to zero.
[NEW] Added new "Create Mesh Assets" parameter:
      This allows to create prefabs of objects that use the concave collider and
      instance them either at runtime through scripts or manually using the editor.
      If the colliders were generated with this option disabled, their mesh references
      will be empty when trying to instance a prefab.
[NEW] Added new "Normalize Input Mesh" parameter:
      This overrides the "Internal Scale" parameter. The mesh will be internally
      processed to fit a unit length sphere. Usually this doesn't improve the result
      but in some specific cases it can.
[FIX] Hulls now take the Gameobject's transform scale into account.
[CHG] "Create Hull Mesh" option has been moved from advanced options to the main panel.
[DEL] Removed the "Use Legacy Method" checkbox and moved it to the new Algorithm
      selector.

V1.0 - 23/09/2012:

[---] Initial release