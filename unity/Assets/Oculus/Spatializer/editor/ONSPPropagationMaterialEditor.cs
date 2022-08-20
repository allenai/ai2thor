/************************************************************************************
Filename    :   ONSPPropagationMaterialEditor.cs
Content     :   Propagation material editor class
                Attach to geometry to define material properties
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEditor;
using UnityEngine;

using Spectrum = ONSPPropagationMaterial.Spectrum;
using Point = ONSPPropagationMaterial.Point;

[CustomEditor(typeof(ONSPPropagationMaterial))]
internal sealed class ONSPPropagationMaterialEditor : Editor{
  
  private enum AxisScale{ Lin, Log, Sqr }

  private sealed class SpectrumDrawer{

    private const float cutoff = 20000f;

    private static readonly Texture2D texture = EditorGUIUtility.whiteTexture;
    
    private static readonly GUIStyle textStyle = new GUIStyle{

      alignment = TextAnchor.MiddleLeft,
      clipping = TextClipping.Overflow,
      fontSize = 8,
      fontStyle = FontStyle.Bold,
      wordWrap = false,
      normal = new GUIStyleState{ textColor = Color.grey },
      focused = new GUIStyleState{ textColor = Color.grey }

    };

    private static int focus;

    private readonly string label;

    private readonly AxisScale scale;
    private readonly Spectrum spectrum;
    
    private bool dragInitiated;
    private bool isDragging;

    private bool displaySpectrum;
    private bool displayPoints;

    internal bool IsFocus{

      get{

        return focus == spectrum.GetHashCode();

      }

    }

    internal SpectrumDrawer(string label, Spectrum spectrum, AxisScale scale){
      
      this.label = label;
      this.spectrum = spectrum;
      this.scale = scale;

    }

    internal void Draw(Event e){

      displaySpectrum = EditorGUILayout.Foldout(displaySpectrum, label);
      
      if(displaySpectrum){

        EditorGUI.indentLevel++;
        DrawSpectrum(e);

        displayPoints = EditorGUILayout.Foldout(displayPoints, "Points");

        if(displayPoints){

          EditorGUI.indentLevel++;
          DrawPoints();
          EditorGUI.indentLevel--;

        }

        EditorGUI.indentLevel--;

      }

    }

    internal void LoadFoldoutState(){

      displaySpectrum = EditorPrefs.GetBool(label + "Spectrum", true);
      displayPoints = EditorPrefs.GetBool(label + "Points", false);

    }

    internal void SaveFoldoutState(){

      EditorPrefs.SetBool(label + "Spectrum", displaySpectrum);
      EditorPrefs.SetBool(label + "Points", displayPoints);

    }

    private void DrawSpectrum(Event e){

      float height = 10 * EditorGUIUtility.singleLineHeight;
      Rect r = EditorGUILayout.GetControlRect(true, height);

      r.width -= rightMargin;
      DrawDataTicks(r);
      r = AudioCurveRendering.BeginCurveFrame(r);

      AudioCurveRendering.DrawFilledCurve(r, EvaluateCurve, AudioCurveRendering.kAudioOrange);

      DrawFrequencyTicks(r);

      HandleEvent(r, e);
      if(IsFocus) DrawSelected(r);

      AudioCurveRendering.EndCurveFrame();

    }
        
    private void DrawPoints(){
      
      var points = spectrum.points;
      int lines = points.Count > 0 ? points.Count + 2 : 1;
      float height = EditorGUIUtility.singleLineHeight * lines;
      Rect r1 = EditorGUILayout.GetControlRect(true, height);
      r1.width -= rightMargin;
      r1.height = EditorGUIUtility.singleLineHeight;

      {

        int oldCount = points.Count;
        int newCount = EditorGUI.DelayedIntField(r1, "Size", oldCount);
        r1.y += r1.height;

        if(newCount < points.Count){

          points.RemoveRange(newCount, oldCount - newCount);
          Undo.SetCurrentGroupName("Points Removed");
          GUI.changed = true;

        } else if(newCount > oldCount){

          if(newCount > points.Capacity)
            points.Capacity = newCount;

          for(int i = oldCount; i < newCount; i++)
            points.Add(new Point(125 * (1 << i)));

          Undo.SetCurrentGroupName("Points Added");
          GUI.changed = true;

        }

      }

      if(points.Count > 0){

        Rect r2 = new Rect(r1.xMax + 9, r1.y + r1.height * 1.125f, 24, r1.height * .75f);
        
        r1.width /= 2;
        EditorGUI.LabelField(r1, "Frequency");
        r1.x += r1.width;
        EditorGUI.LabelField(r1, "Data");
        r1.x -= r1.width;
        r1.y += r1.height;

        for(int i = 0; i < points.Count; i++){

          points[i].frequency = EditorGUI.FloatField(r1, points[i].frequency);
          points[i].frequency = Mathf.Clamp(points[i].frequency, 0f, cutoff);
          r1.x += r1.width;
          points[i].data = EditorGUI.FloatField(r1, points[i].data);
          points[i].data = Mathf.Clamp01(points[i].data);
          r1.x -= r1.width;
          r1.y += r1.height;

          if(GUI.Button(r2, "–")){

            RemovePointAt(i);
            break;

          }

          r2.y += r1.height;

        }

      }

    }

    private void DrawDataTicks(Rect r){

      const int ticks = 10;
      Rect label = new Rect(r.xMax + 9, r.y - r.height / (2 * ticks), 24, r.height / ticks);
      Rect tick = new Rect(r.xMax + 2, r.y - 1, 4.5f, 2);

      for(int i = 0; i <= ticks; i++){
        
        float value = MapData(1 - (float)i / ticks, false);
        
        EditorGUI.DrawRect(tick, textStyle.normal.textColor);
        GUI.Label(label, value.ToString("0.000"), textStyle);
        tick.y += label.height;
        label.y += label.height;

      }

    }

    private void DrawFrequencyTicks(Rect r){
      
      Rect tick = new Rect(r.x, r.y, 1, r.height);
      Rect label = new Rect(r.x, r.yMax - 1.5f * EditorGUIUtility.singleLineHeight, 32, EditorGUIUtility.singleLineHeight);

      for(int i = 1; i < 30; i++){

        float frequency;

        if(MapFrequencyTick(i, out frequency)){

          tick.x = MapFrequency(frequency) * r.width;
          tick.height = label.y - r.y;
          tick.width = 2;
          EditorGUI.DrawRect(tick, textStyle.normal.textColor);

          tick.y = label.yMax;
          tick.height = r.yMax - label.yMax;
          EditorGUI.DrawRect(tick, textStyle.normal.textColor);

          label.x = tick.x - 2;
          GUI.Label(label, FrequencyToString(frequency), textStyle);

          tick.y = r.y;
          tick.height = r.height;
          tick.width = 1;

        } else{

          tick.x = MapFrequency(frequency) * r.width;
          EditorGUI.DrawRect(tick, textStyle.normal.textColor);

        }

      }

    }

    private void DrawSelected(Rect r){
      
      if(spectrum.points.Count > spectrum.selection){

        const float radius = 12;
        Vector2 position = MapPointPosition(r, spectrum.points[spectrum.selection]);
        Vector2 size = new Vector2(radius, radius);
        r = new Rect(position - size / 2, size);

#if UNITY_5
        GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, false, 0);
        GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, false, 0);
#else
        GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, false, 0, Color.white, 0, radius);
        GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, false, 0, Color.black, 2, radius);
#endif

        }
    }

    private void HandleEvent(Rect r, Event e){

      Vector2 position = e.mousePosition;

      switch(e.type){

        case EventType.MouseDown:

          if(r.Contains(position)){

            if(e.clickCount == 2){

              spectrum.selection = spectrum.points.Count;
              spectrum.points.Add(MapMouseEvent(r, position));
              Undo.SetCurrentGroupName("Point Added");
              GUI.changed = true;

            } else{

              int selection = spectrum.selection;
              float minDistance = float.MaxValue;

              for(int i = 0; i < spectrum.points.Count; i++){

                float distance = Vector2.Distance(MapPointPosition(r, spectrum.points[i]), position);

                if(distance < minDistance){

                  selection = i;
                  minDistance = distance;

                }

              }

              if(selection != spectrum.selection){

                spectrum.selection = selection;
                Undo.SetCurrentGroupName("Point Selected");
                GUI.changed = true;

              }

            }

            focus = spectrum.GetHashCode();
            dragInitiated = true;

          } else{

            isDragging = false;
            focus = 0;

          }

          e.Use();

          break;

        case EventType.MouseDrag:

          if(dragInitiated){

            dragInitiated = false;
            isDragging = true;

          }

          if(isDragging && spectrum.selection < spectrum.points.Count){

            spectrum.points[spectrum.selection] = MapMouseEvent(r, position);
            e.Use();

          }

          break;

        case EventType.Ignore:
        case EventType.MouseUp:

          dragInitiated = false;

          if(isDragging){

            isDragging = false;
            Undo.SetCurrentGroupName("Point Moved");
            GUI.changed = true;
            e.Use();

          }

          break;

        case EventType.KeyDown:

          switch(e.keyCode){

            case KeyCode.Delete:
            case KeyCode.Backspace:

              if(spectrum.selection < spectrum.points.Count){

                RemovePointAt(spectrum.selection);
                e.Use();

              }

              break;

          }

          break;

      }

    }

    private void RemovePointAt(int index){

      spectrum.points.RemoveAt(index);

      if(spectrum.selection == index)
        spectrum.selection = spectrum.points.Count;

      Undo.SetCurrentGroupName("Point Removed");
      GUI.changed = true;

    }

    private float EvaluateCurve(float f){

      return 2 * MapData(spectrum[MapFrequency(f, false)]) - 1;

    }

    private Vector2 MapPointPosition(Rect r, Point point){

      return new Vector2{

        x = r.x + r.width * MapFrequency(point.frequency),
        y = r.y + r.height * (1 - MapData(point.data))

      };

    }

    private Point MapMouseEvent(Rect r, Vector2 v){

      return new Point{

        frequency = v.x < r.xMin ? 0 : v.x > r.xMax ? cutoff : MapFrequency((v.x - r.x) / r.width, false),
        data = v.y < r.yMin ? 1 : v.y > r.yMax ? 0 : MapData(1 - (v.y - r.y) / r.height, false)

      };

    }

    private float MapData(float f, bool forward = true){

      switch(scale){

        case AxisScale.Log:
          return forward ? f < 1e-3f ? 0 : 1 + Mathf.Log10(f) / 3 : Mathf.Pow(10, -3 * (1 - f));

        case AxisScale.Sqr:
          return forward ? Mathf.Sqrt(f) : f * f;

        default:
        case AxisScale.Lin:
          return f;

      }

    }
    
    public static bool MapFrequencyTick(int i, out float frequency){

      int power = i / 9 + 1;
      int multiplier = i % 9 + 1;

      frequency = multiplier * Mathf.Pow(10, power);

      return multiplier == 1;

    } 

    public static float MapFrequency(float f, bool forward = true){

      return forward ? f < 10 ? 0 : Mathf.Log(f / 10, cutoff / 10) : 10 * Mathf.Pow(cutoff / 10, f);

    }

    private static string FrequencyToString(float frequency){

      if(frequency < 1000)
        return string.Format("{0:F0} Hz", frequency);
      else
        return string.Format("{0:F0} kHz", frequency * .001f);

    }

  }

  private const float rightMargin = 36;

  private SpectrumDrawer absorption, scattering, transmission;

  private void OnEnable(){

    GetSpectra(out absorption, out scattering, out transmission);

    absorption.LoadFoldoutState();
    scattering.LoadFoldoutState();
    transmission.LoadFoldoutState();
    
  }

  private void OnDisable(){
    
    absorption.SaveFoldoutState();
    scattering.SaveFoldoutState();
    transmission.SaveFoldoutState();

  }

  public override void OnInspectorGUI(){

    ONSPPropagationMaterial material = target as ONSPPropagationMaterial;

    EditorGUI.BeginChangeCheck();

    Rect r = EditorGUILayout.GetControlRect();
    r.width -= rightMargin;

       ONSPPropagationMaterial.Preset newPreset =
      (ONSPPropagationMaterial.Preset)EditorGUI.EnumPopup(r, "Preset", material.preset);

    Event e = Event.current;
    EventType type = e.type;
    absorption.Draw(e);
    scattering.Draw(e);
    transmission.Draw(e);
    
    if(EditorGUI.EndChangeCheck()){

      string groupName = Undo.GetCurrentGroupName();

      Undo.RegisterCompleteObjectUndo(material, groupName);

      if(groupName == "Point Added")
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup() - 1);

      if(material.preset != newPreset)
        material.preset = newPreset;
      else
        material.preset = ONSPPropagationMaterial.Preset.Custom;

      if(Application.isPlaying)
        material.UploadMaterial();

    }

  }

  private void GetSpectra(out SpectrumDrawer absorption,
                          out SpectrumDrawer scattering,
                          out SpectrumDrawer transmission){

    ONSPPropagationMaterial material = target as ONSPPropagationMaterial;

    absorption = new SpectrumDrawer("Absorption", material.absorption, AxisScale.Sqr);
    scattering = new SpectrumDrawer("Scattering", material.scattering, AxisScale.Lin);
    transmission = new SpectrumDrawer("Transmission", material.transmission, AxisScale.Sqr);

  }

}
