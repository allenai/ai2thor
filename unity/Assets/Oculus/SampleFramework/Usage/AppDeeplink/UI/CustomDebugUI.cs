using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CustomDebugUI : MonoBehaviour
{
    [SerializeField]
    private RectTransform textPrefab = null;
    public static CustomDebugUI instance;
    const System.Reflection.BindingFlags privateFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

    void Awake()
    {
        Debug.Assert(instance == null);
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public RectTransform AddTextField(string label, int targetCanvas = 0)
    {
        RectTransform textRT = GameObject.Instantiate(textPrefab).GetComponent<RectTransform>();
        InputField inputField = textRT.GetComponentInChildren<InputField>();
        inputField.text = label;

        DebugUIBuilder ui = DebugUIBuilder.instance;
        var addRect = typeof(DebugUIBuilder).GetMethod("AddRect", privateFlags);
        addRect.Invoke(ui, new object[] { textRT, targetCanvas });
        
        return textRT;
    }

    public void RemoveFromCanvas(RectTransform element, int targetCanvas = 0)
    {
        DebugUIBuilder ui = DebugUIBuilder.instance;
        var field = typeof(DebugUIBuilder).GetField("insertedElements", privateFlags);
        var relayout = typeof(DebugUIBuilder).GetMethod("Relayout", privateFlags);
        List<RectTransform>[] elements = (List<RectTransform>[])field.GetValue(ui);
        if(targetCanvas > -1 && targetCanvas < elements.Length-1)
        {
            elements[targetCanvas].Remove(element);
            element.SetParent(null);
            relayout.Invoke(ui, new object[] { });
        }
    }
}
