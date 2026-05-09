using UnityEngine;
using UnityEngine.UIElements;


public class Point
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public float Speed { get; set; }

    public Point(Vector3 pos, Quaternion rot)
    {
        this.Position = pos;
        this.Rotation = rot;
        Speed = 0;
    }
    public Point() { }
}