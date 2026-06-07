using UnityEngine;


public class VisibleZone : MonoBehaviour
{
    public RobotPropertyProvider _PP;

    void Start()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }


    void Update()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = _PP.VisibleZone;
    }
}
