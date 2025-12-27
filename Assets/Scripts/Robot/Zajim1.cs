using System.Drawing;
using System.Reflection;
using UnityEngine;

public class Zajim1 : MonoBehaviour
{
    public GameObject world;
    public GameObject z1;
    public GameObject z2;
    public GameObject Base;
    public GameObject Ogr;
    private Rigidbody z1R;
    private Rigidbody z2R;
    public float zSpeed = 0.01f;
    public bool zajimOn = false;
    public bool zajat = false;
    public bool z1Col = false;
    public bool z2Col = false;
    public Collision z1Collision;
    public Collision z2Collision;
    private Collision ZObj;
    private Vector3 z1Pos;
    private Vector3 z2Pos;

    void Start()
    {

        z1R = z1.GetComponent<Rigidbody>();
        z2R = z2.GetComponent<Rigidbody>();
        /* z1.GetComponent<ZajimZZ>().ZCollisionEnter += Collision1Enter;
         z2.GetComponent<ZajimZZ>().ZCollisionEnter += Collision2Enter;
         z1.GetComponent<ZajimZZ>().ZCollisionExit += Collision1Exit;
         z2.GetComponent<ZajimZZ>().ZCollisionExit += Collision2Exit;*/
        z1Pos = z1.transform.localPosition;
        z2Pos = z2.transform.localPosition;
    }

    void FixedUpdate()
    {
        if (zajimOn && !zajat) //
        {
            if ((z1Col == true) && (z2Col == true))
            {
                if ((z1Collision == z2Collision) && (z1Collision.transform.tag == "Деталь"))
                {
                    ZObj = z1Collision;
                    z1Collision.transform.parent = Base.transform;
                    // z1Collision.transform.SetParent(Base.transform, true); 
                    z1Collision.rigidbody.isKinematic = true;

                    zajat = true;
                }
            }

            else
            {
                // z1R.MovePosition(z1R.transform.localPosition - new Vector3(-1, 0, 0) * zSpeed);
                //z2R.MovePosition(z2R.transform.localPosition - new Vector3(1, 0, 0) * zSpeed);
                z1R.transform.localPosition += new Vector3(1, 0, 0) * zSpeed;
                z2R.transform.localPosition += new Vector3(-1, 0, 0) * zSpeed;
            }

        }

        else if (!zajimOn)
        {
            if ((ZObj != null) && zajat)
            {
                zajat = false;
                //ZObj.rigidbody.transform.parent = null;
                ZObj.rigidbody.transform.parent = null;
                ZObj.rigidbody.isKinematic = false;
                ZObj = null;
                z1Collision = null;
                z2Collision = null;
            }
            if (z1Pos.x < z1.transform.localPosition.x)
            {
                if (Mathf.Abs(z1Pos.x - z1.transform.localPosition.x) < zSpeed && Mathf.Abs(z1Pos.x - z1.transform.localPosition.x) != 0)
                {
                    z1R.transform.localPosition += new Vector3(-1, 0, 0) * Mathf.Abs(z1Pos.x - z1.transform.localPosition.x);
                }
                else
                {
                    z1R.transform.localPosition += new Vector3(-1, 0, 0) * zSpeed;
                }

                //z1R.MovePosition(z1.transform.localPosition - new Vector3(-1, 0, 0) * zSpeed);
            }
            if (z2Pos.x > z2.transform.localPosition.x)
            {
                if (Mathf.Abs(z2Pos.x - z2.transform.localPosition.x) < zSpeed && Mathf.Abs(z2Pos.x - z2.transform.localPosition.x) != 0)
                {
                    z2R.transform.localPosition += new Vector3(1, 0, 0) * Mathf.Abs(z2Pos.x - z2.transform.localPosition.x);
                }
                else
                {
                    z2R.transform.localPosition += new Vector3(1, 0, 0) * zSpeed;
                }

                //z2R.MovePosition(z2.transform.localPosition - new Vector3(1, 0, 0) * zSpeed);
            }
            z1Col = false;
            z2Col = false;


        }
        /*else
        {
            z1R.isKinematic = true;
            z2R.isKinematic = true;
        }*/
        //Physics.SyncTransforms();
        /*if (z1Col && z2Col) 
        {
            if(z1Collision == z2Collision)
            {
                z1Collision.transform.parent = transform;
            }
            
        }*/
    }
    /*void Collision1Enter(Collision collision)
    {
        z1Col = true;
        z1Collision = collision;
    }
    void Collision2Enter(Collision collision)
    {
        z2Col = true;
        z2Collision = collision;
    }
    private void Collision1Exit(Collision collision)
    {
        z1Col = false;
        z1Collision = null;
    }
    private void Collision2Exit(Collision collision)
    {
        z2Col = false;
        z2Collision = null;
    }*/
}
