# Overview

This example demonstrates creating a Voice Chat application using
Oculus Platform Rooms (for invites), VOIP (sending and receiving
microphone input) and Peer-to-Peer networking (sharing headset
positions and rotations).  The application works on both Rift
and Gear VR.  Also, by using an Application Grouping, users will be
be able to chat cross-platform.

# Application Setup

1. Open the Project in Unity 5.4 or later
2. Import the OculusPlatform Unity package

## Rift
1. Create your Rift application on the dashboard
2. Copy the Application ID into the Project (Main Menu -> Oculus Platform -> Edit Settings -> Oculus Rift App Id)

## GearVR
1. Create the GearVR application on the dashboard
2. Move the GearVR application into the Rift application's App Grouping
3. Copy the Application ID into the Project (Main Menu -> Oculus Platform -> Edit Settings -> Gear VR App Id)
4. Copy the OSIG files for the GearVR devices you are testing to Assets\Plugins\Android\Assets

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
  1. Each apk you upload needs a new build number (Player Settings -> Other Settings)
  4. Add Friends you are testing with as Subscribed Users for the Alpha channel


