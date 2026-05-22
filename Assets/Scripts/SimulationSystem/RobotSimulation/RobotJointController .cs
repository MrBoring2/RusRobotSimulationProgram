using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotJointController : MonoBehaviour
{
    public GameObject J1;
    public GameObject J2;
    public GameObject J3;
    public GameObject J4;
    public GameObject J5;
    public GameObject J6;

    private RobotPropertyProvider _propertyProvider;

    private void Start()
    {
        _propertyProvider = gameObject.GetComponent<RobotPropertyProvider>();
    }
    void FixedUpdate()
    {
        J1.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J1Angle, new Vector3(0, 1, 0));
        J2.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J2Angle, new Vector3(1, 0, 0));
        J3.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J3Angle, new Vector3(1, 0, 0));
        J4.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J4Angle, new Vector3(0, 0, 1));
        J5.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J5Angle, new Vector3(1, 0, 0));
        J6.transform.localRotation = Quaternion.AngleAxis(_propertyProvider.J6Angle, new Vector3(0, 0, 1));

        Physics.SyncTransforms();
    }
}
