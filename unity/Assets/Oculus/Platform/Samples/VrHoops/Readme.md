# Overview

This example uses basic Quickmatch and Peer-to-Peer networking to creating a cross-platform ball shooting game.
Quickmatch is used to find other players for a match and Networking is used to synchronize player
state such as movement of the balls.

# Application Setup

1. Open the Project in Unity 5.4.1p1 or later
2. Import the OculusPlatform Unity package (Main Menu -> Assets -> Import Package -> Custom Package)

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
2. Click Create Pool
3. For the Pool Key use: NORMAL_QUICKMATCH, or if you want to use a different Pool Key, update the constant in MatchmakingManager.cs
4. Choose Quickmatch mode
5. Enter 2 for Min Users and 3 for Max Users
6. Choose None for Skill Pool
7. Leave Advanced Quickmatch set to No
8. Leave Should Consider Ping Time? at the default setting of No
9. Don't add anything under Data Settings
10. Click Submit.

# Configure Leaderboards

This sample uses two Leaderboards to track player scores.  One leaderboard tracks the player that has
won the most games and another tracks who achieved the highest score in a single game.  Setup the leaderboards
using the following steps:

1. Navigate to your App Grouping section on the Developer Dashboard
2. Create a new leadername with the API NAME **MOST_MATCHES_WON** and sort order **Higher is Better**
3. Create a new leadername with the API NAME **HIGHEST_MATCH_SCORE** and sort order **Higher is Better**

# Configure Achievements

The sample updates an achievement that counts the number of times a player has won.  Follow these steps to create an
achievement that is unlocked when the player has won 10 matches:

1. Navigate to your App Grouping section on the Developer Dashboard
2. Click on the **Create Achievement** button
3. Set the API Name to **LIKES_TO_WIN**
4. Set an appropriate Title and Description
5. Leave the Write Policy as **CLIENT_AUTHORITATIVE**
6. Leave Is Achievement Secret untoggled
7. Set the Type to **Count**
7. Set the Target to *10*

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
