using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderValueToText : MonoBehaviour {
    Text text;
    void Awake()
    {
        text = GetComponent<Text>();
    }

	public void SetText(Slider slider)
    {
        text.text = System.Math.Round(slider.value, 2).ToString();
    }
}
