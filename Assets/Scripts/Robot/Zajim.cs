using Assets.Scripts.Providers.PropertyProviders;
using UnityEngine;

public class Zajim : MonoBehaviour
{
    [SerializeField]
    public GameObject z1;
    public GameObject z2;
    public GameObject Base;
    private Rigidbody z1R;
    private Rigidbody z2R;
    public bool zajat = false;
    public bool z1Col = false;
    public bool z2Col = false;
    public GameObject z1Collision;
    public GameObject z2Collision;
    private GameObject ZObj = null;
    private Vector3 z1Pos;
    private Vector3 z2Pos;
    
    public RobotPropertyProvider _propertyProvider;
   
    void Start()
    {
        
        z1R = z1.GetComponent<Rigidbody>();
        z2R = z2.GetComponent<Rigidbody>();
        z1Pos = z1.transform.localPosition;
        z2Pos = z2.transform.localPosition;
    }

    void FixedUpdate()
    {
        if (_propertyProvider.EndEffectorOn && !zajat) //
        {
            if((z1Col==true) && (z2Col == true))
            {
                //if ((z1Collision == z2Collision) && (z1Collision.transform.tag == "Деталь"))
                if ((z1Collision == z2Collision) && (z1Collision.gameObject.GetComponent<IPropertyProvider>() is WorkpiecePropertyProvider))
                {
                    ZObj = z1Collision;
                    z1Collision.transform.parent = Base.gameObject.transform;
                    z1Collision.GetComponent<Rigidbody>().isKinematic = true;
                    zajat = true;
                }
            }
            
            else
            {
                z1R.transform.localPosition += new Vector3(1, 0, 0) * _propertyProvider.SpeedEffector;
                z2R.transform.localPosition += new Vector3(-1, 0, 0) * _propertyProvider.SpeedEffector;
            }
            
        }
        
        else if (!_propertyProvider.EndEffectorOn)
        {
            z1Col = false;
            z2Col = false;
            z1Collision = null;
            z2Collision = null;
            if ((ZObj != null) && zajat)
            {
                zajat = false;
                ZObj.transform.parent = null;
                ZObj.GetComponent<Rigidbody>().isKinematic = false;
                ZObj = null;
                z1Collision = null;
                z2Collision = null;
                
            }
            if (z1Pos.x < z1.transform.localPosition.x)
            {
                if(Mathf.Abs(z1Pos.x - z1.transform.localPosition.x) < _propertyProvider.SpeedEffector && Mathf.Abs(z1Pos.x - z1.transform.localPosition.x) !=0)
                {
                    z1R.transform.localPosition += new Vector3(-1, 0, 0) * Mathf.Abs(z1Pos.x - z1.transform.localPosition.x);
                }
                else
                {
                    z1R.transform.localPosition += new Vector3(-1, 0, 0) * _propertyProvider.SpeedEffector;
                }
                
            }
            if (z2Pos.x > z2.transform.localPosition.x)
            {
                if (Mathf.Abs(z2Pos.x - z2.transform.localPosition.x) < _propertyProvider.SpeedEffector && Mathf.Abs(z2Pos.x - z2.transform.localPosition.x) !=0)
                {
                    z2R.transform.localPosition += new Vector3(1, 0, 0) * Mathf.Abs(z2Pos.x - z2.transform.localPosition.x);
                }
                else
                {
                    z2R.transform.localPosition += new Vector3(1, 0, 0) * _propertyProvider.SpeedEffector;
                }
                
            }
            
            
            
        }
    }
}
