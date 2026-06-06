using Assets.Scripts.Providers;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrigOven : MonoBehaviour
{
    private bool collisionDetected;
    public List<Collider> ColList = new();

    void OnTriggerEnter(Collider col)
    {
        ColList.Add(col);
    }
    void OnTriggerExit(Collider col)
    {
        ColList.Remove(col);
    }
    private void FixedUpdate()
    {
        if(ColList.Count > 0)
        {
            collisionDetected = true;
        }
        else
        {
            collisionDetected = false;
        }
    }
    public bool Detect()
    {
        return collisionDetected;
    }
}
