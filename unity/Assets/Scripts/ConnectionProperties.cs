using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;

public enum ConnectionType {
    Unknown = 0,
    DoubleDoor,
    SignleDoor,
    Window
}

public class ConnectionProperties : MonoBehaviour {
    public bool IsOpen = false;
    public string OpenFromRoomId;
    public string OpenToRoomId;
    public ConnectionType Type = ConnectionType.Unknown;

    //public Material WallMaterialId
    [Button]
    public void ToggleOpen() {


        var canOpen = this.gameObject.GetComponentInChildren<CanOpen_Object>();
        if (canOpen != null) {
            canOpen.SetOpennessImmediate(!this.IsOpen ? 1.0f : 0.0f);
        }
        this.IsOpen = !this.IsOpen;
    }

    // [Button]
    // public void Close() { 
    //    this.IsOpen = false;
    //     var canOpen = this.gameObject..GetComponentInChildren<CanOpen_Object>();
    //     if (canOpen != null) {
    //         canOpen.SetOpennessImmediate(0.0f);
    //     }
    // }
}
