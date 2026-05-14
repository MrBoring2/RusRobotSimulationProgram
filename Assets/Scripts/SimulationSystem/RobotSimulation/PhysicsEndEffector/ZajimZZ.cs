using UnityEngine;

public class ZajimZZ: MonoBehaviour
{
    Zajim parent;
    public int numZ;

    void Start()
    {
        parent = gameObject.GetComponentInParent<Zajim>();
    }

  
    private void OnCollisionEnter(Collision collision)
    {
        if (!parent.zajat && parent._propertyProvider.EndEffectorOn)
        {
            if (numZ == 1)
            {
                parent.z1Col = true;
                parent.z1Collision = collision.transform.gameObject;
            }
            else
            {
                parent.z2Col = true;
                parent.z2Collision = collision.transform.gameObject;
            }
        }
        
        
    }
    private void OnCollisionExit(Collision collision)
    {
        if(!parent.zajat && !parent._propertyProvider.EndEffectorOn) 
        {
            if (numZ == 1)
            {
                parent.z1Col = false;
                parent.z1Collision = null;
            }
            else
            {
                parent.z2Col = false;
                parent.z2Collision = null;
            }
        }
        
    }
}
