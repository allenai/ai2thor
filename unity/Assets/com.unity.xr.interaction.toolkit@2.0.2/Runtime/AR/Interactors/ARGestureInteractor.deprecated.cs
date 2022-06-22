//-----------------------------------------------------------------------
// <copyright file="ARGestureInteractor.cs" company="Google">
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
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    public partial class ARGestureInteractor
    {
        [SerializeField]
        ARSessionOrigin m_ARSessionOrigin;

        /// <summary>
        /// The <a href="https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/api/UnityEngine.XR.ARFoundation.ARSessionOrigin.html">ARSessionOrigin</a>
        /// that this Interactor will use (such as to get the <see cref="Camera"/>
        /// or to transform from Session space). Will find one if <see langword="null"/>.
        /// </summary>
        [Obsolete("arSessionOrigin is marked for deprecation and will be removed in a future version. Please use xrOrigin instead.")]
        public ARSessionOrigin arSessionOrigin
        {
            get => m_ARSessionOrigin;
            set
            {
                m_ARSessionOrigin = value;
                if (Application.isPlaying)
                    PushARSessionOrigin();
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        static ARGestureInteractor s_Instance;
        /// <summary>
        /// (Read Only) The <see cref="ARGestureInteractor"/> instance.
        /// </summary>
        /// <remarks>
        /// <c>instance</c> has been deprecated. Use <see cref="ARBaseGestureInteractable.gestureInteractor"/> instead of singleton.
        /// </remarks>
        [Obsolete("instance has been deprecated. Use ARBaseGestureInteractable.gestureInteractor instead of singleton.")]
        public static ARGestureInteractor instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<ARGestureInteractor>();
                    if (s_Instance == null)
                    {
                        Debug.LogError("No instance of ARGestureInteractor exists in the scene.");
                    }
                }

                return s_Instance;
            }
        }

        /// <inheritdoc cref="instance"/>
        /// <remarks>
        /// <c>Instance</c> has been deprecated. Use <see cref="instance"/> instead.
        /// </remarks>
        [Obsolete("Instance has been deprecated. Use instance instead. (UnityUpgradable) -> instance")]
        public static ARGestureInteractor Instance => instance;

        /// <inheritdoc cref="dragGestureRecognizer"/>
        /// <remarks><c>DragGestureRecognizer</c> has been deprecated. Use <see cref="dragGestureRecognizer"/> instead.</remarks>
        [Obsolete("DragGestureRecognizer has been deprecated. Use dragGestureRecognizer instead. (UnityUpgradable) -> dragGestureRecognizer")]
        public DragGestureRecognizer DragGestureRecognizer => dragGestureRecognizer;

        /// <inheritdoc cref="pinchGestureRecognizer"/>
        /// <remarks><c>PinchGestureRecognizer</c> has been deprecated. Use <see cref="pinchGestureRecognizer"/> instead.</remarks>
        [Obsolete("PinchGestureRecognizer has been deprecated. Use pinchGestureRecognizer instead. (UnityUpgradable) -> pinchGestureRecognizer")]
        public PinchGestureRecognizer PinchGestureRecognizer => pinchGestureRecognizer;

        /// <inheritdoc cref="twoFingerDragGestureRecognizer"/>
        /// <remarks><c>TwoFingerDragGestureRecognizer</c> has been deprecated. Use <see cref="twoFingerDragGestureRecognizer"/> instead.</remarks>
        [Obsolete("TwoFingerDragGestureRecognizer has been deprecated. Use twoFingerDragGestureRecognizer instead. (UnityUpgradable) -> twoFingerDragGestureRecognizer")]
        public TwoFingerDragGestureRecognizer TwoFingerDragGestureRecognizer => twoFingerDragGestureRecognizer;

        /// <inheritdoc cref="tapGestureRecognizer"/>
        /// <remarks><c>TapGestureRecognizer</c> has been deprecated. Use <see cref="tapGestureRecognizer"/> instead.</remarks>
        [Obsolete("TapGestureRecognizer has been deprecated. Use tapGestureRecognizer instead. (UnityUpgradable) -> tapGestureRecognizer")]
        public TapGestureRecognizer TapGestureRecognizer => tapGestureRecognizer;

        /// <inheritdoc cref="twistGestureRecognizer"/>
        /// <remarks><c>TwistGestureRecognizer</c> has been deprecated. Use <see cref="twistGestureRecognizer"/> instead.</remarks>
        [Obsolete("TwistGestureRecognizer has been deprecated. Use twistGestureRecognizer instead. (UnityUpgradable) -> twistGestureRecognizer")]
        public TwistGestureRecognizer TwistGestureRecognizer => twistGestureRecognizer;
#pragma warning restore IDE1006

        /// <summary>
        /// Passes the <see cref="arSessionOrigin"/> to the Gesture Recognizers.
        /// </summary>
        /// <seealso cref="GestureRecognizer{T}.arSessionOrigin"/>
        [Obsolete("PushARSessionOrigin has been deprecated. Use PushXROrigin instead for similar functionality.")]
        protected virtual void PushARSessionOrigin()
        {
            dragGestureRecognizer.arSessionOrigin = m_ARSessionOrigin;
            pinchGestureRecognizer.arSessionOrigin = m_ARSessionOrigin;
            twoFingerDragGestureRecognizer.arSessionOrigin = m_ARSessionOrigin;
            tapGestureRecognizer.arSessionOrigin = m_ARSessionOrigin;
            twistGestureRecognizer.arSessionOrigin = m_ARSessionOrigin;
        }
    }
}
#endif
