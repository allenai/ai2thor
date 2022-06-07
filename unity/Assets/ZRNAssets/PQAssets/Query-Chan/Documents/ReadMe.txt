Query-Chan Asset ReadMe

Powered by Pocket Queries, Inc.
http://www.pocket-queries.co.jp
http://www.query-chan.com


-----------------------------------
Asset Information
-----------------------------------
File Name    :  Query-Chan.unitypackage
Version Info.:  Ver. 1.2.0  
Release Date :  29th, Aug, 2014

< Version Info >
(29th, Aug, 2014) Ver. 1.2.0 : Fix shader problem.
(27th, Aug, 2014) Ver. 1.1.0 : Add 7 animations.
(20th, Aug, 2014) Ver. 1.0.0 : Initial Release.


-----------------------------------
File Structure
-----------------------------------
Assets
 --> PQAssets
    --> Query-Chan
       --> Documents  :  ReadMe file and License documents.
       --> Materials  :  Query-Chan body and face emotion materials.
       --> Models     :  Base FBX model.
       --> Prefabs    :  Query-Chan prefab model. (You sould use this file in your game hierarchy.)
       --> Scenes     :  Scene files for demonstration.
       --> Scripts    :  Scripts files.
                         <for demo scene>
                           - CameraAction.cs
                           - GUIController.cs
                           - GUIControllerFlying.cs
                         <for Query-Chan contorol>
                           - QueryAnimationController.cs (to control animations)
                           - QueryEmotionalController.cs (to control face emotion materials)
                           - QuerySoundController.cs (to control sound effect)
       --> Shaders    :  Original shaders for Query-Chan
       --> Sounds     :  You can use about 70 Japanese voice files. so cute!
	   --> Textures   :  Query-Chan body and face textures, and icons for demo app.


-----------------------------------
How to use Query-Chan
-----------------------------------
1. Import Query-Chan.unitypackage to your Game project.

2. Find Query-Chan.prefab in Prefabs folder and Drop it to your Game scne (hierarchy).

3. Press Play button and play game, you can see Query-Chan with default animation (animation name : 000_Def).

4. You can control Query-chan by using below scripts.

   <How to control animations>
     - Use ChangeAnimation() method in the QueryAnimationController class.
       You can use 24 animations. (Please see "enum QueryChanAnimationType" in the class file.)

   <How to control emotions>
     - Use ChangeEmotion() method in the QueryEmotionalController class.
       You can use 22 emotions. (Please see "enum QueryChanEmotionalType" in the class file.)

   <How to control Sounds>
     - Use PlaySoundByType() method in the QuerySoundController class.
       You can use 70 sounds. (Please see "enum QueryChanSoundType" in the class file.)



-----------------------------------
Demo Scene
-----------------------------------
1. You should add below scene files to "Scenes in build" in the "Build Settings window".
     - 01_OperateQuery_Standing.unity
     - 02_OperateQuery_Flying.unity
     - 03_OperateQuery_Attack.unity

2. Play game, you can change animations, emotions and also can play voice sounds to press buttons on views.
   Please try to press "to Fly Mode" button, so you can use another animation patterns.



-----------------------------------
Logo Icon File
-----------------------------------
You are free to use "Query-Chan Logo Image File" below.
We are glad that you use the logo image on your Game applications.

   Assets/PQAssets/Query-Chan/Documents/Query-Chan_license_logo.png
