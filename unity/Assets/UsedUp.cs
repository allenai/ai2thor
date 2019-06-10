using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UsedUp : MonoBehaviour
{
    [SerializeField]
    protected GameObject DisableThis;
    
    public bool isUsedUp = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UseUp()
    {
        DisableThis.SetActive(false);
        isUsedUp = true;
    }
}
