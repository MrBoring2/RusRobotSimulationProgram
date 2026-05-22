using UnityEngine;

public class Tracking : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    public bool trackX = true;
    public bool trackY = true;
    public bool trackZ = true;
    public bool trackRX = true;
    public bool trackRY = true;
    public bool trackRZ = true;

    void FixedUpdate()
    {
        if (target == null) return;

        Vector3 newPos = transform.position;
        Vector3 targetPos = target.position + offset;

        if (trackX) newPos.x = targetPos.x;
        if (trackY) newPos.y = targetPos.y;
        if (trackZ) newPos.z = targetPos.z;

        transform.position = newPos;

        if (trackRX || trackRY || trackRZ)
        {
            Quaternion newRot = transform.rotation;
            Quaternion targetRot = target.rotation;

            if (trackRX) newRot.x = targetRot.x;
            if (trackRY) newRot.y = targetRot.y;
            if (trackRZ) newRot.z = targetRot.z;

            transform.rotation = newRot;
        }
    }
}