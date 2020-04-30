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
using System.Collections;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Contains logic for drawing and interacting with HEU_Curves.
	/// Supports interacting with multiple curves, and multiple points simultaneously.
	/// Utilizes Unity's serialized properties to support Undo.
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(HEU_Curve))]
	public class HEU_CurveUI : Editor
	{
		// CONSTANTS --------------------------------------------------------------------------------------------------

		private Color _unselectedCurveColor = new Color(0, 0.7f, 0);
		private Color _selectedCurveColor = new Color(0, 1f, 0);

		private Color _viewPointColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		private Color _unselectedPointColor = new Color(0, 1f, 0f, 1f);
		private Color _selectedPointColor = new Color(0.8f, 0f, 0.8f);

		private Color _addModeDefaultPointColor = new Color(0.1f, 0.6f, 0.1f);

		private Color _dottedLineColor = Color.yellow;

		private Color _selectionBoxFillColor = new Color(0.5f, 0.8f, 1f, 0.05f);
		private Color _selectionBoxOutlineColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

		private const float _activeBorderWidth = 5f;
		private const float _inactiveBorderWidth = 2f;

		private const float _sceneUIBorderPadding = 2f;

		private const string _curveEditorLabel = "HOUDINI ENGINE CURVE EDITOR";

		private const string _infoHeaderLabel = "CURVE EDITOR INFO";

		private const string _curveNewPointModeLabel = "New Point Mode";

		private const string _infoLabel = "Press F1 to show or hide Info Panel.";

		private const string _curveViewHelp =
			"You can add and edit curve points similar to the Houdini Curve tool."
			+ "\nSelect ADD or EDIT mode or switch to them using Space.";

		private const float _rayCastMaxDistance = 5000f;

		GUIContent[] InteractionModeLabels = new GUIContent[]
		{
			new GUIContent(HEU_Curve.Interaction.VIEW.ToString()),
			new GUIContent(HEU_Curve.Interaction.ADD.ToString()),
			new GUIContent(HEU_Curve.Interaction.EDIT.ToString())
		};

		GUIContent[] NewPointModeLabels = new GUIContent[]
		{
			new GUIContent(CurveNewPointMode.START.ToString()),
			new GUIContent(CurveNewPointMode.INSIDE.ToString()),
			new GUIContent(CurveNewPointMode.END.ToString()),
		};

		// CACHE ------------------------------------------------------------------------------------------------------

		private Camera _currentCamera;

		private Texture2D _lineTexture;

		private Texture2D _boxTexture;

		private Rect _curveEditorUIRect;

		private HEU_Curve.Interaction _interactionMode;

		// Map of selected points for each curve
		private Dictionary<string, List<int>> _selectedCurvePoints = new Dictionary<string, List<int>>();

		private List<HEU_Curve> _curves = new List<HEU_Curve>();
		private Dictionary<string, SerializedObject> _serializedCurvesCache = new Dictionary<string, SerializedObject>();

		// Stack of points added in current Add mode
		private string _latestPointAddedCurve;
		private Stack<int> _latestPointsAdded = new Stack<int>();

		// Drag selection
		private bool _dragMouseDown;
		private Vector3 _dragMouseStart;
		private bool _cleanMouseDown;

		// Add point
		private string _closestCurveName;
		private int _closestPointIndex;
		private Vector3 _newPointPosition;

		private GUIStyle _toolsBGStyle;

		// New point add mode
		private enum CurveNewPointMode
		{
			START,
			INSIDE,
			END,
		}

		private CurveNewPointMode _newPointMode = CurveNewPointMode.END;

		// If info panel is enabled
		private bool _showInfo;

		private bool _showInfoRepaint;

		GUIStyle _helpGridBoxStyle;

		private Rect _infoRect;


		// UI LOGIC ---------------------------------------------------------------------------------------------------

		private void OnEnable()
		{
			_lineTexture = new Texture2D(1, 2);
			_lineTexture.wrapMode = TextureWrapMode.Repeat;
			_lineTexture.SetPixel(0, 0, new Color(1, 1, 1, 0));
			_lineTexture.SetPixel(0, 1, new Color(1, 1, 0, 1));
			_lineTexture.Apply();

			_infoRect = new Rect(10, 10, 500, 220);

			GUISkin heuSkin = HEU_EditorUI.LoadHEUSkin();
			_toolsBGStyle = heuSkin.GetStyle("toolsbg");

			_selectedCurvePoints.Clear();

			HEU_Curve.Interaction setInteraction = HEU_Curve.PreferredNextInteractionMode;
			HEU_Curve.PreferredNextInteractionMode = HEU_Curve.Interaction.VIEW;
			SwitchToMode(setInteraction);

			// Moves focus to the Scene window, which we need for keyboard input at start
			if (SceneView.currentDrawingSceneView != null)
			{
				SceneView.currentDrawingSceneView.Focus();
			}

			// Callback will be used to disable this tool and reset state
			Selection.selectionChanged += SelectionChangedCallback;

			_showInfo = false;
			_showInfoRepaint = false;
		}

		/// <summary>
		/// Callback when selection has changed.
		/// </summary>
		private void SelectionChangedCallback()
		{
			Selection.selectionChanged -= SelectionChangedCallback;

			DisableUI();
		}

		/// <summary>
		/// Clear out cache, and reset any Editor states.
		/// </summary>
		private void DisableUI()
		{
			// Make sure all curves being edited will be ready to be cooked, since we're done editing.
			foreach (Object targetObject in targets)
			{
				HEU_Curve curve = targetObject as HEU_Curve;
				if (curve != null && curve.EditState == HEU_Curve.CurveEditState.EDITING)
				{
					SetCurveState(HEU_Curve.CurveEditState.REQUIRES_GENERATION, GetOrCreateSerializedCurve(curve.CurveName));
				}
			}

			DeselectAllPoints();

			ShowTools();

			_curves.Clear();
			_serializedCurvesCache.Clear();

			_latestPointAddedCurve = null;
			_latestPointsAdded.Clear();

			_closestCurveName = null;
			_closestPointIndex = -1;

			_dragMouseDown = false;
			_cleanMouseDown = false;
		}

		/// <summary>
		/// Finds and returns existing serialized object of specified curve.
		/// Creates serialized object if not found.
		/// </summary>
		/// <param name="curveName">Name of curve to look for</param>
		/// <returns>Serialized curve object</returns>
		private SerializedObject GetOrCreateSerializedCurve(string curveName)
		{
			SerializedObject serializedCurve = null;
			if(!_serializedCurvesCache.TryGetValue(curveName, out serializedCurve))
			{
				HEU_Curve curve = GetCurve(curveName);
				if(curve != null)
				{
					serializedCurve = new SerializedObject(curve);
					_serializedCurvesCache.Add(curveName, serializedCurve);
				}
			}
			return serializedCurve;
		}

		/// <summary>
		/// Update and draw the curves for the specified asset.
		/// Manages the interaction modes.
		/// </summary>
		/// <param name="asset"></param>
		public void UpdateSceneCurves(HEU_HoudiniAsset asset)
		{
			_serializedCurvesCache.Clear();

			// Filter out non-editable curves
			_curves.Clear();
			foreach (Object targetObject in targets)
			{
				HEU_Curve curve = targetObject as HEU_Curve;
				if (curve != null && curve.IsEditable())
				{
					_curves.Add(curve);
				}
			}

			if (_curves.Count == 0)
			{
				return;
			}

			_currentCamera = Camera.current;

			Color defaultHandleColor = Handles.color;

			Event currentEvent = Event.current;
			Vector3 mousePosition = HEU_EditorUI.GetMousePosition(ref currentEvent, _currentCamera);

			int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
			EventType eventType = currentEvent.GetTypeForControl(controlID);

			EditorGUI.BeginChangeCheck();

			// Keep track of curves that were updated so we can apply changes via serialization
			List<SerializedObject> updatedCurves = new List<SerializedObject>();

			if (_interactionMode == HEU_Curve.Interaction.VIEW)
			{
				UpdateViewMode(asset, controlID, eventType, mousePosition, updatedCurves);
			}
			else if (_interactionMode == HEU_Curve.Interaction.ADD)
			{
				UpdateAddMode(asset, controlID, eventType, mousePosition, updatedCurves);
			}
			else if (_interactionMode == HEU_Curve.Interaction.EDIT)
			{
				UpdateEditMode(asset, controlID, eventType, mousePosition, updatedCurves);
			}

			if (EditorGUI.EndChangeCheck())
			{
				foreach (SerializedObject serializedCurve in updatedCurves)
				{
					serializedCurve.ApplyModifiedProperties();
				}
			}

			Handles.color = defaultHandleColor;

			if (eventType == EventType.Layout)
			{
				// Delay update the show info so that the error doesn't popup trying to draw elements during drawing.
				_showInfoRepaint = _showInfo;
			}

			DrawSceneInfo();
		}

		private void UpdateViewMode(HEU_HoudiniAsset asset, int controlID, EventType eventType, Vector3 mousePosition, List<SerializedObject> updatedCurves)
		{
			Event currentEvent = Event.current;

			switch (eventType)
			{
				case EventType.MouseDown:
				{

					break;
				}
				case EventType.MouseUp:
				{

					break;
				}
				case EventType.KeyUp:
				{
					if (!currentEvent.alt && currentEvent.keyCode == KeyCode.Space)
					{
						// Toggle modes
						SwitchToMode(HEU_Curve.Interaction.ADD);
					}
					else if (currentEvent.keyCode == KeyCode.F1)
					{
						_showInfo = !_showInfo;
					}

					break;
				}
				case EventType.KeyDown:
				{
					

					break;
				}
				case EventType.Repaint:
				{
					foreach (HEU_Curve curve in _curves)
					{
						// Draw the cooked curve using its vertices
						DrawCurveUsingVertices(curve, _unselectedCurveColor);

						DrawPointCaps(curve, _viewPointColor);
					}

					break;
				}
				case EventType.Layout:
				{

					break;
				}
			}
		}

		private void UpdateAddMode(HEU_HoudiniAsset asset, int controlID, EventType eventType, Vector3 mousePosition, List<SerializedObject> updatedCurves)
		{
			Event currentEvent = Event.current;

			Color defaultHandleColor = Handles.color;

			switch (eventType)
			{
				case EventType.MouseDown:
				{
					if (!currentEvent.alt && currentEvent.button == 0 && _closestCurveName != null && _closestPointIndex >= 0)
					{
						AddPoint(_closestCurveName, _closestPointIndex, _newPointPosition, updatedCurves);
						_closestCurveName = null;

						currentEvent.Use();
					}

					break;
				}
				case EventType.MouseUp:
				{

					break;
				}
				case EventType.MouseMove:
				{
					// Use the mouse move event will force a repaint allowing for much more responsive UI
					currentEvent.Use();
					break;
				}
				case EventType.KeyUp:
				{
					if (currentEvent.keyCode == KeyCode.Space && !currentEvent.alt)
					{
						// Toggle modes
						SwitchToMode(HEU_Curve.Interaction.EDIT);
					}
					else if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
					{
						SwitchToMode(HEU_Curve.Interaction.VIEW);
						currentEvent.Use();
					}

					break; 
				}
				case EventType.KeyDown:
				{
					if (currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Delete)
					{
						// Delete last added point
						if (_latestPointAddedCurve != null)
						{
							HEU_Curve latestAddCurve = GetCurve(_latestPointAddedCurve);
							if (latestAddCurve != null && _latestPointsAdded.Count > 0)
							{
								SelectSinglePoint(latestAddCurve, _latestPointsAdded.Pop());
								DeleteSelectedPoints(updatedCurves);
							}
						}

						currentEvent.Use();
					}
					else if (currentEvent.keyCode == KeyCode.A)
					{
						int mode = (int)_newPointMode + 1;
						if (mode > (int)CurveNewPointMode.END)
						{
							mode = (int)CurveNewPointMode.START;
						}
						_newPointMode = (CurveNewPointMode)mode;
					}
					else if (currentEvent.keyCode == KeyCode.F1)
					{
						_showInfo = !_showInfo;
					}

					break;
				}
				case EventType.Layout:
				{
					// This disables deselection on asset while in Add mode
					HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

					break;
				}
				case EventType.Repaint:
				{
					bool bMouseInDrawArea = HEU_GeneralUtility.IsMouseWithinSceneView(_currentCamera, mousePosition) 
						&& !HEU_GeneralUtility.IsMouseOverRect(_currentCamera, mousePosition, ref _curveEditorUIRect)
						&& !HEU_GeneralUtility.IsMouseOverRect(_currentCamera, mousePosition, ref _infoRect);

					// Plane for default collider
					Plane collisionPlane = new Plane(Vector3.up, Vector3.zero);
					//Ray mouseRay = _currentCamera.ScreenPointToRay(mousePosition);
					//Vector3 planePosition = mouseRay.origin + mouseRay.direction * 100f;
					//Plane collisionPlane = new Plane(-_currentCamera.transform.forward, planePosition);

					HEU_Curve.CurveDrawCollision drawCollision = asset.CurveDrawCollision;
					List<Collider> drawColliders = null;
					LayerMask drawLayerMask = Physics.DefaultRaycastLayers;
					if (drawCollision == HEU_Curve.CurveDrawCollision.LAYERMASK)
					{
						drawLayerMask = asset.GetCurveDrawLayerMask();
					}
					else if (drawCollision == HEU_Curve.CurveDrawCollision.COLLIDERS)
					{
						drawColliders = asset.GetCurveDrawColliders();
					}

					// Adding new point between line segments
					
					_closestPointIndex = -1;
					_closestCurveName = null;
					_newPointPosition = Vector3.zero;

					float closestDistance = float.MaxValue;

					foreach (HEU_Curve curve in _curves)
					{
						// Draw the cooked curve using its vertices
						DrawCurveUsingVertices(curve, _selectedCurveColor);

						DrawPointCaps(curve, _addModeDefaultPointColor);

						List<Vector3> points = curve.GetAllPoints();
						int numPoints = points.Count;

						if (_currentCamera != null && bMouseInDrawArea)
						{
							Ray ray = _currentCamera.ScreenPointToRay(mousePosition);
							RaycastHit[] results = null;

							if (numPoints > 0 && (_newPointMode == CurveNewPointMode.INSIDE))
							{
								// Control -> add point between closest line segment

								for (int i = 0; i < numPoints - 1; ++i)
								{
									Vector3 pointPos0 = curve.GetTransformedPosition(points[i]);
									Vector3 pointPos1 = curve.GetTransformedPosition(points[i + 1]);

									Vector3 screenPos0 = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPos0, _currentCamera);
									Vector3 screenPos1 = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPos1, _currentCamera);

									float distance = HandleUtility.DistancePointToLineSegment(mousePosition, screenPos0, screenPos1);
									if (distance < closestDistance)
									{
										closestDistance = distance;
										_closestPointIndex = i + 1;
										_closestCurveName = curve.CurveName;
									}
								}
							}
							else
							{
								// Show new point from either end of curve, whichever is closest.
								// Use collision to find new point.

								Vector3 hitPoint = Vector3.zero;
								bool bHit = false;

								if (drawCollision == HEU_Curve.CurveDrawCollision.LAYERMASK)
								{
									// Using layermask
									RaycastHit hitInfo;
									if (Physics.Raycast(ray, out hitInfo, _rayCastMaxDistance, drawLayerMask))
									{
										hitPoint = hitInfo.point;
										bHit = true;
									}
								}
								else if (drawColliders != null && drawColliders.Count > 0)
								{
									// Using colliders
									results = Physics.RaycastAll(ray, _rayCastMaxDistance, drawLayerMask);
									foreach (RaycastHit hit in results)
									{
										foreach (Collider drawCollider in drawColliders)
										{
											if (hit.collider == drawCollider)
											{
												hitPoint = hit.point;
												bHit = true;
												break;
											}
										}
									}
								}
								else
								{
									// Using identity plane
									float collisionEnter = 0f;
									if (collisionPlane.Raycast(ray, out collisionEnter))
									{
										collisionEnter = Mathf.Clamp(collisionEnter, _currentCamera.nearClipPlane, _currentCamera.farClipPlane);
										hitPoint = ray.origin + ray.direction * collisionEnter;
										bHit = true;
									}
								}

								if (bHit)
								{
									Vector3 hitPointScreenPosition = HEU_EditorUI.GetHandleWorldToScreenPosition(hitPoint, _currentCamera);

									// Find the closest point to add from (either first or last point)

									// Empty curve:
									// If its just a single curve, we can use the hit point as closest point.
									// For multiple curves, it gets trickier since we don't have an existing point
									// to check for closest point. So we'll just use the parent's transform position
									// as our anchor point.

									Vector3 checkPoint = Vector3.zero;
									int curveClosestPointIndex = 0;

									if (numPoints == 0)
									{
										if(_curves.Count > 1)
										{
											// Multiple curves -> use position of asset
											checkPoint = curve._targetGameObject.transform.position;
										}
										else
										{
											// Single curve -> use hit point as closest
											checkPoint = hitPoint;
										}
									}
									else if (_newPointMode == CurveNewPointMode.START)
									{
										// Curve with at least 1 point + shift held -> use first point
										checkPoint = HEU_EditorUI.GetHandleWorldToScreenPosition(curve.GetTransformedPoint(0), _currentCamera);
										curveClosestPointIndex = 0;
									}
									else
									{
										// Curve with at least 1 point -> use last point
										checkPoint = HEU_EditorUI.GetHandleWorldToScreenPosition(curve.GetTransformedPoint(numPoints - 1), _currentCamera);
										curveClosestPointIndex = numPoints;
									}

									float curveClosestPointDistance = Vector3.Distance(checkPoint, hitPointScreenPosition);
									if (curveClosestPointDistance < closestDistance)
									{
										closestDistance = curveClosestPointDistance;
										_closestPointIndex = curveClosestPointIndex;
										_closestCurveName = curve.CurveName;
										_newPointPosition = hitPoint;
									}

									// Snap to grid
									_newPointPosition = HEU_EditorUI.GetSnapPosition(_newPointPosition);
								}
							}
						}
					}

					// Note that curve name can be empty for valid empty curves
					if (_closestCurveName != null && _closestPointIndex >= 0)
					{
						HEU_Curve closestCurve = GetCurve(_closestCurveName);
						if (closestCurve != null)
						{
							int numPoints = closestCurve.GetNumPoints();
							if ((_newPointMode == CurveNewPointMode.INSIDE) && !currentEvent.alt && numPoints >= 2)
							{
								// Handle adding new point at projected mouse cursor between points

								// First draw the curve line segments
								DrawCurveUsingPoints(closestCurve, Color.yellow);

								// Draw the caps again to hid the ends of line segments above (visually pleasing)
								DrawPointCaps(closestCurve, _addModeDefaultPointColor);

								Vector3 pointPos0 = closestCurve.GetTransformedPoint(_closestPointIndex - 1);
								Vector3 pointPos1 = closestCurve.GetTransformedPoint(_closestPointIndex);

								Vector3 screenPos0 = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPos0, _currentCamera);
								Vector3 screenPos1 = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPos1, _currentCamera);

								Vector3 curveNewPointPosition = HandleUtility.ProjectPointLine(mousePosition, screenPos0, screenPos1);

								Vector2 deltaNew = curveNewPointPosition - screenPos0;
								Vector2 deltaLine = screenPos1 - screenPos0;
								float ratio = Mathf.Clamp01(deltaNew.magnitude / deltaLine.magnitude);

								Vector3 newDir = (pointPos1 - pointPos0);
								curveNewPointPosition = pointPos0 + (newDir.normalized * newDir.magnitude * ratio);

								Handles.color = _selectedPointColor;
								HEU_EditorUI.DrawSphereCap(GUIUtility.GetControlID(FocusType.Passive), curveNewPointPosition, Quaternion.identity, HEU_EditorUI.GetHandleSize(curveNewPointPosition));

								Handles.color = Color.yellow;
								HEU_EditorUI.DrawCircleCap(0, pointPos0, Quaternion.LookRotation(_currentCamera.transform.forward), HEU_EditorUI.GetHandleSize(pointPos0));
								HEU_EditorUI.DrawCircleCap(0, pointPos1, Quaternion.LookRotation(_currentCamera.transform.forward), HEU_EditorUI.GetHandleSize(pointPos1));
								Handles.color = defaultHandleColor;

								_newPointPosition = curveNewPointPosition;

								SceneView.RepaintAll();
							}
							else if (!currentEvent.alt)
							{
								// Handle adding new point at closest curve's end points

								if (closestCurve.GetNumPoints() > 0)
								{
									// Draw dotted line from last point to newPointPosition
									int connectionPoint = (_closestPointIndex > 0) ? _closestPointIndex - 1 : 0;
									Vector3 pointPos0 = closestCurve.GetTransformedPoint(connectionPoint);
									Vector3[] dottedLineSegments = new Vector3[] { pointPos0, _newPointPosition };

									Handles.color = _dottedLineColor;
									Handles.DrawDottedLines(dottedLineSegments, 4f);
								}

								Handles.color = _selectedPointColor;
								HEU_EditorUI.DrawSphereCap(GUIUtility.GetControlID(FocusType.Passive), _newPointPosition, Quaternion.identity, HEU_EditorUI.GetHandleSize(_newPointPosition));
								Handles.color = defaultHandleColor;

								SceneView.RepaintAll();
							}
						}
					}

					break;
				}
			}
		}

		private void UpdateEditMode(HEU_HoudiniAsset asset, int controlID, EventType eventType, Vector3 mousePosition, List<SerializedObject> updatedCurves)
		{
			// In edit, we draw points as interactable buttons, allowing for selection/deselection.
			// We also draw drag handle for selected buttons.

			Event currentEvent = Event.current;

			// For multi-point selection, calculates bounds and centre point
			Bounds bounds = new Bounds();
			int numSelectedPoints = 0;

			bool bInteractionOcurred = false;

			bool isDraggingPoints = false;
			bool wasDraggingPoints = false;

			// Draw the curve points
			EditModeDrawCurvePoints(currentEvent, eventType, ref numSelectedPoints, ref bounds, ref bInteractionOcurred);

			// Two types of dragging: dragging selected points, dragging selection box to select points

			if (numSelectedPoints > 0)
			{
				// Drag selected points

				Vector3 dragHandlePosition = bounds.center;

				// Let Unity do the transform handle magic
				Vector3 newPosition = Handles.PositionHandle(dragHandlePosition, Quaternion.identity);
				isDraggingPoints = (EditorGUIUtility.hotControl != 0);

				Vector3 deltaMove = newPosition - dragHandlePosition;
				if (deltaMove.magnitude > 0)
				{
					// User dragged point(s)
					// We update point value here, but defer parameter coords update until after we finished editing

					foreach (KeyValuePair<string, List<int>> curvePoints in _selectedCurvePoints)
					{
						List<int> selectedPoints = curvePoints.Value;
						if (selectedPoints.Count > 0)
						{
							SerializedObject serializedCurve = GetOrCreateSerializedCurve(curvePoints.Key);
							SerializedProperty curvePointsProperty = serializedCurve.FindProperty("_points");

							foreach (int pointIndex in selectedPoints)
							{
								SerializedProperty pointProperty = curvePointsProperty.GetArrayElementAtIndex(pointIndex);
								Vector3 updatedPosition = pointProperty.vector3Value + deltaMove;

								HEU_Curve curve = serializedCurve.targetObject as HEU_Curve;
								if (curve != null)
								{
									// Localize the movement vector to the curve point's transform,
									// since deltaMove is based on the transformed curve point,
									// and we're adding to the local curve point.
									Vector3 localDeltaMove = curve.GetInvertedTransformedDirection(deltaMove);
									updatedPosition = pointProperty.vector3Value + localDeltaMove;
								}

								pointProperty.vector3Value = updatedPosition;
							}

							// Setting to editing mode to flag that cooking needs to be deferred
							SetCurveState(HEU_Curve.CurveEditState.EDITING, serializedCurve);

							AddChangedSerializedObject(serializedCurve, updatedCurves);
						}
					}

					bInteractionOcurred = true;
				}

				// After drag, process/cook each curve to update its state
				foreach (HEU_Curve curve in _curves)
				{
					SerializedObject serializedCurve = GetOrCreateSerializedCurve(curve.CurveName);
					SerializedProperty stateProperty = serializedCurve.FindProperty("_editState");
					HEU_Curve.CurveEditState editState = (HEU_Curve.CurveEditState)stateProperty.intValue;

					// On mouse release, transition editing curve to generation state
					if (!isDraggingPoints)
					{
						if (editState == HEU_Curve.CurveEditState.EDITING)
						{
							// Flag to cook once user has stopped dragging
							SetCurveState(HEU_Curve.CurveEditState.REQUIRES_GENERATION, serializedCurve);

							AddChangedSerializedObject(serializedCurve, updatedCurves);

							wasDraggingPoints = true;
						}
					}

					// Draw uncooked curve to show user the intermediate curve
					if (editState == HEU_Curve.CurveEditState.EDITING || editState == HEU_Curve.CurveEditState.REQUIRES_GENERATION)
					{
						if (eventType == EventType.Repaint)
						{
							DrawCurveUsingPoints(curve, Color.red);
						}
					}
				}
			}
			
			// Drag to select points
			switch (eventType)
			{
				case EventType.MouseDown:
				{
					if (currentEvent.button == 0 && !_dragMouseDown && !isDraggingPoints && !wasDraggingPoints)
					{
						// This is to reduce the possibility of getting into drag selection mode right after
						// dragging a point.
						_cleanMouseDown = true;
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (currentEvent.button == 0 && !bInteractionOcurred && !_dragMouseDown && !isDraggingPoints && !wasDraggingPoints)
					{
						if (_selectedCurvePoints.Count > 0 && !currentEvent.alt && !currentEvent.control)
						{
							DeselectAllPoints();
							currentEvent.Use();
						}
					}

					if (currentEvent.button == 0)
					{
						if (_dragMouseDown)
						{
							// Note that as user was dragging, the points were auto-selected, so we shouldn't
							// need to do anything here other than stop dragging.
							_dragMouseDown = false;

							currentEvent.Use();
						}

						_cleanMouseDown = false;
					}

					break;
				}
				case EventType.MouseDrag:
				{
					if (!_dragMouseDown && !currentEvent.alt && !currentEvent.control
						&& currentEvent.button == 0 && !isDraggingPoints && !wasDraggingPoints && _cleanMouseDown)
					{
						_dragMouseStart = mousePosition;
						_dragMouseDown = true;
					}

					if (_dragMouseDown)
					{
						currentEvent.Use();
					}

					break;
				}
				case EventType.MouseMove:
				{
					// Use the mouse move event will force a repaint allowing for much more responsive UI
					currentEvent.Use();
					break;
				}
				case EventType.KeyUp:
				{
					if (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
					{
						SwitchToMode(HEU_Curve.Interaction.VIEW);
						currentEvent.Use();
					}
					else if (!currentEvent.alt && currentEvent.keyCode == KeyCode.Space)
					{
						// Toggle modes
						SwitchToMode(HEU_Curve.Interaction.ADD);
						currentEvent.Use();
					}

					break;
				}
				case EventType.KeyDown:
				{
					if (currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Delete)
					{
						DeleteSelectedPoints(updatedCurves);
						currentEvent.Use();
					}
					else if (currentEvent.keyCode == KeyCode.F1)
					{
						_showInfo = !_showInfo;
					}

					break;
				}
				case EventType.Layout:
				{
					// This disables deselection on asset while in Add mode
					HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

					break;
				}
				case EventType.Repaint:
				{
					if (_dragMouseDown)
					{
						DrawSelectionBox(mousePosition, true);
					}

					break;
				}
			}
		}

		private void EditModeDrawCurvePoints(Event currentEvent, EventType eventType, ref int numSelectedPoints, ref Bounds bounds, ref bool bInteractionOcurred)
		{
			Color defaultHandleColor = Handles.color;

			// First we draw all the curves, while drawing each point as button
			// and collecting the selected points.
			foreach (HEU_Curve curve in _curves)
			{
				if (eventType == EventType.Repaint)
				{
					// Draw the cooked curve using its vertices
					DrawCurveUsingVertices(curve, _selectedCurveColor);
				}

				// During dragging, we draw the points in the drag logic later
				if (_dragMouseDown)
				{
					continue;
				}

				// Now draw all the points, but tailor their visual style according to interaction
				List<Vector3> points = curve.GetAllPoints();

				List<int> selectedPoints = new List<int>();
				_selectedCurvePoints.TryGetValue(curve.CurveName, out selectedPoints);

				for (int i = 0; i < points.Count; ++i)
				{
					Vector3 pointPos = curve.GetTransformedPosition(points[i]);
					float pointSize = HEU_EditorUI.GetHandleSize(pointPos);
					float pickSize = pointSize * 2f;

					if (selectedPoints != null && selectedPoints.Contains(i))
					{
						// Selected point
						numSelectedPoints++;

						if (numSelectedPoints == 1)
						{
							bounds = new Bounds(pointPos, Vector3.zero);
						}
						else
						{
							bounds.Encapsulate(pointPos);
						}

						if (selectedPoints.Count > 1 || _selectedCurvePoints.Keys.Count > 1)
						{
							Handles.color = _selectedPointColor;
							if (HEU_EditorUI.DrawSphereCapButton(pointPos, Quaternion.identity, pointSize, pickSize))
							{
								if (currentEvent.control)
								{
									DeselectPoint(curve.CurveName, i);
								}
								else
								{
									SelectSinglePoint(curve, i);
								}
								bInteractionOcurred = true;
							}
							Handles.color = defaultHandleColor;
						}
					}
					else
					{
						// Unselected point

						Handles.color = _unselectedPointColor;
						if (HEU_EditorUI.DrawSphereCapButton(pointPos, Quaternion.identity, pointSize, pickSize))
						{
							if (currentEvent.control)
							{
								SelectAddPoint(curve, i);
							}
							else
							{
								SelectSinglePoint(curve, i);
							}

							bInteractionOcurred = true;
						}
					}
				}
			}
		}

		private void DrawCurveUsingVertices(HEU_Curve curve, Color lineColor)
		{
			Vector3[] vertices = curve.GetVertices();

			Color defaultColor = Handles.color;
			Handles.color = lineColor;
			Matrix4x4 defaultMatrix = Handles.matrix;
			Handles.matrix = curve._targetGameObject.transform.localToWorldMatrix;
			Handles.DrawAAPolyLine(_lineTexture, 10f, vertices);
			Handles.matrix = defaultMatrix;
			Handles.color = defaultColor;
		}

		private void DrawCurveUsingPoints(HEU_Curve curve, Color lineColor)
		{
			List<Vector3> points = curve.GetAllPoints();

			Color defaultColor = Handles.color;
			Handles.color = lineColor;
			Matrix4x4 defaultMatrix = Handles.matrix;
			Handles.matrix = curve._targetGameObject.transform.localToWorldMatrix;
			Handles.DrawAAPolyLine(_lineTexture, 10f, points.ToArray());
			Handles.matrix = defaultMatrix;
			Handles.color = defaultColor;
		}

		private void DrawPointCaps(HEU_Curve curve, Color capColor)
		{
			List<Vector3> points = curve.GetAllPoints();

			Color defaultColor = Handles.color;
			Handles.color = capColor;
			for (int i = 0; i < points.Count; ++i)
			{
				Vector3 pointPos = curve.GetTransformedPosition(points[i]);
				HEU_EditorUI.DrawSphereCap(GUIUtility.GetControlID(FocusType.Passive), pointPos, Quaternion.identity, HEU_EditorUI.GetHandleSize(pointPos));
			}
			Handles.color = defaultColor;
		}

		private HEU_Curve GetCurve(string curveName)
		{
			foreach (HEU_Curve curve in _curves)
			{
				if (curve.CurveName.Equals(curveName))
				{
					return curve;
				}
			}
			return null;
		}

		private void SelectSinglePoint(HEU_Curve curve, int pointIndex)
		{
			_selectedCurvePoints.Clear();
			_selectedCurvePoints[curve.CurveName] = new List<int>();
			_selectedCurvePoints[curve.CurveName].Add(pointIndex);
		}

		private void SelectAddPoint(HEU_Curve curve, int pointIndex)
		{
			if(!_selectedCurvePoints.ContainsKey(curve.CurveName))
			{
				_selectedCurvePoints[curve.CurveName] = new List<int>();
			}
			_selectedCurvePoints[curve.CurveName].Add(pointIndex);
		}

		private void DeselectAllPoints()
		{
			_selectedCurvePoints.Clear();
		}

		private void DeselectPoint(string curveName, int pointIndex)
		{
			List<int> points = null;
			if (_selectedCurvePoints.TryGetValue(curveName, out points))
			{
				points.Remove(pointIndex);

				if(points.Count == 0)
				{
					_selectedCurvePoints.Remove(curveName);
				}
			}
		}

		private void DrawSelectionBox(Vector3 mousePosition, bool bAutoSelectPoints)
		{
			// First draw the selection box from drag start to current mouse position.

			// Calculating the bounding box in screenspace then converting to world seems to
			// produce the best lines in the Scene view regardless of camera orientation.
			Vector3 xVec = new Vector3((mousePosition.x - _dragMouseStart.x), 0, 0);
			Vector3 yVec = new Vector3(0, (mousePosition.y - _dragMouseStart.y), 0);

			Vector3 s00 = _dragMouseStart;
			Vector3 s01 = _dragMouseStart + xVec;
			Vector3 s10 = _dragMouseStart + yVec;
			Vector3 s11 = _dragMouseStart + xVec + yVec;

			Vector3 camFwd = _currentCamera.transform.forward;
			float depth = Mathf.Abs((_currentCamera.transform.position + camFwd * 2f).z);
			Vector3 w00 = _currentCamera.ScreenToWorldPoint(new Vector3(s00.x, s00.y, depth));
			Vector3 w01 = _currentCamera.ScreenToWorldPoint(new Vector3(s01.x, s01.y, depth));
			Vector3 w10 = _currentCamera.ScreenToWorldPoint(new Vector3(s10.x, s10.y, depth));
			Vector3 w11 = _currentCamera.ScreenToWorldPoint(new Vector3(s11.x, s11.y, depth));

			Color defaultColor = Handles.color;
			Handles.color = Color.white;
			Vector3[] lines = new Vector3[]
			{
				w00, w01, w11, w10, w00
			};
			Handles.DrawSolidRectangleWithOutline(lines, _selectionBoxFillColor, _selectionBoxOutlineColor);
			Handles.color = defaultColor;

			if (bAutoSelectPoints)
			{
				// Now we select points withing the selection box

				DeselectAllPoints();

				// We'll use a rect to test against each curve point
				Rect selectionRect = new Rect(_dragMouseStart.x, _dragMouseStart.y, (mousePosition.x - _dragMouseStart.x), (mousePosition.y - _dragMouseStart.y));

				foreach(HEU_Curve curve in _curves)
				{
					int numPoints = curve.GetNumPoints();
					
					for(int i = 0; i < numPoints; ++i)
					{
						Vector3 pointPosition = curve.GetTransformedPoint(i);

						Vector3 pointScreenPosition = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPosition, _currentCamera);

						if(selectionRect.Contains(pointScreenPosition, true))
						{
							SelectAddPoint(curve, i);

							Handles.color = _selectedPointColor;
						}
						else
						{
							Handles.color = _unselectedPointColor;
						}

						HEU_EditorUI.DrawSphereCap(i, pointPosition, Quaternion.identity, HEU_EditorUI.GetHandleSize(pointPosition));
						Handles.color = defaultColor;
					}
				}
			}
		}

		private void DrawSceneInfo()
		{
			float pixelsPerPoint = HEU_EditorUI.GetPixelsPerPoint();
			float screenWidth = Screen.width / pixelsPerPoint;
			float screenHeight = Screen.height / pixelsPerPoint;

			float screenPosHalf = screenWidth * 0.5f;
			float wx = 120;
			float height = 80;
			float height_subtract = 140;

			SetupUIElements();

			Handles.BeginGUI();

			_curveEditorUIRect = new Rect(screenPosHalf - wx, screenHeight - height_subtract, wx * 2f, height);
			using (new GUILayout.AreaScope(_curveEditorUIRect, "", _toolsBGStyle))
			{
				using (new GUILayout.VerticalScope())
				{
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();

						GUILayout.Label(_curveEditorLabel);

						GUILayout.FlexibleSpace();
					}

					HEU_Curve.Interaction newInteraction = (HEU_Curve.Interaction)GUILayout.Toolbar((int)_interactionMode, InteractionModeLabels, GUILayout.MinHeight(30));
					if (newInteraction != _interactionMode)
					{
						// Reset selection and do new
						SwitchToMode(newInteraction);
					}

					using (new GUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();

						GUILayout.Label(_infoLabel);

						GUILayout.FlexibleSpace();
					}
				}

			}

			if (_showInfoRepaint)
			{
				using (new GUILayout.AreaScope(_infoRect, "", _toolsBGStyle))
				{
					GUILayout.Label(_infoHeaderLabel);

					// Help text
					if (_interactionMode == HEU_Curve.Interaction.VIEW)
					{
						GUILayout.Label(_curveViewHelp);
					}
					else if (_interactionMode == HEU_Curve.Interaction.ADD)
					{
						DrawHelpLineGridBox("Left Mouse", "Add point to end of curve.");
						DrawHelpLineGridBox("A", "Toggle where to add new point (Start, Inside, End).");
						DrawHelpLineGridBox("Hold Ctrl", "Grid snapping.");
						DrawHelpLineGridBox("Backspace", "Delete last new point.");
						DrawHelpLineGridBox("Space", "Edit mode.");
						DrawHelpLineGridBox("Esc / Enter", "View mode.");
						
						GUILayout.Space(5);
						
						using (new GUILayout.VerticalScope())
						{
							GUILayout.Label(_curveNewPointModeLabel);

							// Mode of adding new point (at start, middle, or end)
							_newPointMode = (CurveNewPointMode)GUILayout.Toolbar((int)_newPointMode, NewPointModeLabels, GUILayout.MaxWidth(300), GUILayout.MinHeight(20));
						}
					}
					else if (_interactionMode == HEU_Curve.Interaction.EDIT)
					{
						DrawHelpLineGridBox("Left Mouse", "Select point.");
						DrawHelpLineGridBox("Ctrl + Left Mouse", "Multi-select point.");
						DrawHelpLineGridBox("Hold Ctrl + Left Mouse", "Grid snapping when moving points.");
						DrawHelpLineGridBox("Backspace", "Delete selected points.");
						DrawHelpLineGridBox("Space", "Add mode.");
						DrawHelpLineGridBox("Esc / Enter", "View mode.");
					}

				}
			}

			Handles.EndGUI();
		}

		private void SetupUIElements()
		{
			if (_helpGridBoxStyle == null)
			{
				_helpGridBoxStyle = new GUIStyle(GUI.skin.box);
				float c = 0.4f;
				_helpGridBoxStyle.normal.background  = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 0.2f));
				_helpGridBoxStyle.normal.textColor = Color.white;
				_helpGridBoxStyle.fontStyle = FontStyle.Normal;
				_helpGridBoxStyle.fontSize = 12;
				_helpGridBoxStyle.alignment = TextAnchor.MiddleLeft;
			}
		}

		private void DrawHelpLineGridBox(string keyText, string descText)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				GUILayout.Box(keyText, _helpGridBoxStyle, GUILayout.Width(150), GUILayout.Height(20));
				GUILayout.Box(descText, _helpGridBoxStyle, GUILayout.Width(320), GUILayout.Height(20));

				GUILayout.FlexibleSpace();
			}
		}

		private void SwitchToMode(HEU_Curve.Interaction newInteraction)
		{
			DeselectAllPoints();

			if (_interactionMode == HEU_Curve.Interaction.VIEW && newInteraction != HEU_Curve.Interaction.VIEW)
			{
				// When transitioning from View mode, need to hide the info planel until the layout event is
				// triggered so that the additional UI elements are properly handled by Unity.
				_showInfoRepaint = false;
			}

			_interactionMode = newInteraction;

			// We clear our add points list when transitioning to other modes
			if (_interactionMode != HEU_Curve.Interaction.ADD)
			{
				_latestPointAddedCurve = null;
				_latestPointsAdded.Clear();
			}

			// Show/hide the position handle
			if(_interactionMode == HEU_Curve.Interaction.EDIT || _interactionMode == HEU_Curve.Interaction.ADD)
			{
				Tools.hidden = true;
			}
			else
			{
				ShowTools();
			}
		}

		/// <summary>
		/// Show the position handle for asset.
		/// </summary>
		private void ShowTools()
		{
			Tools.hidden = false;
		}

		/// <summary>
		/// Set editing state for given curve.
		/// </summary>
		/// <param name="newState">New state to set</param>
		/// <param name="serializedCurve">Curve to change state for</param>
		private void SetCurveState(HEU_Curve.CurveEditState newState, SerializedObject serializedCurve)
		{
			SerializedProperty editStateProperty = serializedCurve.FindProperty("_editState");
			editStateProperty.intValue = (int)newState;

			// Once we're done editing, we sync points to parameters so cooking will get latest values
			if (newState == HEU_Curve.CurveEditState.REQUIRES_GENERATION)
			{
				SyncCurvePointsToParameters(serializedCurve);
			}

			// This allows to apply serialized changes
			GUI.changed = true;
		}

		/// <summary>
		/// Update given curve's parameters with values from its points array.
		/// During editing, the points should have been updated, which now need to be transferred to coords parameters.
		/// </summary>
		/// <param name="serializedCurve"></param>
		private void SyncCurvePointsToParameters(SerializedObject serializedCurve)
		{
			// Get the parameters, find the coords parameter data, then set the points array as string

			SerializedProperty parametersProperty = serializedCurve.FindProperty("_parameters");
			
			// Since Unity doesn't automatically serialize referenced objects, so we need to create serialized object, and apply changes
			SerializedObject parameterObject = new SerializedObject(parametersProperty.objectReferenceValue);
			SerializedProperty parameterList = parameterObject.FindProperty("_parameterList");
			for(int i = 0; i < parameterList.arraySize; ++i)
			{
				SerializedProperty parameterDataProperty = parameterList.GetArrayElementAtIndex(i);
				SerializedProperty nameProperty = parameterDataProperty.FindPropertyRelative("_name");
				if (nameProperty.stringValue.Equals(HEU_Defines.CURVE_COORDS_PARAM))
				{
					SerializedProperty stringsProperty = parameterDataProperty.FindPropertyRelative("_stringValues");

					List<Vector3> points = new List<Vector3>();
					SerializedProperty curvePointsProperty = serializedCurve.FindProperty("_points");
					for(int j = 0; j < curvePointsProperty.arraySize; ++j)
					{
						points.Add(curvePointsProperty.GetArrayElementAtIndex(j).vector3Value);
					}
					stringsProperty.GetArrayElementAtIndex(0).stringValue = HEU_Curve.GetPointsString(points);

					break;
				}
			}
			parameterObject.ApplyModifiedProperties();
		}

		private void DeleteSelectedPoints(List<SerializedObject> updatedCurves)
		{
			foreach (KeyValuePair<string, List<int>> curvePoints in _selectedCurvePoints)
			{
				List<int> selectedPoints = curvePoints.Value;
				if (selectedPoints.Count > 0)
				{
					SerializedObject serializedCurve = GetOrCreateSerializedCurve(curvePoints.Key);
					SerializedProperty curvePointsProperty = serializedCurve.FindProperty("_points");

					// Re-order point indices to delete from highest index to lowest, as otherwse
					// our indces get out of sync when deleting the lower indices first.
					int[] sortedIndices = selectedPoints.ToArray();
					ReverseCompare reverseCompare = new ReverseCompare();
					System.Array.Sort(sortedIndices, reverseCompare);

					foreach (int pointIndex in sortedIndices)
					{
						if (pointIndex >= 0 && pointIndex < curvePointsProperty.arraySize)
						{
							curvePointsProperty.DeleteArrayElementAtIndex(pointIndex);
						}
					}

					SerializedProperty editStateProperty = serializedCurve.FindProperty("_editState");
					if (editStateProperty.intValue != (int)HEU_Curve.CurveEditState.EDITING)
					{
						SetCurveState(HEU_Curve.CurveEditState.REQUIRES_GENERATION, serializedCurve);
					}

					AddChangedSerializedObject(serializedCurve, updatedCurves);
					GUI.changed = true;
				}
			}

			DeselectAllPoints();
		}

		private void AddPoint(string curveName, int pointIndex, Vector3 newPointPosition, List<SerializedObject> updatedCurves)
		{
			SerializedObject serializedCurve = GetOrCreateSerializedCurve(curveName);
			SerializedProperty curvePointsProperty = serializedCurve.FindProperty("_points");
			if(pointIndex >= 0 && pointIndex <= curvePointsProperty.arraySize)
			{
				HEU_Curve curve = GetCurve(curveName);
				newPointPosition = curve.GetInvertedTransformedPosition(newPointPosition);

				curvePointsProperty.InsertArrayElementAtIndex(pointIndex);
				curvePointsProperty.GetArrayElementAtIndex(pointIndex).vector3Value = newPointPosition;

				SerializedProperty editStateProperty = serializedCurve.FindProperty("_editState");
				if (editStateProperty.intValue != (int)HEU_Curve.CurveEditState.EDITING)
				{
					SetCurveState(HEU_Curve.CurveEditState.REQUIRES_GENERATION, serializedCurve);
				}

				_latestPointAddedCurve = curveName;
				_latestPointsAdded.Push(pointIndex);

				AddChangedSerializedObject(serializedCurve, updatedCurves);
				GUI.changed = true;
			}
		}

		private void AddChangedSerializedObject(SerializedObject serializedObject, List<SerializedObject> serializedList)
		{
			if (!serializedList.Contains(serializedObject))
			{
				serializedList.Add(serializedObject);
			}
		}
	}

}   // HoudiniEngineUnity
