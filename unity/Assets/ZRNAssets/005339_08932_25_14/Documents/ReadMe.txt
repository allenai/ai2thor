Japanese Otaku City Asset ReadMe

Powered by ZENRIN CO., LTD.
http://www.zenrin.co.jp/


-----------------------------------
Asset Information
-----------------------------------
File Name    :  JapaneseOtakuCity.unitypackage
Version Info.:  Ver. 1.1.0  
Release Date :  29th, Aug, 2014

< Version Info >
(29th, Aug, 2014) Ver. 1.1.0 : Add 7 animations to Query-Chan and Fix Shader probrem with Query-Chan.
(26th, Aug, 2014) Ver. 1.0.0 : Initial Release.


-----------------------------------
File Structure
-----------------------------------
Assets
 --> ZRNAssets
    --> 005339_08932_25_14 (Japane Otaku City 3D-Model Assets)
       --> Documents  :  ReadMe file and License documents.
       --> Materials  :  City materials.
       --> Models     :  FBX model.
       --> Scenes     :  Scene files for demonstration.
       --> Scripts    :  Scripts files.
                         <for demo scene>
                           - ZRNGUIController.cs
                         <for City contorol>
                           - AmbientController.cs (to control city ambient)
                           - CameraController.cs (to control city cameras)
       --> Shaders    :  Original shaders for Japanese Otaku City.
	   --> Textures   :  Japanese Otaku City emvironment textures.
    --> Cars          :  Car models.

***** Below folders are sample data created by another company
 --> Standard Assets (this assets are made by Unity Technologies.)
 --> PQAssets (These files of the foler are provided by Pocket Queries, Inc.)
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
Hierarchy and Texture Useful List
-----------------------------------
1. Texture List
  < Architecture >
    - BUildings with signboards :  005339_08932_25_14_0 - 005339_08932_25_14_106
    - Others : 005339_08932_L_-XXXXXXXXXX, s_XXXXXXXXXXXXX, S_XXXXXXXXXXXXX, T_XXXXXXXXXXXXX
  < Ground >
    - Ground : tex_PQ_Ground
    - Road   : tex_PQ_Road
  < SignBoard >
    - Road Sign : 005339_08932_25_14_TransP_0 - 005339_08932_25_14_TransP_2, 005339_08932_25_14_52,
                  005339_08932_L_-90129_0_01_01_1, 005339_08932_L_-101063_0_01_01_11, 005339_08932_L_-101410_0_01_01_14

2. Hierarchy Object Description
  -> Please see the file "Assets/ZRNAssets/005339_08932_25_14/Documents/JOC_ModelList_v100.pdf"


-----------------------------------
How to use Query-Chan
-----------------------------------
1. Import JapaneseOtakuCity.unitypackage to your Game project.

2. Find "PQ_Remake_AKIHABARA.fbx" in Models folder and Drop it to your Game scene (hierarchy).

3. Please see "Controller" GameObject in the demo scene(Sample_005339_08932_25_14) Hierarchy, if you would like to control city environment.


-----------------------------------
Demo Scene
-----------------------------------
1. You would open below scene file.
     - Sample_005339_08932_25_14.unity

2. Play game, you can change cameras, environment and ambients.
   You also can play Query-Chan FlyThrough and AI navigation car.
