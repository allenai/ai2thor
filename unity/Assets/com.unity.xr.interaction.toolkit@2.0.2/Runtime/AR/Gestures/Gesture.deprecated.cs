//-----------------------------------------------------------------------
// <copyright file="Gesture.cs" company="Google">
//
// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

// Modifications copyright © 2020 Unity Technologies ApS

#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION

using System;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    public abstract partial class Gesture<T> where T : Gesture<T>
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="isCanceled"/>
        [Obsolete("WasCancelled has been deprecated. Use isCanceled instead. (UnityUpgradable) -> isCanceled")]
        public bool WasCancelled => isCanceled;

        /// <inheritdoc cref="targetObject"/>
        [Obsolete("TargetObject has been deprecated. Use targetObject instead. (UnityUpgradable) -> targetObject")]
        public GameObject TargetObject
        {
            get => targetObject;
            protected set => targetObject = value;
        }

        /// <inheritdoc cref="recognizer"/>
        [Obsolete("m_Recognizer has been deprecated. Use recognizer instead. (UnityUpgradable) -> recognizer")]
        // ReSharper disable once InconsistentNaming -- Deprecated
        protected internal GestureRecognizer<T> m_Recognizer => recognizer;
#pragma warning restore IDE1006 // Naming Styles
    }
}

#endif
