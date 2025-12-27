using UnityEngine;

public class ZajimZZ: MonoBehaviour
{
    Zajim parent;
    public int numZ;
    /*public event System.Action<Collision> ZCollisionEnter;
    public event System.Action<Collision> ZCollisionExit;*/
    void Start()
    {
        parent = gameObject.GetComponentInParent<Zajim>();
    }

    // Update is called once per frame
    void Update()
    {

    }
  
    private void OnCollisionEnter(Collision collision)
    {
        if (!parent.zajat && parent._propertyProvider.EndEffectorOn)
        {
            if (numZ == 1)
            {
                parent.z1Col = true;
                parent.z1Collision = collision;
                /*ZCollisionEnter.Invoke(collision);*/
                UnityEngine.Debug.LogWarning("Коллизия!!!!!!!!!!" + gameObject.name);
            }
            else
            {
                parent.z2Col = true;
                parent.z2Collision = collision;
                /*ZCollisionEnter.Invoke(collision);*/
                UnityEngine.Debug.LogWarning("Коллизия!!!!!!!!!!" + gameObject.name);
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
                /*ZCollisionEnter.Invoke(collision);*/
                UnityEngine.Debug.LogWarning("Коллизия!!!!!!!!!!" + gameObject.name);
            }
            else
            {
                parent.z2Col = false;
                parent.z2Collision = null;
                /*ZCollisionEnter.Invoke(collision);*/
                UnityEngine.Debug.LogWarning("Коллизия!!!!!!!!!!" + gameObject.name);
            }
        }
        
    }
}
