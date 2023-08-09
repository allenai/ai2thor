using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UI;
using System.Linq;
using System;
using System.Reflection;

namespace Barmetler.RoadSystem
{
	[CustomEditor(typeof(Road))]
	public class RoadEditor : Editor
	{
		static HashSet<RoadEditor> ActiveEditors = new HashSet<RoadEditor>();
		public static RoadEditor GetEditor(GameObject gameObject) =>
			ActiveEditors.FirstOrDefault(e => ((Road)e.target).gameObject == gameObject);

		public Road road { get; private set; }
		RoadSystemSettings settings;
		SerializedObject settingsSerialized;
		Tool lastTool;

		int selectedAnchorPoint = -1;

		public int SelectedAnchorPoint => Tools.current == Tool.Custom ? -1 : selectedAnchorPoint;

		bool rightMouseDown = false;

		public void OnUndoRedo()
		{
			road.OnCurveChanged(true);
		}

		private void OnEnable()
		{
			ActiveEditors.Add(this);
			road = (Road)target;
			settings = RoadSystemSettings.Instance;
			settingsSerialized = new SerializedObject(settings);
			road.RefreshEndPoints();
			UpdateToolVisibility();
			Undo.undoRedoPerformed += OnUndoRedo;
		}

		private void OnDisable()
		{
			Tools.hidden = false;
			Undo.undoRedoPerformed -= OnUndoRedo;
			ActiveEditors.Remove(this);
			RoadLinkTool.Select(road, selectedAnchorPoint <= road.NumSegments / 2);
		}

		private void OnSceneGUI()
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
				rightMouseDown = true;
			else if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
				rightMouseDown = false;

			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			if (selectedAnchorPoint >= road.NumPoints) selectedAnchorPoint = road.NumPoints - 1;

			if (road.transform.hasChanged && !(road.transform.hasChanged = false))
				road.RefreshEndPoints();

			UpdateToolVisibility();

			DrawInfo(controlID);
			GUIControlPoints(controlID);
			GUIAddOrRemovePoints(controlID);

			GUIDrawWindow();
		}

		void UpdateToolVisibility()
		{
			switch (Tools.current)
			{
				case Tool.Move:
				case Tool.Rotate:
				case Tool.Scale:
					Tools.hidden = selectedAnchorPoint != -1;
					break;
				case Tool.Rect:
					Tools.hidden = true;
					break;
				case Tool.Custom:
					// Enable tools if you switch to custom tool, but from then on let the custom tool manage tool visibility
					if (lastTool != Tool.Custom)
						Tools.hidden = false;
					break;
				default:
					Tools.hidden = false;
					break;
			}

			lastTool = Tools.current;
		}

		void DrawInfo(int controlID)
		{
			if (settings.DrawBoundingBoxes)
			{
				RSHandleUtility.DrawBoundingBoxes(road);
			}

			var points = road.GetEvenlySpacedPoints(1, 1).Select(e => e.ToWorldSpace(road.transform)).ToArray();
			Vector3 lastPos = Vector3.zero;
			Handles.color = Color.green * 0.8f;
			for (int i = 0; i < points.Length; ++i)
			{
				var p = points[i];
				if (i > 0)
					Handles.DrawLine(lastPos, p.position);
				lastPos = p.position;
			}
			foreach (var p in points)
			{
				Handles.color = Color.blue * 0.8f;
				Handles.SphereHandleCap(0, p.position, Quaternion.identity, 0.2f, EventType.Repaint);
				Handles.color = Color.yellow * 0.8f;
				Handles.DrawLine(p.position, p.position + p.normal);
				Handles.color = Color.red * 0.8f;
				Handles.DrawLine(p.position, p.position + p.forward);
			}
		}

		readonly Dictionary<int, Quaternion> initialRotations = new Dictionary<int, Quaternion>();

