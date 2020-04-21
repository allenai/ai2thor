/*
* Copyright (c) <2018> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Custom drag handle that supports multiple axes, with option to turn them on/off.
	/// </summary>
	public class HEU_DragHandleMulti : MonoBehaviour
	{
		// DATA -------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Per-axis cache data
		/// </summary>
		public class DragAxisInfo
		{
			public int _handleHash;
			public DragAxis _dragAxis;
			public Vector3 _direction;

			public Vector2 _dragMouseStart;
			public Vector2 _dragMouseCurrent;

			public Vector3 _dragWorldStart;

			public float _handleClickTime;
			public int _handleClickID;
			public bool _handleHasMoved;

			public Color _axisColor;

			public DragAxisInfo(string handleName, DragAxis axis, Vector3 direction, Color axisColor)
			{
				_handleHash = handleName.GetHashCode();
				_dragAxis = axis;
				_direction = direction;
				_axisColor = axisColor;
			}
		}

		public static float _handleDoubleClikcInterval = 0.5f;

		public enum DragHandleResult
		{
			NONE,

			LMB_PRESS,
			LMB_CLICK,
			LMB_DOUBLECLICK,
			LMB_DRAG,
			LMB_RELEASE,

			RMB_PRESS,
			RMB_CLICK,
			RMB_DOUBLECLICK,
			RMB_DRAG,
			RMB_RELEASE,
		}

		public enum DragAxis
		{
			X_AXIS,
			Y_AXIS,
			Z_AXIS,
			ALL_AXIS
		}

		// Create all axes
		private static DragAxisInfo _axisInfoX = new DragAxisInfo("HEU_DragHandleX", DragAxis.X_AXIS, Vector3.right, Handles.xAxisColor);
		private static DragAxisInfo _axisInfoY = new DragAxisInfo("HEU_DragHandleY", DragAxis.Y_AXIS, Vector3.up, Handles.yAxisColor);
		private static DragAxisInfo _axisInfoZ = new DragAxisInfo("HEU_DragHandleZ", DragAxis.Z_AXIS, Vector3.forward, Handles.zAxisColor);
		private static DragAxisInfo _axisInfoAll = new DragAxisInfo("HEU_DragHandleAll", DragAxis.ALL_AXIS, Vector3.one, new Color(0.8f, 0.0f, 0.8f, 0.7f));


		// LOGIC ------------------------------------------------------------------------------------------------------

		public static Vector3 DoDragHandle(Vector3 position, bool bEnableAxisX, bool bEnableAxisY, bool bEnableAxisZ, bool bEnableAxisAll, out DragHandleResult result)
		{
			result = DragHandleResult.NONE;

			if (bEnableAxisAll)
			{
				position = DoDragHandleAxis(_axisInfoAll, position, ref result);
			}

			if (bEnableAxisX && result == DragHandleResult.NONE)
			{
				position = DoDragHandleAxis(_axisInfoX, position, ref result);
			}

			if (bEnableAxisY && result == DragHandleResult.NONE)
			{
				position = DoDragHandleAxis(_axisInfoY, position, ref result);
			}

			if (bEnableAxisZ && result == DragHandleResult.NONE)
			{
				position = DoDragHandleAxis(_axisInfoZ, position, ref result);
			}

			return position;
		}

		private static Vector3 DoDragHandleAxis(DragAxisInfo axisInfo, Vector3 position, ref DragHandleResult result)
		{
			// Must request a control ID for each interactible control in the GUI that can respond to events
			int id = GUIUtility.GetControlID(axisInfo._handleHash, FocusType.Passive);

			float handleSize = HandleUtility.GetHandleSize(position);

			Camera camera = Camera.current;

			Event currentEvent = Event.current;

			Vector2 mousePos = HEU_EditorUI.GetMousePosition(ref currentEvent, camera);

			Vector3 handlePosition = Handles.matrix.MultiplyPoint(position);
			Matrix4x4 cachedHandleMatrix = Handles.matrix;

			int mouseButtonID = Event.current.button;

			// Process events (using GetTypeForControl to filter events relevant to this control)
			switch (currentEvent.GetTypeForControl(id))
			{
				case EventType.MouseDown:
				{
					if(HandleUtility.nearestControl == id && (mouseButtonID == 0 || mouseButtonID == 1))
					{
						GUIUtility.hotControl = id;

						axisInfo._dragMouseCurrent = axisInfo._dragMouseStart = mousePos;
						axisInfo._dragWorldStart = position;
						axisInfo._handleHasMoved = false;

						currentEvent.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);

						if(mouseButtonID == 0)
						{
							result = DragHandleResult.LMB_PRESS;
						}
						else if(mouseButtonID == 1)
						{
							result = DragHandleResult.RMB_PRESS;
						}
					}

					break;
				}
				case EventType.MouseUp:
				{
					if(GUIUtility.hotControl == id && (mouseButtonID == 0 || mouseButtonID == 1))
					{
						GUIUtility.hotControl = 0;
						currentEvent.Use();
						EditorGUIUtility.SetWantsMouseJumping(0);

						if (mouseButtonID == 0)
						{
							result = DragHandleResult.LMB_RELEASE;
						}
						else if (mouseButtonID == 1)
						{
							result = DragHandleResult.RMB_RELEASE;
						}

						// Double-click
						if(mousePos == axisInfo._dragMouseStart)
						{
							bool doubleClick = (axisInfo._handleClickID == id) && (Time.realtimeSinceStartup - axisInfo._handleClickTime < _handleDoubleClikcInterval);

							axisInfo._handleClickID = id;
							axisInfo._handleClickTime = Time.realtimeSinceStartup;

							if (mouseButtonID == 0)
							{
								result = doubleClick ? DragHandleResult.LMB_DOUBLECLICK : DragHandleResult.LMB_CLICK;
							}
							else if (mouseButtonID == 1)
							{
								result = doubleClick ? DragHandleResult.RMB_DOUBLECLICK : DragHandleResult.RMB_CLICK;
							}
						}
					}

					break;
				}
				case EventType.MouseDrag:
				{
					if(GUIUtility.hotControl == id)
					{
						if (axisInfo._dragAxis == DragAxis.ALL_AXIS)
						{
							// Free movement - (all axis)
							// Flip y because Unity is inverted
							axisInfo._dragMouseCurrent += new Vector2(currentEvent.delta.x, -currentEvent.delta.y);

							Vector3 position2 = camera.WorldToScreenPoint(Handles.matrix.MultiplyPoint(axisInfo._dragWorldStart))
								+ (Vector3)(axisInfo._dragMouseCurrent - axisInfo._dragMouseStart);

							position = Handles.matrix.inverse.MultiplyPoint(camera.ScreenToWorldPoint(position2));
						}
						else
						{
							// Linear movement (constraint to current axis)

							axisInfo._dragMouseCurrent += new Vector2(currentEvent.delta.x, currentEvent.delta.y);

							float mag = HandleUtility.CalcLineTranslation(axisInfo._dragMouseStart, axisInfo._dragMouseCurrent, axisInfo._dragWorldStart, axisInfo._direction);
							position = axisInfo._dragWorldStart + axisInfo._direction * mag;
						}

						if (mouseButtonID == 0)
						{
							result = DragHandleResult.LMB_DRAG;
						}
						else if (mouseButtonID == 1)
						{
							result = DragHandleResult.RMB_DRAG;
						}

						axisInfo._handleHasMoved = true;

						GUI.changed = true;
						currentEvent.Use();
					}

					break;
				}
				case EventType.MouseMove:
				case EventType.Repaint:
				{
					Color handleColor = Handles.color;
					if((GUIUtility.hotControl == id && axisInfo._handleHasMoved) || (HandleUtility.nearestControl == id))
					{
						Handles.color = Color.yellow;
					}
					else
					{
						Handles.color = axisInfo._axisColor;
					}

					Handles.matrix = Matrix4x4.identity;
					if (axisInfo._dragAxis == DragAxis.ALL_AXIS)
					{
						HEU_EditorUI.DrawCubeCap(id, handlePosition, Quaternion.identity, handleSize * 0.25f);
					}
					else
					{
						HEU_EditorUI.DrawArrowCap(id, handlePosition, Quaternion.LookRotation(axisInfo._direction), handleSize);
					}
					Handles.matrix = cachedHandleMatrix;

					Handles.color = handleColor;

					// This forces a Repaint. We want this when we change the axis color due to being cursor being nearest.
					if(currentEvent.type == EventType.MouseMove && HandleUtility.nearestControl == id)
					{
						SceneView.RepaintAll();
					}

					break;
				}
				case EventType.Layout:
				{
					// AddControl tells Unity where each Handle is relative to the current mouse position

					Handles.matrix = Matrix4x4.identity;
					if (axisInfo._dragAxis == DragAxis.ALL_AXIS)
					{
						float distance = handleSize * 0.3f;
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePosition, distance));
					}
					else
					{
						HandleUtility.AddControl(id, HandleUtility.DistanceToLine(handlePosition, handlePosition + axisInfo._direction * handleSize) * 0.4f);
					}
					Handles.matrix = cachedHandleMatrix;
					break;
				}
			}

			return position;
		}
	}
}   // HoudiniEngineUnity
