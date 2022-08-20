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

using Oculus.Interaction.Editor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using props = Oculus.Interaction.UnityCanvas.CanvasRenderTexture.Properties;

namespace Oculus.Interaction.UnityCanvas.Editor
{
    [CustomEditor(typeof(CanvasRenderTexture))]
    public class CanvasRenderTextureEditor : EditorBase
    {
        private const string DEFAULT_UI_NAME = "UI/Default";

        private static List<CanvasRenderer> _tmpRenderers = new List<CanvasRenderer>();
        private static List<Graphic> _tmpGraphics = new List<Graphic>();

        public new CanvasRenderTexture target
        {
            get
            {
                return base.target as CanvasRenderTexture;
            }
        }

        private Func<bool> _isOverlayMode = () => false;

        protected override void OnEnable()
        {
            var renderingMode = serializedObject.FindProperty(props.RenderingMode);
            _isOverlayMode = () => renderingMode.intValue == (int)RenderingMode.OVR_Overlay ||
                                   renderingMode.intValue == (int)RenderingMode.OVR_Underlay;

            Draw(props.Resolution, props.DimensionDriveMode, (resProp, modeProp) =>
            {
                Rect rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);

                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;

                Rect dropdownRect = rect;
                dropdownRect.x = rect.xMax - 70;
                dropdownRect.width = 70;

                Rect contentRect = rect;
                contentRect.xMin = labelRect.xMax;
                contentRect.xMax = dropdownRect.xMin;

                GUI.Label(labelRect, resProp.displayName);

                if (modeProp.intValue == (int)CanvasRenderTexture.DriveMode.Auto)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.Vector2IntField(contentRect, GUIContent.none, target.CalcAutoResolution());
                    }
                }
                else
                {
                    resProp.vector2IntValue = EditorGUI.Vector2IntField(contentRect, GUIContent.none, resProp.vector2IntValue);
                }

