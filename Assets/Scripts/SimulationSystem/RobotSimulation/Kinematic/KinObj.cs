using UnityEngine;
using UnityEngine.UIElements;

public class Angles
{
    private float[] angs = new float[6];
    public float thetha1
    {
        get => angs[0]; set => angs[0] = value;
    }
    public float thetha2 { 
        get => angs[1]; set => angs[1] = value;
    }
    public float thetha3 { 
        get => angs[2]; set => angs[2] = value;
    }
    public float thetha4 { 
        get => angs[3]; set => angs[3] = value;
    }
    public float thetha5 { 
        get => angs[4]; set => angs[4] = value;
    }
    public float thetha6 {
        get => angs[5]; set => angs[5] = value;
    }
    public float[] GetFloats()
    {
        // return new float[6] { thetha1, thetha2, thetha3, thetha4, thetha5, thetha6 };
        return angs;
    }
    public float GetThetha(int i)
    {
        //return GetFloats()[i];
        return angs[i];
    }
    public void SetThetha(int i, float value)
    {
        angs[i] = value;
    }
    public Angles(float thetha1, float thetha2, float thetha3, float thetha4, float thetha5, float thetha6)
    {
        angs[0] = thetha1;
        angs[1] = thetha2;
        angs[2] = thetha3;
        angs[3] = thetha4;
        angs[4] = thetha5;
        angs[5] = thetha6;
    }
    public Angles(float[] thetha)
    {
        for(int i = 0; i < 6; i++)
        {
            angs[i] = thetha[i];
        }
    }
    public Angles(Angles ang)
    {
        for (int i = 0; i < 6; i++)
        {
            angs[i] = ang.angs[i];
        }
    }
    public Angles() { }
    public  bool Equals(Angles ang)
    {
        if (ang == null) return false;
        return (GetThetha(0) == ang.GetThetha(0)) &&
               (GetThetha(1) == ang.GetThetha(1)) &&
               (GetThetha(2) == ang.GetThetha(2)) &&
               (GetThetha(3) == ang.GetThetha(3)) &&
               (GetThetha(4) == ang.GetThetha(4)) &&
               (GetThetha(5) == ang.GetThetha(5));
    }
    float NormalizeAngle360(float angle)
    {
        angle = angle % 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }
    public float Diff(Angles ang)
    {
        if (ang == null) return 0;
        float max = float.NegativeInfinity;
        for(int i = 0; i < 6; i++)
            {
                float diff = Mathf.Abs(NormalizeAngle360(GetThetha(i)) - NormalizeAngle360(ang.GetThetha(i)));
                if (diff > max) max = diff;
                
            }
        return max;
    }
    public void Setfloat(float[] thetha)
    {
        for (int i = 0; i < 6; i++)
        {
            angs[i] = thetha[i];
        }
    }
    public Angles PercentAngles(float Percent)
    {
        float[] newThetha = new float[6];
        for(int i = 0; i < 6; i++)
        {
            newThetha[i] = GetThetha(i) * Percent / 100f;
        }
        return new Angles(newThetha);
    }
    // Метод для обновления всех углов
    public int UpdateFromFloats(float[] thetha)
    {
        if (thetha == null || thetha.Length < 6 || thetha.Length > 6) return 5;
        for(int i = 0;i < 6; i++)
        {
            SetThetha(i, thetha[i]);
        }
        return 0;
    }

    // Метод для обновления из строки
    public int UpdateFromString(string values)
    {
        if (string.IsNullOrEmpty(values))
        {
            return 5;
        }
        string[] parts = values.Split(',');
        if (parts.Length == 6)
        {
            float[] new_angs = new float[6];
            for(int i = 0; i<6; i++)
            {
                if (float.TryParse(parts[i], out float value) && value > 0)
                {
                    new_angs[i] = value;
                }
                else
                {
                    return 5; 
                }
            }
            UpdateFromFloats(new_angs);
            return 0;
        }
        else
        {
            return 5;
        }
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