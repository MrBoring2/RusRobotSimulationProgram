using UnityEngine;

public class ForwarKinObj : MonoBehaviour
{
    public Point Pos
    {
        get => new(transform.position, transform.rotation);
    }

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        
    }
}