                EditorGUI.PropertyField(dropdownRect, modeProp, GUIContent.none);
            });

            Draw(props.PixelsPerUnit, p =>
            {
                var driveMode = serializedObject.FindProperty(props.DimensionDriveMode);
                if (driveMode.intValue == (int)CanvasRenderTexture.DriveMode.Auto)
                {
                    EditorGUILayout.PropertyField(p);
                }
            });

            Draw(props.GenerateMips, p =>
            {
                if (!_isOverlayMode())
                {
                    EditorGUILayout.PropertyField(p);
                }
            });

            Draw(props.UseAlphaToMask, props.AlphaCutoutThreshold, (maskProp, cutoutProp) =>
             {
                 if (renderingMode.intValue == (int)RenderingMode.AlphaCutout)
                 {
                     EditorGUILayout.PropertyField(maskProp);

                     if (maskProp.boolValue == false)
                     {
                         EditorGUILayout.PropertyField(cutoutProp);
                     }
                 }
             });

            Draw(props.UseExpensiveSuperSample, props.EmulateWhileInEditor, props.DoUnderlayAA, (sampleProp, emulateProp, aaProp) =>
            {
                if (_isOverlayMode())
                {
                    EditorGUILayout.PropertyField(sampleProp);

                    if (renderingMode.intValue == (int)RenderingMode.OVR_Underlay)
                    {
                        EditorGUILayout.PropertyField(aaProp);
                    }

                    EditorGUILayout.PropertyField(emulateProp);
                }
            });
        }

        protected override void OnBeforeInspector()
        {
            base.OnBeforeInspector();

            bool isEmpty;

            AutoFix(AutoFixIsUsingScreenSpaceCanvas(), AutoFixSetToWorldSpaceCamera, "The OverlayRenderer only supports Canvases that are set to World Space.");
            AutoFix(AutoFixIsUsingDefaultUIShader(), AutoFixAssignOverlayShaderToGraphics, "Some Canvas Graphics are using the default UI shader, which has rendering issues when combined with Overlay rendering.");

            AutoFix(isEmpty = AutoFixIsMaskEmpty(), AutoFixAssignUIToMask, "The rendering Mask is empty, it needs to contain at least one layer for rendering to function.");

            if (!isEmpty)
            {
                AutoFix(AutoFixAnyCamerasRenderingTargetLayers(), AutoFixRemoveRenderingMaskFromCameras, "Some cameras are rendering using a layer that is specified here as a Rendering layer. This can cause the UI to be rendered twice.");
                AutoFix(AutoFixAnyRenderersOnUnrenderedLayers(), AutoFixMoveRenderersToMaskedLayers, "Some CanvasRenderers are using a layer that is not included in the rendered LayerMask.");
            }
        }

        #region AutoFix

        private bool AutoFix(bool needsFix, Action fixAction, string message)
        {
            if (needsFix)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
                    if (GUILayout.Button("Auto-Fix", GUILayout.ExpandHeight(true)))
                    {
                        fixAction();
                    }
                }
            }

            return needsFix;
        }

        private bool AutoFixIsUsingScreenSpaceCanvas()
        {
            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            return canvas.renderMode != RenderMode.WorldSpace;
        }

        private void AutoFixSetToWorldSpaceCamera()
        {
            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas != null)
            {
                Undo.RecordObject(canvas, "Set Canvas To World Space");
                canvas.renderMode = RenderMode.WorldSpace;
            }
        }

        private bool AutoFixIsUsingDefaultUIShader()
        {
            if (UnityInfo.Version_2020_3_Or_Newer())
            {
                return false;
            }

            if (target.DefaultUIMaterial == null)
            {
                return false;
            }

            target.GetComponentsInChildren(_tmpGraphics);
            foreach (var graphic in _tmpGraphics)
            {
                if (AutoFixIsExceptionCaseForDefaultUIShader(graphic))
                {
                    continue;
                }

                Material mat = graphic.material;
                if (mat == null)
                {
                    return true;
                }

                Shader shader = mat.shader;
                if (shader == null)
                {
                    continue;
                }

                if (shader.name == DEFAULT_UI_NAME)
                {
                    return true;
                }
            }

            return false;
        }

        private void AutoFixAssignOverlayShaderToGraphics()
        {
            target.GetComponentsInChildren(_tmpGraphics);
            foreach (var graphic in _tmpGraphics)
            {
                if (AutoFixIsExceptionCaseForDefaultUIShader(graphic))
                {
                    continue;
                }

                Material mat = graphic.material;
                if (mat == null)
                {
                    Undo.RecordObject(graphic, "Set Graphic Material");
                    graphic.material = target.DefaultUIMaterial;
                    EditorUtility.SetDirty(graphic);
                    continue;
                }

                Shader shader = mat.shader;
                if (shader == null)
                {
                    continue;
                }

                if (shader.name == DEFAULT_UI_NAME)
                {
                    Undo.RecordObject(graphic, "Set Graphic Material");
                    graphic.material = target.DefaultUIMaterial;
                    EditorUtility.SetDirty(graphic);
                    continue;
                }
            }
        }

        private bool AutoFixIsExceptionCaseForDefaultUIShader(Graphic graphic)
        {
            //Hardcoded edge-cases
            return graphic.GetType().Namespace == "TMPro" ||
                   graphic.GetType().Name == "OCText";
        }

        private bool AutoFixIsMaskEmpty()
        {
            var layerProp = serializedObject.FindProperty(props.RenderLayers);
            return layerProp.intValue == 0;
        }

        public void AutoFixAssignUIToMask()
        {
            Undo.RecordObject(target, "Set Overlay Mask");
            var layerProp = serializedObject.FindProperty(props.RenderLayers);
            layerProp.intValue = CanvasRenderTexture.DEFAULT_UI_LAYERMASK;
            serializedObject.ApplyModifiedProperties();
        }

        private bool AutoFixAnyRenderersOnUnrenderedLayers()
        {
            target.GetComponentsInChildren(_tmpRenderers);
            foreach (var renderer in _tmpRenderers)
            {
                int layer = renderer.gameObject.layer;
                if (((1 << layer) & target.RenderingLayers) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void AutoFixMoveRenderersToMaskedLayers()
        {
            var maskedLayers = AutoFixGetMaskedLayers();
            var targetLayer = maskedLayers.FirstOrDefault();

            target.GetComponentsInChildren(_tmpRenderers);
            foreach (var renderer in _tmpRenderers)
            {
                int layer = renderer.gameObject.layer;
                if ((layer & target.RenderingLayers) == 0)
                {
                    Undo.RecordObject(renderer.gameObject, "Set Overlay Layer");
                    renderer.gameObject.layer = targetLayer;
                }
            }
        }

        private bool AutoFixAnyCamerasRenderingTargetLayers()
        {
            var cameras = FindObjectsOfType<Camera>();
            foreach (var camera in cameras)
            {
                //Ignore the special camera we create to render to the overlay
                if (camera == target.OverlayCamera)
                {
                    continue;
                }

                if ((camera.cullingMask & target.RenderingLayers) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void AutoFixRemoveRenderingMaskFromCameras()
        {
            var cameras = FindObjectsOfType<Camera>();
            foreach (var camera in cameras)
            {
                Undo.RecordObject(camera, "Set Camera Culling Mask");
                camera.cullingMask = camera.cullingMask & ~target.RenderingLayers;
            }
        }

        private List<int> AutoFixGetMaskedLayers()
        {
            List<int> maskedLayers = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                if ((target.RenderingLayers & (1 << i)) != 0)
                {
                    maskedLayers.Add(i);
                }
            }
            return maskedLayers;
        }
#endregion
    }
}
