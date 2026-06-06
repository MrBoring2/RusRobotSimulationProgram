using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class IsKinematik : MonoBehaviour
{
    public Rigidbody Base;
    private List<GameObject> inCollided = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void FixedUpdate()
    {
        if(inCollided.Count > 0)
        {
            Base.isKinematic = true;
        }
        else
        {
            Base.isKinematic = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        inCollided.Add(other.gameObject);
    }
    private void OnTriggerExit(Collider other)
    {
        inCollided.Remove(other.gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
