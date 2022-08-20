/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// Transforms position from world space to local space
    /// </summary>
    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        return worldToLocal.MultiplyPoint3x4(position);
    }

    /// <summary>
    /// Transforms position from local space to world space
    /// </summary>
    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        return localToWorld.MultiplyPoint3x4(position);
    }
}
