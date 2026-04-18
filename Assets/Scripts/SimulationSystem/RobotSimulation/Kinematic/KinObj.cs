using UnityEngine;

public class Angles 
{
    public float thetha1 = 0;
    public float thetha2 = -90;
    public float thetha3 = 90;
    public float thetha4 = 0;
    public float thetha5 = 0;
    public float thetha6 = 0;
    public float[] GetFloats()
    {
        return new float[6] { thetha1, thetha2, thetha3, thetha4, thetha5, thetha6 };
    }
}
public class RP
{
    public float a1 = 450;
    public float a2 = -350;
    public float b = 0;
    public float c1 = 447;
    public float c2 = 1150;
    public float c3 = 1350;
    public float c4 = 550;
    public RP(float a1, float a2, float b, float c1, float c2, float c3, float c4)
    {
        this.a1 = a1;
        this.a2 = a2;
        this.b = b;
        this.c1 = c1;
        this.c2 = c2;
        this.c3 = c3;
        this.c4 = c4;        

    }
}