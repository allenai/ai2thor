// Uncomment this if you have the Touch controller classes in your project
//#define USE_OVRINPUT

using System;
using Oculus.Platform;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * This class shows a very simple way to integrate setting the Group Presence
 * with a destination and how to respond to a user's app launch details that
 * include the destination they wish to travel to.
 */
public class GroupPresenceSample : MonoBehaviour
{
  /**
   * Sets extra fields on the rich presence
   */

  // A boolean to indicate whether the destination is joinable. You can check
  // the current capacity against the max capacity to determine whether the room
  // is joinable.
  public bool IsJoinable = true;

  // Users with the same destination + session ID are considered together by Oculus
  // Users with the same destination and different session IDs are not
  public string LobbySessionID;
  public string MatchSessionID;

  // Users can be suggested as part of the invite flow
  public UInt64 SuggestedUserID;

  public Text InVRConsole;
  public Text DestinationsConsole;

  private List<string> DestinationAPINames = new List<string>();
  private ulong LoggedInUserID = 0;

  // Start is called before the first frame update
  void Start()
  {
    UpdateConsole("Init Oculus Platform SDK...");
    Core.AsyncInitialize().OnComplete(message => {
      if (message.IsError)
      {
        // Init failed, nothing will work
        UpdateConsole(message.GetError().Message);
      }
      else
      {
        UpdateConsole("Init complete!");

        /**
         * Get and cache the Logged in User ID for future queries
         */
        Users.GetLoggedInUser().OnComplete(OnLoggedInUser);

        /**
         * Get the list of destinations defined for this app from the developer portal
         */
        RichPresence.GetDestinations().OnComplete(OnGetDestinations);

        /**
         * Listen for future join intents that might come in
         */
        GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntentChangeNotif);

        /**
         * Listen for future leave that might come in
         */
        GroupPresence.SetLeaveIntentReceivedNotificationCallback(OnLeaveIntentChangeNotif);

        /**
         * Listen for the list of users that the current users have invitted
         */
        GroupPresence.SetInvitationsSentNotificationCallback(OnInviteSentNotif);
      }
    });
  }

  /**
    * Setting the group presence
    */
  void SetPresence()
  {
    var options = new GroupPresenceOptions();

    options.SetDestinationApiName(DestinationAPINames[DestinationIndex]);

    if (!string.IsNullOrEmpty(MatchSessionID))
    {
      options.SetMatchSessionId(MatchSessionID);
    }

    if (!string.IsNullOrEmpty(MatchSessionID))
    {
      options.SetLobbySessionId(LobbySessionID);
    }

    // Set is Joinable to let other players deeplink and join this user via the presence
    options.SetIsJoinable(IsJoinable);

    UpdateConsole("Setting Group Presence to " + DestinationAPINames[DestinationIndex] + " ...");

    // Here we are setting the group presence then fetching it after we successfully set it
    GroupPresence.Set(options).OnComplete(message => {
      if (message.IsError)
      {
        UpdateConsole(message.GetError().Message);
      }
      else
      {
        // Note that Users.GetLoggedInUser() does not do a server fetch and will
        // not get an updated presence status
        Users.Get(LoggedInUserID).OnComplete(message2 =>
        {
          if (message2.IsError)
          {
            UpdateConsole("Success! But presence is unknown!");
          }
          else
          {
            UpdateConsole("Group Presence set to:\n" + message2.Data.Presence + "\n" + message2.Data.PresenceDeeplinkMessage + "\n" + message2.Data.PresenceDestinationApiName);
          }
        });
      }
    });
  }

  /**
    * Clearing the rich presence
    */
  void ClearPresence()
  {
    UpdateConsole("Clearing Group Presence...");
    GroupPresence.Clear().OnComplete(message => {
      if (message.IsError)
      {
        UpdateConsole(message.GetError().Message);
      }
      else
      {
        // Clearing the rich presence then fetching the user's presence afterwards
        Users.Get(LoggedInUserID).OnComplete(message2 =>
        {
          if (message2.IsError)
          {
            UpdateConsole("Group Presence cleared! But rich presence is unknown!");
          }
          else
          {
            UpdateConsole("Group Presence cleared!\n" + message2.Data.Presence + "\n");
          }
        });
      }
    });
  }

  /**
   * Launch the invite panel
   */
  void LaunchInvitePanel()
  {
    UpdateConsole("Launching Invite Panel...");
    var options = new InviteOptions();
    if (SuggestedUserID != 0)
    {
      options.AddSuggestedUser(SuggestedUserID);
    }
    GroupPresence.LaunchInvitePanel(options).OnComplete(message =>
    {
      if (message.IsError)
      {
        UpdateConsole(message.GetError().Message);
      }
    });
  }

  /**
   * Launch the roster panel
   */
  void LaunchRosterPanel()
  {
    UpdateConsole("Launching Roster Panel...");
    var options = new RosterOptions();
    if (SuggestedUserID != 0)
    {
      options.AddSuggestedUser(SuggestedUserID);
    }
    GroupPresence.LaunchRosterPanel(options).OnComplete(message =>
    {
      if (message.IsError)
      {
        UpdateConsole(message.GetError().Message);
      }
    });
  }

  // User has interacted with a deeplink outside this app
  void OnJoinIntentChangeNotif(Message<Oculus.Platform.Models.GroupPresenceJoinIntent> message)
  {
    if (message.IsError)
    {
      UpdateConsole(message.GetError().Message);
    } else
    {
      var joinIntent = message.Data;

      // The deeplink message, this should give enough info on how to go the
      // destination in the app.
      var deeplinkMessage = joinIntent.DeeplinkMessage;

      // The API Name of the destination. You can set the user to this after
      // navigating to the app
      var destinationApiName = joinIntent.DestinationApiName;
      var matchSessionID = joinIntent.MatchSessionId;
      var lobbySessionID = joinIntent.LobbySessionId;

      var detailsString = "-Deeplink Message:\n" + deeplinkMessage + "\n-Api Name:\n" + destinationApiName + "\n-Lobby Session Id:\n" + lobbySessionID + "\n-Match Session Id:\n" + matchSessionID;
      detailsString += "\n";
      UpdateConsole("Got updated Join Intent & setting the user's presence:\n" + detailsString);

      var options = new GroupPresenceOptions();

      if (!string.IsNullOrEmpty(destinationApiName))
      {
        options.SetDestinationApiName(destinationApiName);
      }

      if (!string.IsNullOrEmpty(matchSessionID))
      {
        options.SetMatchSessionId(matchSessionID);
      }

      if (!string.IsNullOrEmpty(lobbySessionID))
      {
        options.SetLobbySessionId(lobbySessionID);
      }
      GroupPresence.Set(options);
    }
  }

  // User has interacted with the roster to leave the current destination / lobby / match
  void OnLeaveIntentChangeNotif(Message<Oculus.Platform.Models.GroupPresenceLeaveIntent> message)
  {
    if (message.IsError)
    {
      UpdateConsole(message.GetError().Message);
    } else
    {
      var leaveIntent = message.Data;

      var destinationApiName = leaveIntent.DestinationApiName;
      MatchSessionID = leaveIntent.MatchSessionId;
      LobbySessionID = leaveIntent.LobbySessionId;

      var detailsString = "-Api Name:\n" + destinationApiName + "\n-Lobby Session Id:\n" + LobbySessionID + "\n-Match Session Id:\n" + MatchSessionID;
      detailsString += "\n";
      UpdateConsole("Clearing presence because user wishes to leave:\n" + detailsString);

      // User left
      GroupPresence.Clear();
    }
  }

  // User has invited users
  void OnInviteSentNotif(Message<Oculus.Platform.Models.LaunchInvitePanelFlowResult> message)
  {
    if (message.IsError)
    {
      UpdateConsole(message.GetError().Message);
    } else
    {
      var users = message.Data.InvitedUsers;
      var usersCount = users.Count;

      var usersInvitedString = "-Users:\n";
      if (usersCount > 0)
      {
        foreach(var user in users)
        {
          usersInvitedString += user.OculusID + "\n";
        }
      } else
      {
        usersInvitedString += "none\n";
      }

      UpdateConsole("Users sent invite to:\n" + usersInvitedString);
    }
  }

  void OnGetDestinations(Message<Oculus.Platform.Models.DestinationList> message)
  {
    if (message.IsError)
    {
      UpdateConsole("Could not get the list of destinations!");
    }
    else
    {
      foreach(Oculus.Platform.Models.Destination destination in message.Data)
      {
        DestinationAPINames.Add(destination.ApiName);
        UpdateDestinationsConsole();
      }
    }
  }

  #region Helper Functions

  private int DestinationIndex = 0;
  private bool OnlyPushUpOnce = false;
  // Update is called once per frame
  void Update()
  {
    if (PressAButton())
    {
      if (DestinationAPINames.Count > 0)
      {
        SetPresence();
      }
      else
      {
        UpdateConsole("No destinations to set to!");
        return;
      }
    }
    else if (PressBButton())
    {
      ClearPresence();
    }
    else if (PressXButton())
    {
      LaunchInvitePanel();
    }
    else if (PressYButton())
    {
      LaunchRosterPanel();
    }

    ScrollThroughDestinations();
  }

  private void ScrollThroughDestinations()
  {
    if (PressUp())
    {
      if (!OnlyPushUpOnce)
      {
        DestinationIndex--;
        if (DestinationIndex < 0)
        {
          DestinationIndex = DestinationAPINames.Count - 1;
        }
        OnlyPushUpOnce = true;
        UpdateDestinationsConsole();
      }
    }
    else if (PressDown())
    {
      if (!OnlyPushUpOnce)
      {
        DestinationIndex++;
        if (DestinationIndex >= DestinationAPINames.Count)
        {
          DestinationIndex = 0;
        }
        OnlyPushUpOnce = true;
        UpdateDestinationsConsole();
      }
    }
    else
    {
      OnlyPushUpOnce = false;
    }
  }

  private void UpdateDestinationsConsole()
  {
    if (DestinationAPINames.Count == 0)
    {
      DestinationsConsole.text = "Add some destinations to the developer dashboard first!";
    }
    string destinations = "Destination API Names:\n";
    for (int i = 0; i < DestinationAPINames.Count; i++)
    {
      if (i == DestinationIndex)
      {
        destinations += "==>";
      }
      destinations += DestinationAPINames[i] + "\n";
    }
    DestinationsConsole.text = destinations;
  }

  private void OnLoggedInUser(Message<Oculus.Platform.Models.User> message)
  {
    if (message.IsError)
    {
      Debug.LogError("Cannot get logged in user");
    }
    else
    {
      LoggedInUserID = message.Data.ID;
    }
  }

  private void UpdateConsole(string value)
  {
    Debug.Log(value);

    InVRConsole.text =
      "Scroll Up/Down on Right Thumbstick\n(A) - Set Group Presence to selected\n(B) - Clear Group Presence\n(X) - Launch Invite Panel\n(Y) - Launch Roster Panel\n\n" + value;
  }

  #endregion

  #region I/O Inputs
  private bool PressAButton()
  {
#if USE_OVRINPUT
    return OVRInput.GetUp(OVRInput.Button.One) || Input.GetKeyUp(KeyCode.A);
#else
    return Input.GetKeyUp(KeyCode.A);
#endif
  }

  private bool PressBButton()
  {
#if USE_OVRINPUT
    return OVRInput.GetUp(OVRInput.Button.Two) || Input.GetKeyUp(KeyCode.B);
#else
    return Input.GetKeyUp(KeyCode.B);
#endif
  }

  private bool PressXButton()
  {
#if USE_OVRINPUT
    return OVRInput.GetUp(OVRInput.Button.Three) || Input.GetKeyUp(KeyCode.X);
#else
    return Input.GetKeyUp(KeyCode.X);
#endif
  }

  private bool PressYButton()
  {
#if USE_OVRINPUT
    return OVRInput.GetUp(OVRInput.Button.Four) || Input.GetKeyUp(KeyCode.Y);
#else
    return Input.GetKeyUp(KeyCode.Y);
#endif
  }

  private bool PressUp()
  {
#if USE_OVRINPUT
    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
    return (axis.y > 0.2 || Input.GetKeyUp(KeyCode.UpArrow));
#else
    return Input.GetKeyUp(KeyCode.UpArrow);
#endif
  }

  private bool PressDown()
  {
#if USE_OVRINPUT
    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
    return (axis.y < -0.2 || Input.GetKeyUp(KeyCode.DownArrow));
#else
    return Input.GetKeyUp(KeyCode.DownArrow);
#endif
  }

  #endregion
}
