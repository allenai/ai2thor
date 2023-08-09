using UnityEngine;

public class ToggleGameObjectEnable : MonoBehaviour
{
    [SerializeField]
    private GameObject _go;

    private bool _enabled = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            _enabled = !_enabled;
            _go.SetActive(_enabled);
        }
    }
}