		void GUIControlPoints(int controlID)
		{
			var e = Event.current;
			var hasModifiers = (e.alt || e.shift || e.control);

			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
				selectedAnchorPoint = -1;

			switch (Tools.current)
			{
				case Tool.Move:
				case Tool.Rotate:
				case Tool.Scale:
					// Draw selection Handles
					var points = Enumerable
						.Range(0, road.NumSegments + 1)
						.Select(e => (index: e * 3, pos: road.transform.TransformPoint(road[e * 3])))
						.OrderByDescending(e => Vector3.Dot(Camera.current.transform.forward, e.pos - Camera.current.transform.position))
						.ToList();

					Handles.color = hasModifiers ? Color.grey : Color.red * 0.8f;
					foreach (var p in points)
					{
						if (selectedAnchorPoint != p.index)
						{
							if (hasModifiers)
								Handles.SphereHandleCap(0, p.pos, Quaternion.identity, 0.2f * HandleUtility.GetHandleSize(p.pos), EventType.Repaint);
							else if (Handles.Button(p.pos, Quaternion.identity,
								 0.3f * HandleUtility.GetHandleSize(p.pos),
								 0.3f * HandleUtility.GetHandleSize(p.pos),
								Handles.SphereHandleCap))
								selectedAnchorPoint = p.index;
						}
					}
					break;
			}

			var pos = Vector3.zero;
			var forward = Vector3.forward;
			var rot = Quaternion.identity;
			if (selectedAnchorPoint != -1)
			{
				pos = road.transform.TransformPoint(road[selectedAnchorPoint]);
				rot = RoadUtilities.GetRotationAtWorldSpace(road, selectedAnchorPoint, out forward, out _);
			}

			if (!e.control)
			{
				switch (Tools.current)
				{
					case Tool.Move:
						if (selectedAnchorPoint != -1)
						{
							var newPos = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Local ? rot : Quaternion.identity);
							if (newPos != pos)
							{
								Undo.RecordObject(road, "Move Control Point");
								road.MovePoint(selectedAnchorPoint, road.transform.InverseTransformPoint(newPos));
							}
						}
						break;

					case Tool.Rotate:
						if (selectedAnchorPoint != -1)
						{
							var hc = GUIUtility.hotControl;
							var newRot = Handles.RotationHandle(Tools.pivotRotation == PivotRotation.Local ? rot : Quaternion.identity, pos);
							if (hc != GUIUtility.hotControl)
							{
								initialRotations[GUIUtility.hotControl] = rot;
							}

							if ((Tools.pivotRotation == PivotRotation.Global && newRot != Quaternion.identity) ||
								(Tools.pivotRotation == PivotRotation.Local && newRot != rot))
							{
								if (Tools.pivotRotation == PivotRotation.Global)
								{
									if (GUIUtility.hotControl == 1317) // 1317
										newRot *= rot;
									else
										newRot *= initialRotations[GUIUtility.hotControl];
								}

								Undo.RecordObject(road, "Rotate Control Point");
								RoadUtilities.SetRotationAtWorldSpace(road, selectedAnchorPoint, newRot);
							}
						}
						break;

					case Tool.Scale:
						if (selectedAnchorPoint != -1)
						{
							Handles.color = hasModifiers ? Color.grey : Color.white * 0.7f;
							Handles.SphereHandleCap(0, pos, Quaternion.identity,
								(hasModifiers ? 0.2f : 0.3f) * HandleUtility.GetHandleSize(pos), EventType.Repaint);
							Handles.color = Color.red + Color.white * 0.4f;
							for (int i = -1; i <= 1; i += 2)
							{
								int j = selectedAnchorPoint + i;
								if (j < 0 || j >= road.NumPoints) continue;
								var hPos = road.transform.TransformPoint(road[j]);

								Handles.color = hasModifiers ? Color.grey : Color.red;
								Handles.DrawLine(pos, hPos);

								Handles.color = hasModifiers ? Color.grey : Color.red + Color.white * 0.4f;
								if (hasModifiers)
								{
									Handles.SphereHandleCap(0, hPos, Quaternion.identity,
										0.2f * HandleUtility.GetHandleSize(pos), EventType.Repaint);
								}
								else
								{
									var nPos = Handles.FreeMoveHandle(hPos, Quaternion.identity,
										0.3f * HandleUtility.GetHandleSize(pos), Vector3.zero, Handles.SphereHandleCap);

									if (hPos != nPos)
									{
										Undo.RecordObject(road, "Scale Control Point");
										var dot = Vector3.Dot(forward, nPos - pos);
										if (i == -1) dot = Mathf.Min(dot, -0.1f);
										else dot = Mathf.Max(dot, 0.1f);
										road.MovePoint(j, road.transform.InverseTransformPoint(pos + forward * dot));
									}
								}
							}
						}
						break;

					case Tool.Rect:

						Handles.color = Color.black;
						for (int i = 0; i < road.NumPoints; i += 3)
						{
							var p = road.transform.TransformPoint(road[i]);

							if (i > 0)
							{
								var p2 = road.transform.TransformPoint(road[i - 1]);
								Handles.DrawLine(p, p2);
							}
							if (i < road.NumPoints - 1)
							{
								var p2 = road.transform.TransformPoint(road[i + 1]);
								Handles.DrawLine(p, p2);
							}
						}

						var points = Enumerable
							.Range(0, road.NumPoints)
							.Select(e => (index: e, pos: road.transform.TransformPoint(road[e])))
							.OrderByDescending(e => Vector3.Dot(Camera.current.transform.forward, e.pos - Camera.current.transform.position))
							.ToList();

						foreach (var p in points)
						{
							var c = ((p.index + 1) / 3 * 3) == selectedAnchorPoint ? (0.7f * Color.cyan + 0.3f * Color.black) : Color.red;
							Handles.color = e.alt ? Color.grey : (p.index % 3 == 0 ? c : (c + Color.white * 0.4f));
							if (e.alt || e.shift || e.control)
							{
								Handles.SphereHandleCap(0, p.pos, Quaternion.identity,
									0.2f * HandleUtility.GetHandleSize(p.pos), EventType.Repaint);
							}
							else
							{
								var newPos = Handles.FreeMoveHandle($"Handle-{p.index}".GetHashCode(), p.pos, Quaternion.identity,
									 (p.index % 3 == 0 ? 0.3f : 0.25f) * HandleUtility.GetHandleSize(p.pos),
									Vector3.zero, Handles.SphereHandleCap);

								if (p.pos != newPos)
								{
									selectedAnchorPoint = (p.index + 1) / 3 * 3;
									Undo.RecordObject(road, "Move Control Point");
									road.MovePoint(p.index, road.transform.InverseTransformPoint(newPos));
								}
							}
						}

						break;
				}
			}
		}

		readonly Dictionary<KeyCode, bool> wasDown = new Dictionary<KeyCode, bool>();

		bool WasDown(KeyCode keyCode)
		{
			if (wasDown.ContainsKey(keyCode))
			{
				return wasDown[keyCode];
			}
			else
			{
				return wasDown[keyCode] = false;
			}
		}

		void GUIAddOrRemovePoints(int controlID)
		{
			var e = Event.current;

			if (rightMouseDown) return;
			switch (Tools.current)
			{
				case Tool.Move:
				case Tool.Rotate:
				case Tool.Scale:
				case Tool.Rect:
					break;
				default:
					return;
			}

			// ===============================
			// =========[ Extrusion ]=========
			// ===============================

			if (e.type == EventType.KeyDown)
			{
				var keyCode = e.keyCode;
				if (e.control && !e.alt && !e.shift && keyCode == KeyCode.E && !WasDown(KeyCode.E))
				{
					if (Extrude(ref selectedAnchorPoint))
						e.Use();
				}
				else if (!e.control && !e.alt && !e.shift && keyCode == KeyCode.Backspace && !WasDown(KeyCode.Backspace))
				{
					if (RemoveSelected())
						e.Use();
				}
				wasDown[keyCode] = true;
			}
			else if (e.type == EventType.KeyUp)
			{
				wasDown[e.keyCode] = false;
			}

			// ===============================
			// =====[ Segment Insertion ]=====
			// ===============================

			if (e.shift && !e.alt && !e.control)
			{
				var minDist = float.PositiveInfinity;
				int segmentIndex = -1;
				var segment = new Vector3[] { };

				for (int seg = 0; seg < road.NumSegments; ++seg)
				{
					var v =
						Bezier.GetEvenlySpacedPoints(
							road.GetPointsInSegment(seg), new List<Vector3> { road.GetNormal(seg), road.GetNormal(seg + 1) }, 1
						).Select(e => e.ToWorldSpace(road.transform).position).ToArray();
					var d = HandleUtility.DistanceToPolyLine(v);
					if (d < minDist)
					{
						minDist = d;
						segmentIndex = seg;
						segment = v;
					}
				}

				if (segmentIndex != -1)
				{
					var hoverPos = HandleUtility.ClosestPointToPolyLine(segment);

					Handles.color = Color.white;
					Handles.SphereHandleCap(0, hoverPos, Quaternion.identity, 1, EventType.Repaint);

					if (e.type == EventType.MouseDown && e.button == 0)
					{
						Undo.RecordObject(road, "Insert Segment");
						road.InsertSegment(road.transform.InverseTransformPoint(hoverPos), segmentIndex);
					}
				}

				// If you click off the road, or the road itself, the selection system will deselect it.
				if (e.type == EventType.Used)
				{
					Selection.activeObject = road;
				}
				if (Event.current.type == EventType.MouseMove) SceneView.RepaintAll();
			}

			// ===============================
			// =====[ Segment Extension ]=====
			// ===============================

			/*
			 * Behavior:
			 * - You can't extend from an endpoint that is connected to an anchor.
			 * - If an end point is selected, the extension will be made from there.
			 * - Otherwise, the closest endpoint will be chosen.
			 * - Selection of coordinates and normal:
			 *   1. If shift is held, the mouse ray is intersected with a plane at the end point.
			 *      The plane's up vector depends on Tools.pivotRotation, it is essentially the green arrow of the translation-gizmo.
			 *      The intersection point has to be within 500 units of the camera.
			 *   2. If the mouse position intersects with a collider within a distance of 500 units, that point is chosen.
			 *   3. Otherwise, the point at the same depth as the end point is used.
			 */

			if (e.control && !e.alt)
			{
				var minDist = float.PositiveInfinity;
				var selectedEndpoint = (isValid: false, isStart: false, position: Vector3.zero, index: (int)0);
				foreach (var isStart in new[] { true, false })
				{
					if ((isStart && road.start) || (!isStart && road.end)) continue;
					var position = road.transform.TransformPoint(road[isStart ? 0 : -1]);
					var d = 0.0f;
					if ((isStart && selectedAnchorPoint == 0) || (!isStart && selectedAnchorPoint == road.NumPoints - 1))
						d = -1;
					else
						d = HandleUtility.DistanceToPolyLine(position, position);

					if (d < minDist)
					{
						minDist = d;
						selectedEndpoint.isValid = true;
						selectedEndpoint.isStart = isStart;
						selectedEndpoint.position = position;
						selectedEndpoint.index = isStart ? 0 : road.NumPoints - 1;
					}
				}

				if (selectedEndpoint.isValid)
				{
					Handles.color = Color.red;

					Handles.SphereHandleCap(
						0,
						selectedEndpoint.position, Quaternion.identity,
						0.3f * HandleUtility.GetHandleSize(selectedEndpoint.position),
						EventType.Repaint);


					var depth = Vector3.Dot(Camera.current.transform.forward, selectedEndpoint.position - Camera.current.transform.position);
					var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
					Vector3 position;

					RoadUtilities.GetRotationAtWorldSpace(road, selectedEndpoint.index, out var forward, out var up);
					var d = selectedEndpoint.position.y;
					if (Tools.pivotRotation == PivotRotation.Local)
						d = Vector3.Dot(up, selectedEndpoint.position);
					else
					{
						forward = Vector3.forward;
						up = Vector3.up;
					}

					if (e.shift && new Plane(-up, d).Raycast(ray, out var enter) && enter < 500)
					{
						position = ray.origin + ray.direction * enter;

						var right = Vector3.Cross(up, forward);
						var c = Handles.color;
						Handles.color = new Color(1, 0.3f, 0.3f, 0.5f);
						RSHandleUtility.DrawGridCircles(selectedEndpoint.position, right, forward, 5, new[] {
						(selectedEndpoint.position, 20.0f),
						(position, 20.0f),
					});
						Handles.color = c;
					}
					else if (!e.shift && settings.UseRayCast && Physics.Raycast(ray, out var rayHit, 500))
					{
						position = rayHit.point;
						if (settings.CopyHitNormal)
							up = rayHit.normal;
					}
					else
					{
						var direction = ray.direction / Vector3.Dot(Camera.current.transform.forward, ray.direction);
						position = Camera.current.transform.position + depth * direction;
					}

					Handles.DrawLine(selectedEndpoint.position, position);

					Handles.DrawLine(position, position + 0.3f * HandleUtility.GetHandleSize(selectedEndpoint.position) * up);
					Handles.ArrowHandleCap(0, position + 0.3f * HandleUtility.GetHandleSize(selectedEndpoint.position) * up, Quaternion.LookRotation(up), 1 * HandleUtility.GetHandleSize(selectedEndpoint.position), EventType.Repaint);

					Handles.SphereHandleCap(
						0,
						position, Quaternion.identity,
						0.3f * HandleUtility.GetHandleSize(selectedEndpoint.position),
						EventType.Repaint);

					if (e.type == EventType.MouseDown && e.button == 0)
					{
						Undo.RecordObject(road, "Add Segment");
						road.AppendSegment(road.transform.InverseTransformPoint(position), selectedEndpoint.isStart, road.transform.InverseTransformDirection(up));
						if (selectedAnchorPoint > 0) selectedAnchorPoint = road.NumPoints - 1;
					}
				}

				// If you click off the road, or the road itself, the selection system will deselect it.
				if (e.type == EventType.Used)
				{
					Selection.activeObject = road;
				}
				if (Event.current.type == EventType.MouseMove) SceneView.RepaintAll();
			}
		}

		#region Actions

		public bool SelectedIsEndpoint(YesNoMaybe shouldBeConnectedToAnchor = YesNoMaybe.MAYBE)
		{
			return IsEndPoint(selectedAnchorPoint, shouldBeConnectedToAnchor);
		}

		public bool IsEndPoint(int i, YesNoMaybe shouldBeConnectedToAnchor = YesNoMaybe.MAYBE)
		{
			bool isEndPoint = (i == 0 || i == road.NumPoints - 1);
			if (!isEndPoint) return false;

			bool isConnected = (i == 0 ? road.start : road.end);

			switch (shouldBeConnectedToAnchor)
			{
				case YesNoMaybe.YES:
					return isConnected;
				case YesNoMaybe.NO:
					return !isConnected;
				default:
					return true;
			}
		}

		public bool UnlinkSelected()
		{
			return Unlink(selectedAnchorPoint);
		}

		/// <summary>
		/// Unlinks A point from an Anchor.
		/// </summary>
		/// <param name="i">- control point index</param>
		/// <returns>Should use Event?</returns>
		public bool Unlink(int i)
		{
			if (IsEndPoint(selectedAnchorPoint))
			{
				if (!(i == 0 ? road.start : road.end))
				{
					Debug.LogWarning("Endpoint is not connected to anything!");
					return true;
				}

				Undo.RecordObject(road, "Unlink Point from Anchor");
				if (i == 0)
					road.start.Disconnect();
				else
					road.end.Disconnect();

				return true;
			}

			return false;
		}

		public bool RemoveSelected()
		{
			return Remove(ref selectedAnchorPoint);
		}

		/// <summary>
		/// Removes a point from the Road.
		/// </summary>
		/// <param name="i">- control point index</param>
		/// <returns>Should use Event?</returns>
		public bool Remove(ref int i)
		{
			if (i != -1)
			{
				if (road.NumSegments == 1)
				{
					Debug.LogWarning("Can't delete last segment!");
					return true;
				}
				Undo.RecordObject(road, "Delete Point");
				if (i == 0 && road.start)
					road.start.Disconnect();
				else if (i == road.NumPoints - 1 && road.end)
					road.end.Disconnect();

				road.DeleteAnchor(i);
				return true;
			}

			return false;
		}

		public bool ExtrudeSelected()
		{
			return Extrude(ref selectedAnchorPoint);
		}

		/// <summary>
		/// Extends the end of the Road.
		/// </summary>
		/// <param name="i">- control point index</param>
		/// <returns>Should use Event?</returns>
		public bool Extrude(ref int i)
		{
			if (i == 0 || i == road.NumPoints - 1)
			{
				if ((i == 0 && road.start == null) || (i != 0 && road.end == null))
				{
					Undo.RecordObject(road, "Extrude");
					var endIndex = i;
					var controlIndex = i == 0 ? 1 : -2;
					road.AppendSegment(road[endIndex] - (road[controlIndex] - road[endIndex]).normalized * 2, i == 0);
					if (i != 0) i = road.NumPoints - 1;
					return true;
				}
			}

			return false;
		}

		#endregion Actions

		static Rect windowRect = new Rect(10000, 10000, 300, 300);

		void GUIDrawWindow()
		{
			// only enable when a point can be selected
			switch (Tools.current)
			{
				case Tool.Move:
				case Tool.Rotate:
				case Tool.Scale:
				case Tool.Rect:
					break;
				default: return;
			}

			windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width - 2);
			windowRect.y = Mathf.Clamp(windowRect.y, 22, Screen.height - windowRect.height - 21);

			windowRect = GUI.Window(0, windowRect, (int WindowID) =>
			{
				if (selectedAnchorPoint != -1)
				{
					Vector3 oldVec = road.transform.TransformPoint(road[selectedAnchorPoint]);
					Vector3 newVec = EditorGUILayout.Vector3Field("Position", oldVec);
					if (newVec != oldVec)
					{
						Undo.RecordObject(road, "Move Point");
						road.MovePoint(selectedAnchorPoint, road.transform.InverseTransformPoint(newVec));
					}

					oldVec = RoadUtilities.GetRotationAtWorldSpace(road, selectedAnchorPoint).eulerAngles;
					newVec = EditorGUILayout.Vector3Field("Rotation", oldVec);
					if (newVec != oldVec)
					{
						Undo.RecordObject(road, "Rotate Point");
						RoadUtilities.SetRotationAtWorldSpace(road, selectedAnchorPoint, Quaternion.Euler(newVec));
					}

					if (selectedAnchorPoint > 0)
					{
						oldVec = road.transform.TransformPoint(road[selectedAnchorPoint - 1]);
						newVec = EditorGUILayout.Vector3Field("Handle Position 1", oldVec);
						if (newVec != oldVec)
						{
							Undo.RecordObject(road, "Move Control Point");
							road.MovePoint(selectedAnchorPoint - 1, road.transform.InverseTransformPoint(newVec));
						}
					}

					if (selectedAnchorPoint < road.NumPoints - 1)
					{
						oldVec = road.transform.TransformPoint(road[selectedAnchorPoint + 1]);
						newVec = EditorGUILayout.Vector3Field("Handle Position 2", oldVec);
						if (newVec != oldVec)
						{
							Undo.RecordObject(road, "Move Control Point");
							road.MovePoint(selectedAnchorPoint + 1, road.transform.InverseTransformPoint(newVec));
						}
					}

					GUILayout.Label("Roll Angle", GUILayout.Width(EditorGUIUtility.labelWidth));
					float oldFloat = road.GetAngle(selectedAnchorPoint / 3);
					float newFloat = EditorGUILayout.DelayedFloatField(oldFloat);
					if (newFloat != oldFloat)
					{
						Undo.RecordObject(road, "Change Angle");
						road.MoveAngle(selectedAnchorPoint / 3, newFloat);
					}
				}
				GUI.DragWindow();
			}, selectedAnchorPoint != -1 ? $"Point {selectedAnchorPoint}" : "No Point Selected");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUI.BeginChangeCheck();

			GUILayout.Space(10);
			Rect rect = EditorGUILayout.GetControlRect(false, 1);
			rect.height = 1;
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
			GUILayout.Space(10);

			EditorGUILayout.PropertyField(settingsSerialized.FindProperty("roadSettings"));
			settingsSerialized.ApplyModifiedProperties();

			BoolField("Auto Set Control Points", road.AutoSetControlPoints, v => road.AutoSetControlPoints = v, road, false);
			if (GUILayout.Button("Set Control Points"))
			{
				Undo.RecordObject(road, "Set Control Points");
				road.AutoSetAllControlPoints();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			if (GUILayout.Button("Reset Road", GUILayout.Height(50)))
			{
				Undo.RecordObject(road, "Reset Road");
				road.Clear();
				road.RefreshEndPoints();
				selectedAnchorPoint = -1;
				SceneView.RepaintAll();
			}

			if (EditorGUI.EndChangeCheck())
			{
				SceneView.RepaintAll();
			}
		}

		void BoolField(string label, bool value, Action<bool> setter, UnityEngine.Object obj, bool endHorizontal = true)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth));
			bool newValue = GUILayout.Toggle(value, "");
			if (newValue != value)
			{
				Undo.RecordObject(obj, $"Toggle {label}");
				setter(newValue);
			}
			if (endHorizontal) GUILayout.EndHorizontal();
		}
	}
}
