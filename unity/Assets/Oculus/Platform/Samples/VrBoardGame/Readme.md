# Overview

This example demonstrates using the Oculus In-App-Purchase API and skill based matchmaking.  
The setting is a simple boardgame (which you are encourage to chage to your creative idea!)
on a 3x3 grid with two pieces and one special 'power-piece' that can be purchased with
IAP through the Oculus Store.  After an Online match is completed the ranking is sent to
the Matchmaking Service so that following match selections will take into account a user's
skill level.

# Application Setup

1. Open the Project in Unity 5.4.1p1 or later
2. Import the OculusPlatform Unity package
  - Unity: Main Menu -> Assets -> Import Package -> Custom Package
  - SDK Location: Unity/OculusPlatform.unitypackage

## Rift
1. Create your Rift application on the Oculus Developer Dashboard
2. Copy the Application ID into the Project (Main Menu -> Oculus Platform -> Edit Settings -> Oculus Rift App Id)

## GearVR
1. Create the GearVR application on the Oculus Developer Dashboard
2. Move the GearVR application into the Rift application's App Grouping
3. Copy the Application ID into the Project (Main Menu -> Oculus Platform -> Edit Settings -> Gear VR App Id)
4. Copy the OSIG files for the GearVR devices you are testing to Assets\Plugins\Android\Assets

# Configure Matchmaking

1. On the Oculus Dashboard, navigate to the Matchmaking section for your App Grouping
2. Change the option box from 'Pools' to 'Skill Pools'
3. Click Create Pool
4. Set the 'Skill Pool Key' to ''VR_BOARD_GAME''
5. Select ''Medium'' for the 'Luck Factor'
6. Enter ''0'' for the 'Draw Probability
7. Click 'Save & Deploy'
8. Change the option box 'Skill Pools' to 'Pools'
9. Click Create Pool
10. Set the 'Pool Key' to ''VR_BOARD_GAME_POOL''
11. Set the Mode to Quickmatch
12. Enter ''2'' for both the Min and Max Users
13. Select ''VR_BOARD_GAME'' for the 'Skill Pool'
14. Leave 'Advanced Quickmatch' set to ''No''
15. Leave 'Should Consider Ping Time?' at the default setting of ''No''
16. Click 'Save & Deploy'

# Configure IAP

1. On the Oculus Dashboard, make sure the Payment Info is setup for your Organization
2. Navigate to the IAP tab under your App Grouping
3. Select the Upload TSV button and choose the Oculus_IAP.tsv in the project root directory.

# Upload your builds

Build executables from Unity and upload them to your Application Dashboard
* Rift
  1. Add the executable and data folder to a zip file
  2. Upload the zip to the Alpha channel on your Dashboard
  3. Set the executable name you chose in the zip file
  4. Add Friends you are testing with as Subscribed Users for the Alpha channel
* GearVR
  1. Create an android keystore (if you don't have one) so Unity can sign the build. (Player Settings -> Publishing Settings)
  2. Upload the apk to the Alpha channel on your Dashboard
  3. Each apk you upload needs a new build number (Player Settings -> Other Settings)
  4. Add Friends you are testing with as Subscribed Users for the Alpha channel
