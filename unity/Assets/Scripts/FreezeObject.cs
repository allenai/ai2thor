using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class FreezeObject : MonoBehaviour
{
    public Space space = Space.World;
    public bool FreezePosition = false;
    public bool FreezeRotation = false;


    private Space m_OldSpace = Space.World;
    private bool m_OldFreezePosition = false;
    private bool m_OldFreezeRotation = false;
    private Vector3 m_Position = Vector3.zero;
    private Quaternion m_Rotation = Quaternion.identity;
    void Awake()
    {
        if (Application.isPlaying)
            Destroy(this);
    }
    void Update()
    {
        if (!Application.isEditor)
        {
            Destroy(this);
            return;
        }
        if (FreezePosition)
        {
            // Save current position if enabled
            if ((FreezePosition != m_OldFreezePosition) || (space != m_OldSpace))
                m_Position = (space == Space.World) ? transform.position : transform.localPosition;
            // Freeze the position
            if (space == Space.World)
                transform.position = m_Position;
            else
                transform.localPosition = m_Position;
        }
        if (FreezeRotation)
        {
            // Save current rotation if enabled
            if ((FreezeRotation != m_OldFreezeRotation) || (space != m_OldSpace))
                m_Rotation = (space == Space.World) ? transform.rotation : transform.localRotation;
            // Freeze the rotation
            if (space == Space.World)
                transform.rotation = m_Rotation;
            else
                transform.localRotation = m_Rotation;
        }
        m_OldSpace = space;
        m_OldFreezePosition = FreezePosition;
        m_OldFreezeRotation = FreezeRotation;
    }

}