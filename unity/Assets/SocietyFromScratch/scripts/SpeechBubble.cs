using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    void LateUpdate() {
        transform.forward = Camera.main.transform.forward;
    }

    public void SetText(string v) {
        text.text  = v;
    }

    public void Show(bool show) {
       this.gameObject.SetActive(show);
    }
}
