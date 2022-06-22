//-----------------------------------------------------------------------
// <copyright file="Manipulator.cs" company="Google">
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
    public abstract partial class ARBaseGestureInteractable
    {
        [SerializeField]
        ARSessionOrigin m_ARSessionOrigin;

        /// <summary>
        /// The <a href="https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/api/UnityEngine.XR.ARFoundation.ARSessionOrigin.html">ARSessionOrigin</a>
        /// that this Interactable will use (such as to get the [Camera](xref:UnityEngine.Camera)
        /// or to transform from Session space). Will find one if <see langword="null"/>.
        /// </summary>
        [Obsolete("arSessionOrigin is marked for deprecation and will be removed in a future version. Please use xrOrigin instead.")]
        public ARSessionOrigin arSessionOrigin
        {
            get => m_ARSessionOrigin;
            set => m_ARSessionOrigin = value;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <c>IsHoverableBy(XRBaseInteractor)</c> has been deprecated. Use <see cref="IsHoverableBy(IXRHoverInteractor)"/> instead.
        /// </remarks>
        [Obsolete("IsHoverableBy(XRBaseInteractor) has been deprecated. Use IsHoverableBy(IXRHoverInteractor) instead.")]
        public override bool IsHoverableBy(XRBaseInteractor interactor) => IsHoverableBy((IXRHoverInteractor)interactor);

        /// <inheritdoc />
        /// <remarks>
        /// <c>IsSelectableBy(XRBaseInteractor)</c> has been deprecated. Use <see cref="IsSelectableBy(IXRSelectInteractor)"/> instead.
        /// </remarks>
        [Obsolete("IsSelectableBy(XRBaseInteractor) has been deprecated. Use IsSelectableBy(IXRSelectInteractor) instead.")]
        public override bool IsSelectableBy(XRBaseInteractor interactor) => IsSelectableBy((IXRSelectInteractor)interactor);
    }
}

#endif
