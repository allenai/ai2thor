Debug FPX Agent Controller

Scene Setup:
1) Enable the DebugFPSAgentController script on the FPS Controller in the scene
2) Disable the DiscreteRemoteFPSAgentController script
3) make sure the “DebugCanvas” prefab is in the scene and enabled

Controls:

////MOVEMENT////

//Target//
-Mouse to look at things with the Reticle

//Move//
-WASD to move

-Point central reticle at objects to see if they are sim objects. Receptacles and Pickups under the reticle
will be listed under the cursor.

////RECEPTACLES////

//Open//
-Left Click - Open targeted Receptacle

//Close//
-Right Click - Close targeted Receptacle

-An "Out of Range" debug error will appear if Receptacle under reticle is too far away

////PICKUP AND PLACE////

Only one item can be stored in the inventory at a time. You must have an item in your inventory to place an item in a receptacle (duh)

//Pickup SimObject//
-Alphanumeric 1, 2, 3... 9, 0 - Press to try and pick up object from corresponding list of currently visible objects on top left of screen. Which number to press is dictated by the SimObject's position in the array.

                                
//Pickup First SimObject//
-E - Goes through the array of currently visible objects, then picks up the first sim object able to be placed in inventory encountered in the array. This is useful for when there are so many SimObjects within visibility that the Alphanumeric inputs start getting overwritten.
                

//Place SimObject in Receptacle//
-Space - Place object currently in inventory in targeted Receptacle under reticle