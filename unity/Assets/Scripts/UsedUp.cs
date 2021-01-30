using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UsedUp : MonoBehaviour {
    [SerializeField]
    protected GameObject DisableThis;
    
    public bool isUsedUp = false;

    public void UseUp() {
        DisableThis.SetActive(false);
        isUsedUp = true;
    }
}
