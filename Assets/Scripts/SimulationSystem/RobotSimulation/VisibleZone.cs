using UnityEngine;


public class VisibleZone : MonoBehaviour
{
    public RobotPropertyProvider _PP;

    void Start()
    {
       
    }


    void Update()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = _PP.VisibleZone;
    }
}
