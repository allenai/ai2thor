namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	// Top level class for initializing the Oculus Platform SDK.  It also performs
	// and entitlement check and returns information about the logged-in user.
	public class PlatformManager : MonoBehaviour
	{
		private static PlatformManager s_instance;

		// my Application-scoped Oculus ID
		private ulong m_myID;

		// my Oculus user name
		private string m_myOculusID;

		#region Initialization and Shutdown

		void Awake()
		{
			// make sure only one instance of this manager ever exists
			if (s_instance != null)
			{
				Destroy(gameObject);
				return;
			}

			s_instance = this;
			DontDestroyOnLoad(gameObject);

			Core.Initialize();
		}

		void Start()
		{
			// First thing we should do is perform an entitlement check to make sure
			// we successfully connected to the Oculus Platform Service.
			Entitlements.IsUserEntitledToApplication().OnComplete(IsEntitledCallback);
		}

		void IsEntitledCallback(Message msg)
		{
			if (msg.IsError)
			{
				TerminateWithError(msg);
				return;
			}

			// Next get the identity of the user that launched the Application.
			Users.GetLoggedInUser().OnComplete(GetLoggedInUserCallback);
		}

		void GetLoggedInUserCallback(Message<User> msg)
		{
			if (msg.IsError)
			{
				TerminateWithError(msg);
				return;
			}

			m_myID = msg.Data.ID;
			m_myOculusID = msg.Data.OculusID;

			Debug.Log(" I am " + m_myOculusID);
		}

		// In this example, for most errors, we terminate the Application.  A full App would do
		// something more graceful.
		public static void TerminateWithError(Message msg)
		{
			Debug.Log("Error: " + msg.GetError().Message);
			UnityEngine.Application.Quit();
		}

		#endregion

		#region Properties

		public static ulong MyID
		{
			get
			{
				if (s_instance != null)
				{
					return s_instance.m_myID;
				}
				else
				{
					return 0;
				}
			}
		}

		public static string MyOculusID
		{
			get
			{
				if (s_instance != null && s_instance.m_myOculusID != null)
				{
					return s_instance.m_myOculusID;
				}
				else
				{
					return string.Empty;
				}
			}
		}

		#endregion
	}
}
