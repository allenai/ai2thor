/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Oculus.Interaction.InterfaceSupport
{
    public class InterfacePicker : EditorWindow
    {
        private class MonoInspector
        {
            public readonly MonoBehaviour Mono;
            public readonly Editor Editor;

            public MonoInspector(MonoBehaviour mono)
            {
                Mono = mono;
                Editor = Editor.CreateEditor(mono);
            }

            public void Destroy()
            {
                DestroyImmediate(Editor);
            }
        }

        private static class GUIStyles
        {
            public static readonly GUIStyle Default;
            public static readonly GUIStyle Window;
            public static readonly GUIStyle Inspector;

            private static readonly RectOffset padding =
                new RectOffset(EDGE_PADDING_PX,
                               EDGE_PADDING_PX,
                               EDGE_PADDING_PX,
                               EDGE_PADDING_PX);

            static GUIStyles()
            {
                Default = new GUIStyle();
                Window = new GUIStyle(Default);
                Window.padding = padding;
                Inspector = new GUIStyle(GUI.skin.window);
                Inspector.padding = padding;
            }
        }

        private const float SELECT_BUTTON_HEIGHT_PX = 32f;
        private const float LABEL_COLUMN_RATIO = 0.4f;
        private const int EDGE_PADDING_PX = 8;

        public static bool AnyOpen => HasOpenInstances<InterfacePicker>();

        private Object _target;
        private string _propertyPath;
        private List<MonoInspector> _monoInspectors;
        private Vector2 _scrollPos = Vector2.zero;

        public static void Show(SerializedProperty prop, List<MonoBehaviour> monos)
        {
            if (monos == null ||
                monos.Count == 0 ||
                prop == null)
            {
                return;
            }

            InterfacePicker picker = GetWindow<InterfacePicker>(true);

            picker._propertyPath = prop.propertyPath;
            picker._target = prop.serializedObject.targetObject;
            picker._monoInspectors?.ForEach((mi) => mi.Destroy());
            picker._monoInspectors = new List<MonoInspector>();
            picker.titleContent = new GUIContent(monos[0].gameObject.name);
            monos.ForEach((m) => picker._monoInspectors.Add(new MonoInspector(m)));

            picker.ShowUtility();
        }

        private void OnGUI()
        {
            if (_target == null)
            {
                Close();
                return;
            }

            Prune();
            DrawAll();
        }

        private void OnDestroy()
        {
            _monoInspectors.ForEach((mi) => mi.Destroy());
        }

        private void Prune()
        {
            _monoInspectors.FindAll((m) => m.Mono == null).ForEach((mi) =>
            {
                _monoInspectors.Remove(mi);
                mi.Destroy();
            });
        }

        private void DrawAll()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUIStyles.Window);
            foreach (var monoInspector in _monoInspectors)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical(GUIStyles.Inspector);
                DrawHeader(monoInspector);
                EditorGUILayout.Separator();
                DrawComponent(monoInspector);
                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader(MonoInspector monoInspector)
        {
            if (GUILayout.Button($"{monoInspector.Mono.GetType().Name}",
                GUILayout.Height(SELECT_BUTTON_HEIGHT_PX)))
            {
                Apply(monoInspector.Mono);
                Close();
            }
        }

        private void DrawComponent(MonoInspector monoInspector)
        {
            GUI.enabled = false;
            EditorGUIUtility.labelWidth = position.width * LABEL_COLUMN_RATIO;
            monoInspector.Editor.OnInspectorGUI();
            GUI.enabled = true;
        }

        private void Apply(MonoBehaviour mono)
        {
            SerializedProperty property =
                new SerializedObject(_target).FindProperty(_propertyPath);
            property.objectReferenceValue = mono;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
