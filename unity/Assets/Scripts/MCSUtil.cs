using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Utility functions helpful for general use
/// </summary>
public class MCSUtil
{
    /// <summary>
    /// Returns if the application was run with a specific argument
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool HasArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Closes the current application
    /// </summary>
    public static void CloseApplication()
    {
        Application.Quit();
    }
}