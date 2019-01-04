WARNING: All below debug functionality has been depricated, this document remains for reference if needed in the future!!!!

Debug FPX Agent Controller

Debug Controller Notes:

All game SimObjects that are either Receptacles or Pickupable will have their name shown when under the reticle. This is determined by the SimObjManipType. This means you might see multiple instances of "Fridge" since the raycast is piercing multiple parts of the fridge at the same time.



Scene Setup:
1) Make sure the <DebugFPSAgentController> script is on the FPS Controller
2) Make sure the DebugCanvas prefab is in the scene
3) Press "~" ingame to activate Debug Controller

Controls:

////MOVEMENT////

//Target//
-Mouse to look at things with the Reticle -Point central reticle at objects to see if they are sim objects. Receptacles and Pickups under the reticle
will be listed under the cursor.

//Move//
-WASD to move

////RECEPTACLES////

//Open//
-Left Click - Open Receptacle under cursor

//Close//
-Right Click - Close Receptacle under cursor

-An "Out of Range" debug error will appear if Receptacle under reticle is too far away

////PICKUP AND PLACE////

Only one item can be stored in the inventory at a time. You must have an item in your inventory to place an item in a receptacle (duh)

//Pickup SimObject//
-Alphanumeric 1, 2, 3... 9, 0 - Press to try and pick up object from corresponding list of currently visible objects on top left of screen. Which number to press is dictated by the SimObject's position in the array.

                                
//Pickup First SimObject//
-E - Goes through the array of currently visible objects, then picks up the first sim object able to be placed in inventory encountered in the array. This is useful for when there are so many SimObjects within visibility that the Alphanumeric inputs start getting overwritten.
                

//Place SimObject in Receptacle//
-Space - Place object currently in inventory in targeted Receptacle under reticle



/////////////////////////////////////////////////////////////////////////////////////////////////////

Other Debug Notes:
If you edit an object's text fields, sometimes the scene will not save if you only edit ONLY those fields. Grab a random object and wiggle it a little, that will make Unity see something in the scene has actually changed, and then all changes will save.