/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Reflection;
using System.Threading;
using Facebook.WitAi.Utilities;
using UnityEngine;

namespace Facebook.WitAi
{
    internal class RegisteredMatchIntent
    {
        public Type type;
        public MethodInfo method;
        public MatchIntent matchIntent;
    }

    internal static class MatchIntentRegistry
    {
        private static DictionaryList<string, RegisteredMatchIntent> registeredMethods;

        public static DictionaryList<string, RegisteredMatchIntent> RegisteredMethods
        {
            get
            {
                if (null == registeredMethods)
                {
                    // Note, first run this will not return any values. Initialize
                    // scans assemblies on a different thread. This is ok for voice
                    // commands since they are generally executed in realtime after
                    // initialization is complete. This is a perf optimization.
                    //
                    // Best practice is to call Initialize in Awake of any method
                    // that will be using the resulting data.
                    Initialize();
                }

                return registeredMethods;
            }
        }

        internal static void Initialize()
        {
            if (null != registeredMethods) return;
            registeredMethods = new DictionaryList<string, RegisteredMatchIntent>();
            new Thread(RefreshAssemblies).Start();
        }

        internal static void RefreshAssemblies()
        {
            // TODO: We could potentially build this list at compile time and cache it
            // Work on a local dictionary to avoid thread complications
            var dictionary = new DictionaryList<string, RegisteredMatchIntent>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try {
                    foreach (Type t in assembly.GetTypes()) {
                        try {
                            foreach (var method in t.GetMethods()) {
                                try {
                                    foreach (var attribute in method.GetCustomAttributes(typeof(MatchIntent))) {
                                        try {
                                            var mi = (MatchIntent)attribute;
                                            dictionary[mi.Intent].Add(new RegisteredMatchIntent() {
                                                type = t,
                                                method = method,
                                                matchIntent = mi
                                            });
                                        } catch (Exception e) {
                                            Debug.LogError(e);
                                        }
                                    }
                                } catch (Exception e) {
                                    Debug.LogError(e);
                                }
                            }
                        } catch (Exception e) {
                            Debug.LogError(e);
                        }
                    }
                } catch (Exception e) {
                    Debug.LogError(e);
                }
            }

            registeredMethods = dictionary;
        }
    }
}
