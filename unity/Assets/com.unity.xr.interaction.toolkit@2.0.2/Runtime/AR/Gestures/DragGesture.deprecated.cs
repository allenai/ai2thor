//-----------------------------------------------------------------------
// <copyright file="DragGesture.cs" company="Google">
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
    public partial class DragGesture
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="fingerId"/>
        /// <remarks>
        /// <c>FingerId</c> has been deprecated. Use <see cref="fingerId"/> instead.
        /// </remarks>
        [Obsolete("FingerId has been deprecated. Use fingerId instead. (UnityUpgradable) -> fingerId")]
        public int FingerId => fingerId;

        /// <inheritdoc cref="startPosition"/>
        /// <remarks>
        /// <c>StartPosition</c> has been deprecated. Use <see cref="startPosition"/> instead.
        /// </remarks>
        [Obsolete("StartPosition has been deprecated. Use startPosition instead. (UnityUpgradable) -> startPosition")]
        public Vector2 StartPosition => startPosition;

        /// <inheritdoc cref="position"/>
        /// <remarks>
        /// <c>Position</c> has been deprecated. Use <see cref="position"/> instead.
        /// </remarks>
        [Obsolete("Position has been deprecated. Use position instead. (UnityUpgradable) -> position")]
        public Vector2 Position => position;

        /// <inheritdoc cref="delta"/>
        /// <remarks>
        /// <c>Delta</c> has been deprecated. Use <see cref="delta"/> instead.
        /// </remarks>
        [Obsolete("Delta has been deprecated. Use delta instead. (UnityUpgradable) -> delta")]
        public Vector2 Delta => delta;
#pragma warning restore IDE1006
    }
}

#endif
