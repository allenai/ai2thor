using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private string[] texts;
    [SerializeField] private int textIndex = -1;

    SpeechBubble bubble;

    void Awake(){
        bubble = GetComponentsInChildren<SpeechBubble>()[0];
    }

    void Update()
    {
        if(textIndex >= 0 && textIndex<texts.Length) {
            bubble.SetText(texts[textIndex]);
            bubble.Show(true);
        } else {
            bubble.Show(false);
        }
    }
}
