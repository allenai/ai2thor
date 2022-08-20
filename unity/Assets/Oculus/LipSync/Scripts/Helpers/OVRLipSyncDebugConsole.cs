/************************************************************************************
Filename    :   OVRLipSyncDebugConsole.cs
Content     :   Write to a text string, used by UI.Text
Created     :   May 22, 2015
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OVRLipSyncDebugConsole : MonoBehaviour
{
    public ArrayList messages = new ArrayList();
    public int       maxMessages = 15;             // The max number of messages displayed
    public Text      textMsg;                      // text string to display

    // Our instance to allow this script to be called without a direct connection.
    private static OVRLipSyncDebugConsole s_Instance = null;

    // Clear timeout
    private bool     clearTimeoutOn = false;
    private float    clearTimeout   = 0.0f;

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static OVRLipSyncDebugConsole instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType(typeof(OVRLipSyncDebugConsole)) as OVRLipSyncDebugConsole;

                if (s_Instance == null)
                {
                    GameObject console = new GameObject();
                    console.AddComponent<OVRLipSyncDebugConsole>();
                    console.name = "OVRLipSyncDebugConsole";
                    s_Instance = FindObjectOfType(typeof(OVRLipSyncDebugConsole)) as OVRLipSyncDebugConsole;
                }
            }

            return s_Instance;
        }
    }

      /// <summary>
      /// Awake this instance.
      /// </summary>
    void Awake()
    {
        s_Instance = this;
        Init();

    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    void Update()
    {
        if(clearTimeoutOn == true)
        {
            clearTimeout -= Time.deltaTime;
            if(clearTimeout < 0.0f)
            {
                Clear();
                clearTimeout = 0.0f;
                clearTimeoutOn = false;
            }
        }
    }

    /// <summary>
    /// Init this instance.
    /// </summary>
    public void Init()
    {
        if(textMsg == null)
        {
            Debug.LogWarning("DebugConsole Init WARNING::UI text not set. Will not be able to display anything.");
        }

        Clear();
    }


    //+++++++++ INTERFACE FUNCTIONS ++++++++++++++++++++++++++++++++

    /// <summary>
    /// Log the specified message.
    /// </summary>
    /// <param name="message">Message.</param>
    public static void Log(string message)
    {
        OVRLipSyncDebugConsole.instance.AddMessage(message, Color.white);
    }

    /// <summary>
    /// Log the specified message and color.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="color">Color.</param>
    public static void Log(string message, Color color)
    {
        OVRLipSyncDebugConsole.instance.AddMessage(message, color);
    }

    /// <summary>
    /// Clear this instance.
    /// </summary>
    public static void Clear()
    {
        OVRLipSyncDebugConsole.instance.ClearMessages();
    }

    /// <summary>
    /// Calls clear after a certain time.
    /// </summary>
    /// <param name="timeToClear">Time to clear.</param>
    public static void ClearTimeout(float timeToClear)
    {
        OVRLipSyncDebugConsole.instance.SetClearTimeout(timeToClear);
    }

    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


    /// <summary>
    /// Adds the message.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="color">Color.</param>
    public void AddMessage(string message, Color color)
    {
        messages.Add(message);

        if(textMsg != null)
            textMsg.color = color;

        Display();
    }

    /// <summary>
    /// Clears the messages.
    /// </summary>
    public void ClearMessages()
    {
        messages.Clear();
        Display();
    }

    /// <summary>
    /// Sets the clear timeout.
    /// </summary>
    /// <param name="timeout">Timeout.</param>
    public void SetClearTimeout(float timeout)
    {
        clearTimeout   = timeout;
        clearTimeoutOn = true;
    }

    /// <summary>
    // Prunes the array to fit within the maxMessages limit
    /// </summary>
    void Prune()
    {
        int diff;
        if (messages.Count > maxMessages)
        {
            if (messages.Count <= 0)
            {
                diff = 0;
            }
            else
            {
                diff = messages.Count - maxMessages;
            }
            messages.RemoveRange(0, (int)diff);
        }
    }

    /// <summary>
    /// Display this instance.
    /// </summary>
    void Display()
    {
        if (messages.Count > maxMessages)
        {
            Prune();
        }

        if(textMsg != null)
        {
            textMsg.text = ""; // Clear text out
            int x = 0;

            while (x < messages.Count)
            {
                     textMsg.text += (string)messages[x];
                    textMsg.text +='\n';
                    x += 1;
            }
        }
    }
}
