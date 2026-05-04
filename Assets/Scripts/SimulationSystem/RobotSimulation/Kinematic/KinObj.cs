using UnityEngine;

public class Angles
{
    public float[] angs = new float[6];
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
       /* switch (i)
        {
            case 0:
                thetha1 = value;
                break;
            case 1:
                thetha2 = value;
                break;
            case 2:
                thetha3 = value;
                break;
            case 3:
                thetha4 = value;
                break;
            case 4:
                thetha5 = value;
                break;
            case 5:
                thetha6 = value;
                break;
        }*/
        angs[i] = value;
    }
    public Angles(float thetha1, float thetha2, float thetha3, float thetha4, float thetha5, float thetha6)
    {
        /*this.thetha1 = thetha1;
        this.thetha2 = thetha2;
        this.thetha3 = thetha3;
        this.thetha4 = thetha4;
        this.thetha5 = thetha5;
        this.thetha6 = thetha6;*/
        angs[0] = thetha1;
        angs[1] = thetha2;
        angs[2] = thetha3;
        angs[3] = thetha4;
        angs[4] = thetha5;
        angs[5] = thetha6;
    }
    public Angles(float[] thetha)
    {
        /*thetha1 = thetha[0];
        thetha2 = thetha[1];
        thetha3 = thetha[2];
        thetha4 = thetha[3];
        thetha5 = thetha[4];
        thetha6 = thetha[5];*/
        for(int i = 0; i < 6; i++)
        {
            angs[i] = thetha[i];
        }
    }
    public Angles(Angles ang)
    {
        /*this.thetha1 = ang.thetha1;
        this.thetha2 = ang.thetha2;
        this.thetha3 = ang.thetha3;
        this.thetha4 = ang.thetha4;
        this.thetha5 = ang.thetha5;
        this.thetha6 = ang.thetha6;*/
        for (int i = 0; i < 6; i++)
        {
            angs[i] = ang.angs[i];
        }
    }
    public Angles() { }
    public  bool Equals(Angles ang)
    {
        if (ang == null) return false;
        /*return thetha1 == ang.thetha1 &&
               thetha2 == ang.thetha2 &&
               thetha3 == ang.thetha3 &&
               thetha4 == ang.thetha4 &&
               thetha5 == ang.thetha5 &&
               thetha6 == ang.thetha6;*/
        return (GetThetha(0) == ang.GetThetha(0)) &&
               (GetThetha(1) == ang.GetThetha(1)) &&
               (GetThetha(2) == ang.GetThetha(2)) &&
               (GetThetha(3) == ang.GetThetha(3)) &&
               (GetThetha(4) == ang.GetThetha(4)) &&
               (GetThetha(5) == ang.GetThetha(5));
    }
    public float Diff(Angles ang)
    {
        if (ang == null) return 0;
        float max = float.NegativeInfinity;
        for(int i = 0; i < 6; i++)
            {
                float diff = Mathf.Abs(GetThetha(i) - ang.GetThetha(i));
                if (diff > max) max = diff;
            }
        return max;
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