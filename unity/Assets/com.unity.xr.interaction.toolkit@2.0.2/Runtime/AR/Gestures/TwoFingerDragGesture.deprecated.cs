//-----------------------------------------------------------------------
// <copyright file="TwoFingerDragGesture.cs" company="Google">
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
    public partial class TwoFingerDragGesture
    {
#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="fingerId1"/>
        /// <remarks>
        /// <c>FingerId1</c> has been deprecated. Use <see cref="fingerId1"/> instead.
        /// </remarks>
        [Obsolete("FingerId1 has been deprecated. Use fingerId1 instead. (UnityUpgradable) -> fingerId1")]
        public int FingerId1 => fingerId1;

        /// <inheritdoc cref="fingerId2"/>
        /// <remarks>
        /// <c>FingerId2</c> has been deprecated. Use <see cref="fingerId2"/> instead.
        /// </remarks>
        [Obsolete("FingerId2 has been deprecated. Use fingerId2 instead. (UnityUpgradable) -> fingerId2")]
        public int FingerId2 => fingerId2;

        /// <inheritdoc cref="startPosition1"/>
        /// <remarks>
        /// <c>StartPosition1</c> has been deprecated. Use <see cref="startPosition1"/> instead.
        /// </remarks>
        [Obsolete("StartPosition1 has been deprecated. Use startPosition1 instead. (UnityUpgradable) -> startPosition1")]
        public Vector2 StartPosition1 => startPosition1;

        /// <inheritdoc cref="startPosition2"/>
        /// <remarks>
        /// <c>StartPosition2</c> has been deprecated. Use <see cref="startPosition2"/> instead.
        /// </remarks>
        [Obsolete("StartPosition2 has been deprecated. Use startPosition2 instead. (UnityUpgradable) -> startPosition2")]
        public Vector2 StartPosition2 => startPosition2;

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
#pragma warning restore IDE1006 // Naming Styles
    }
}

#endif
