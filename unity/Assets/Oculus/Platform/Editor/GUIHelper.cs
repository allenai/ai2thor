namespace Oculus.Platform
{
  using UnityEditor;
  using UnityEngine;

  class GUIHelper {
    public delegate void Worker();

    static void InOut(Worker begin, Worker body, Worker end) {
      try {
        begin();
        body();
      } finally {
        end();
      }
    }

    public static void HInset(int pixels, Worker worker) {
      InOut( 
        () => {
          GUILayout.BeginHorizontal();
          GUILayout.Space(pixels);
          GUILayout.BeginVertical();
        },
        worker,
        () => {
          GUILayout.EndVertical();
          GUILayout.EndHorizontal();
        }
      );
    }

    public delegate T ControlWorker<T>();
    public static T MakeControlWithLabel<T>(GUIContent label, ControlWorker<T> worker) {
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField(label);

      var result = worker();

      EditorGUILayout.EndHorizontal();
      return result;
    }
  }

}
