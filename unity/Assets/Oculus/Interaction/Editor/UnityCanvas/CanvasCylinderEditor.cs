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

using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.UnityCanvas.Editor
{
    [CustomEditor(typeof(CanvasCylinder))]
    public class CanvasCylinderEditor : UnityEditor.Editor
    {
        private SerializedProperty _meshColliderProp;

        private void OnEnable()
        {
            _meshColliderProp = serializedObject.FindProperty("_meshCollider");
        }

        public override void OnInspectorGUI()
        {
            CanvasCylinder canvasCylinder = target as CanvasCylinder;

            if (canvasCylinder != null)
            {
                if (canvasCylinder.Cylinder != null &&
                    canvasCylinder.Cylinder.transform.IsChildOf(canvasCylinder.transform))
                {
                    EditorGUILayout.HelpBox($"{typeof(CanvasCylinder).Name} must be " +
                        $"a child or sibling of its {typeof(Cylinder).Name}", MessageType.Error);
                }

                if (_meshColliderProp != null &&
                    _meshColliderProp.objectReferenceValue is MeshCollider col &&
                    canvasCylinder.transform != col.transform &&
                    canvasCylinder.transform.IsChildOf(col.transform))
                {
                    EditorGUILayout.HelpBox($"{typeof(CanvasCylinder).Name} cannot be a " +
                        $"child of its {typeof(MeshCollider).Name}. It must be a parent, " +
                        $"sibling, or share a GameObject.", MessageType.Error);
                }
            }

            base.OnInspectorGUI();
        }
    }
}
