using Assets.Scripts.CustomEventBus.Signals.Robot;
using System.Collections.Generic;
using UnityEngine;
public class Trigger : MonoBehaviour
{
    public ConvDevice _ConvDevice;
    private List<Rigidbody> items;
    private void Start()
    {
        items = _ConvDevice.items;
    }
    private void OnTriggerEnter(Collider other)
    {
        
        if(other.gameObject.layer == LayerMask.NameToLayer("Detail"))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (!items.Contains(rb))
            {
                items.Add(rb);
            }
        }
            
    }
    private void OnTriggerExit(Collider other)
    {
        if(items.Contains(other.attachedRigidbody)) items.Remove(other.attachedRigidbody);

    }
}