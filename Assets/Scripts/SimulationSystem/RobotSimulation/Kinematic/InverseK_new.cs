using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;




public class InverseK_new : MonoBehaviour
{
    //Translate
    public float X = 0, Y = 0, Z = 0;
    public float UX = 0, UY = 0, UZ = 0;
    public Quaternion RotateQ = Quaternion.Euler(0, 0, 0);
    public Matrix4x4 RotateMatrix = Matrix4x4.zero;

    //Рез. ИК
    public Angles[] Angles = new Angles[8];
    public InverseK_new()
    {
        for (int i = 0; i < 8; i++)
        {
            Angles[i] = new Angles();
        }
    }
    // mm, s1, c1, s23, c23  
    public float mm(int conf)
    {
        return RotateMatrix[0, 2] * ss23(conf) * cc1(conf) + RotateMatrix[1, 2] * ss23(conf) * ss1(conf) + RotateMatrix[2, 2] * cc23(conf);
    }
    public float ss1(int conf)
    {
        return Mathf.Sin(Angles[conf].thetha1);
    }
    public float cc1(int conf)
    {
        return Mathf.Cos(Angles[conf].thetha1);
    }
    public float ss23(int conf)
    {
        return Mathf.Sin(Angles[conf].thetha2 + Angles[conf].thetha3);
    }
    public float cc23(int conf)
    {
        return Mathf.Cos(Angles[conf].thetha2 + Angles[conf].thetha3);
    }
    //theta4_numerator 
    // theta4_denominator
    private float thetha4_numerator(int conf)
    {
        return RotateMatrix[0, 2] * cc23(conf) * cc1(conf) + RotateMatrix[1, 2] * cc23(conf) * ss1(conf) - RotateMatrix[2, 2] * ss23(conf);
    }
    private float thetha4_denominator(int conf)
    {
        return RotateMatrix[1, 2] * cc1(conf) - RotateMatrix[0, 2] * ss1(conf);
    }
    //theta6_numerator 
    // theta6_denominator
    private float thetha6_numerator(int conf)
    {
        return -RotateMatrix[0, 0] * ss23(conf) * cc1(conf) - RotateMatrix[1, 0] * ss23(conf) * ss1(conf) - RotateMatrix[2, 0] * cc23(conf);
    }
    private float thetha6_denominator(int conf)
    {
        return RotateMatrix[0, 1] * ss23(conf) * cc1(conf) + RotateMatrix[1, 1] * ss23(conf) * ss1(conf) + RotateMatrix[2, 1] * cc23(conf);
    }
    /// <summary>
    /// Перевод из системы координат Юнити в Координатную систему робота и подготовка данных для расчета ИК, передавать локальные координаты относительно основания робота
    /// </summary>
    /// <param name="_propertyProvider"></param>
    /// <returns></returns>
    /// 
    public Point Translate(Vector3 position, Quaternion rotation)
    {

        Quaternion correct = Quaternion.Euler(0, 0, 0);

        X = position.x;
        Y = position.y;
        Z = position.z;

        RotateQ = correct * rotation;
        return new Point(new Vector3(X, Y, Z), RotateQ);

    }
    public Angles[] IKCalc(RP RP, Vector3 position, Quaternion rotation)
    {
        return IK(RP, position, rotation);
    }
    public Angles[] IKCalc(RP RP,Point point)
    {
        return IK(RP, point.Position, point.Rotation);
    }
    /// <summary>
    /// Расчет инверсной кинематики n-конфигураций
    /// </summary>
    /// <param name="ZP"></param>
    /// <returns></returns>
    public Angles[] IK(RP RP, Vector3 position, Quaternion rotation)
    {
        Translate(position, rotation);
        //рачсет точки расположения основания сферического запястья
        Vector3 C0 = new Vector3();
        RotateMatrix = Matrix4x4.Rotate(new Quaternion(x: RotateQ.z, y: RotateQ.x, z: RotateQ.y, w: RotateQ.w));
        //C0.x = X - RP.c4 * RotateMatrix[0, 2];
        //C0.y = Y - RP.c4 * RotateMatrix[1, 2];
        //C0.z = Z - RP.c4 * RotateMatrix[2, 2];
        C0.x = Z * 1000 - RP.c4 * RotateMatrix[0, 2];
        C0.y = X * 1000 - RP.c4 * RotateMatrix[1, 2];
        C0.z = Y * 1000 - RP.c4 * RotateMatrix[2, 2];


        float r = Mathf.Sqrt(C0.x * C0.x + C0.y * C0.y);
        float rr = r * r;
        float nx1 = Mathf.Sqrt(rr - RP.b * RP.b) - RP.a1;
        float f1 = Mathf.Atan2(RP.b, nx1 + RP.a1);
        //thetha1
        Angles[0].thetha1 = Mathf.Atan2(C0.y, C0.x) - f1; //i
        Angles[1].thetha1 = Angles[0].thetha1;  //i
        Angles[2].thetha1 = Mathf.Atan2(C0.y, C0.x) + f1 - Mathf.PI; //ii
        Angles[3].thetha1 = Angles[2].thetha1; //ii
        Angles[4].thetha1 = Angles[0].thetha1; //i
        Angles[5].thetha1 = Angles[0].thetha1; //i
        Angles[6].thetha1 = Angles[2].thetha1; //ii
        Angles[7].thetha1 = Angles[2].thetha1; //ii

        float k = Mathf.Sqrt(RP.a2 * RP.a2 + RP.c3 * RP.c3);
        float s1 = Mathf.Sqrt((nx1 * nx1) + ((C0.z - RP.c1) * (C0.z - RP.c1)));
        float s2 = MathF.Sqrt((nx1 + 2 * RP.a1) * (nx1 + 2 * RP.a1) + (C0.z - RP.c1) * (C0.z - RP.c1));
        float acosValue21 = (s1 * s1 + RP.c2 * RP.c2 - k * k) / (2 * s1 * RP.c2);
        //thetha2
        Angles[0].thetha2 = (Mathf.Atan2(nx1, C0.z - RP.c1) - Mathf.Acos(acosValue21)); //i
        Angles[1].thetha2 = (Mathf.Atan2(nx1, C0.z - RP.c1) + Mathf.Acos(acosValue21)); //ii
        float acosValue22 = (s2 * s2 + RP.c2 * RP.c2 - k * k) / (2 * s2 * RP.c2); 
        Angles[2].thetha2 = -(Mathf.Atan2(nx1 + 2 * RP.a1, C0.z - RP.c1) - Mathf.Acos(acosValue22)); //iii
        Angles[3].thetha2 = -(Mathf.Atan2(nx1 + 2 * RP.a1, C0.z - RP.c1) + Mathf.Acos(acosValue22)); //iv
        Angles[4].thetha2 = Angles[0].thetha2;//i
        Angles[5].thetha2 = Angles[1].thetha2;//ii
        Angles[6].thetha2 = Angles[2].thetha2;//iii
        Angles[7].thetha2 = Angles[3].thetha2;//iv

        float acosValue31 = (s1 * s1 - RP.c2 * RP.c2 - k * k) / (2 * RP.c2 * k);
        // thetha3
        Angles[0].thetha3 = Mathf.Acos(acosValue31) - Mathf.Atan2(RP.a2, RP.c3);//i
        Angles[1].thetha3 = -Mathf.Acos(acosValue31) - Mathf.Atan2(RP.a2, RP.c3);//ii
        float acosValue32 = (s2 * s2 - RP.c2 * RP.c2 - k * k) / (2 * RP.c2 * k);
        Angles[2].thetha3 = -Mathf.Atan2(RP.a2, RP.c3) - Mathf.Acos(acosValue32);//iii
        Angles[3].thetha3 = -Mathf.Atan2(RP.a2, RP.c3) + Mathf.Acos(acosValue32);//iv
        Angles[4].thetha3 = Angles[0].thetha3; //i
        Angles[5].thetha3 = Angles[1].thetha3; //ii
        Angles[6].thetha3 = Angles[2].thetha3; //iii
        Angles[7].thetha3 = Angles[3].thetha3; //iv

        ////////////////////////////////////////////
        ///
        //  s1, c1, s23, c23 
        /*float s1i = Mathf.Sin(Angles[0].thetha1);
        float c1i = Mathf.Cos(Angles[0].thetha1);
        float s23i = Mathf.Sin(Angles[0].thetha2 + Angles[0].thetha3);
        float c23i = Mathf.Cos(Angles[0].thetha2 + Angles[0].thetha3);*/

        // Calculate m_i
        //float mi = RotateMatrix[0, 2] * ss23(0) * cc1(0) + RotateMatrix[1, 2] * ss23(0) * ss1(0) + RotateMatrix[2, 2] * cc23(0);

        // thetha4
        //float theta4i_numerator = RotateMatrix[0, 2] * cc23(0) * cc1(0) + RotateMatrix[1, 2] * cc23(0) * ss1(0) - RotateMatrix[2, 2] * ss23(0);
        //float theta4i_denominator = RotateMatrix[1, 2] * cc1(0) - RotateMatrix[0, 2] * ss1(0);
        Angles[0].thetha4 = Mathf.Atan2(thetha4_denominator(0), thetha4_numerator(0));//i
        Angles[1].thetha4 = Mathf.Atan2(thetha4_denominator(1), thetha4_numerator(1));//ii
        Angles[2].thetha4 = Mathf.Atan2(thetha4_denominator(2), thetha4_numerator(2));//iii
        Angles[3].thetha4 = Mathf.Atan2(thetha4_denominator(3), thetha4_numerator(3));//iv
        Angles[4].thetha4 = Mathf.Atan2(thetha4_denominator(4), thetha4_numerator(4)) + MathF.PI; //v
        Angles[5].thetha4 = Mathf.Atan2(thetha4_denominator(5), thetha4_numerator(5)) + MathF.PI; //vi
        Angles[6].thetha4 = Mathf.Atan2(thetha4_denominator(6), thetha4_numerator(6)) + MathF.PI; //vii
        Angles[7].thetha4 = Mathf.Atan2(thetha4_denominator(7), thetha4_numerator(7)) + MathF.PI; //viii
        //  thetha5

        Angles[0].thetha5 = Mathf.Atan2(Mathf.Sqrt(1 - mm(0) * mm(0)), mm(0));//i
        Angles[1].thetha5 = Mathf.Atan2(Mathf.Sqrt(1 - mm(1) * mm(1)), mm(1));//ii
        Angles[2].thetha5 = Mathf.Atan2(Mathf.Sqrt(1 - mm(2) * mm(2)), mm(2));//iii
        Angles[3].thetha5 = Mathf.Atan2(Mathf.Sqrt(1 - mm(3) * mm(3)), mm(3 ));//iv
        Angles[4].thetha5 = -Mathf.Atan2(Mathf.Sqrt(1 - mm(4) * mm(4)), mm(4));//v
        Angles[5].thetha5 = -Mathf.Atan2(Mathf.Sqrt(1 - mm(5) * mm(5)), mm(5));//vi
        Angles[6].thetha5 = -Mathf.Atan2(Mathf.Sqrt(1 - mm(6) * mm(6)), mm(6));//vii
        Angles[7].thetha5 = -Mathf.Atan2(Mathf.Sqrt(1 - mm(7) * mm(7)), mm(7));//viii

        //  thetha6
        // float theta6i_numerator = -RotateMatrix[0, 0] * ss23(0) * cc1(0) - RotateMatrix[1, 0] * ss23(0) * ss1(0) - RotateMatrix[2, 0] * cc23(0);
        //float theta6i_denominator = RotateMatrix[0, 1] * ss23(0) * cc1(0) + RotateMatrix[1, 1] * ss23(0) * ss1(0) + RotateMatrix[2, 1] * cc23(0);
        Angles[0].thetha6 = Mathf.Atan2(thetha6_denominator(0), thetha6_numerator(0));//i
        Angles[1].thetha6 = Mathf.Atan2(thetha6_denominator(1), thetha6_numerator(1));//ii
        Angles[2].thetha6 = Mathf.Atan2(thetha6_denominator(2), thetha6_numerator(2));//iii
        Angles[3].thetha6 = Mathf.Atan2(thetha6_denominator(3), thetha6_numerator(3));//iv
        Angles[4].thetha6 = Mathf.Atan2(thetha6_denominator(4), thetha6_numerator(4)) - Mathf.PI;//v
        Angles[5].thetha6 = Mathf.Atan2(thetha6_denominator(5), thetha6_numerator(5)) - Mathf.PI;//vi
        Angles[6].thetha6 = Mathf.Atan2(thetha6_denominator(6), thetha6_numerator(6)) - Mathf.PI;//vii
        Angles[7].thetha6 = Mathf.Atan2(thetha6_denominator(7), thetha6_numerator(7)) - Mathf.PI;//viii

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0;j < 6; j++)
            {
                if(j == 1)
                {
                    Angles[i].SetThetha(j, Angles[i].GetThetha(j) * 180 / Mathf.PI - 90);
                }
                else
                    Angles[i].SetThetha(j, Angles[i].GetThetha(j) * 180 / Mathf.PI);
            }
        }
        return Angles;
    }
    public float[] CheckLimit(float[] ang)
    {
        Angles Ang = new(ang[0], ang[1], ang[2], ang[3], ang[4], ang[5]);
        CheckLimit(Ang);
        return new float[] {Ang.thetha1, Ang.thetha2, Ang.thetha3, Ang.thetha4, Ang.thetha5, Ang.thetha6};
    }
    /// <summary>
    /// ограничение углов в соответствии с техническими характеристиками робота
    /// </summary>
    /// <param name="ang"></param>
    public bool CheckLimit(Angles ang)
    {
        bool InLimit = false;
        if (ang.thetha1 < -175)
        {
            ang.thetha1 = -175;
            UnityEngine.Debug.LogWarning("ОГР А1");
            InLimit = true;
        }
        if (ang.thetha1 > 175)
        {
            ang.thetha1 = 175;
            UnityEngine.Debug.LogWarning("ОГР А1");
            InLimit = true;
        }
        if (ang.thetha2 > -20)
        {
            ang.thetha2 = -20;
            UnityEngine.Debug.LogWarning("ОГР А2");
            InLimit = true;
        }
        if (ang.thetha2 < -140)
        {
            ang.thetha2 = -140;
            UnityEngine.Debug.LogWarning("ОГР А2");
            InLimit = true;
        }
        if (ang.thetha3 > 170)
        {
            ang.thetha3 = 170;
            UnityEngine.Debug.LogWarning("ОГР А3");
            InLimit = true;
        }
        if (ang.thetha3 < -60)
        {
            ang.thetha3 = -60;
            UnityEngine.Debug.LogWarning("ОГР А3");
            InLimit = true;
        }
        /*if (ang.thetha4 < -180)
        {
            ang.thetha4 = -180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        InLimit = true;
        }
        if (ang.thetha4 > 180)
        {
            ang.thetha4 = 180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        InLimit = true;
        }*/
        if (ang.thetha5 < -105)
        {
            ang.thetha5 = -105;
            UnityEngine.Debug.LogWarning("ОГР А5");
            InLimit = true;
        }
        if (ang.thetha5 > 105)
        {
            ang.thetha5 = 105;
            UnityEngine.Debug.LogWarning("ОГР А5");
            InLimit = true;
        }
        /*if (ang.thetha6 < -180)
        {
            ang.thetha6 = -180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        InLimit = true;
        }
        if (ang.thetha6 > 180)
        {
            ang.thetha6 = 180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        InLimit = true;
        }*/
        return InLimit;
    }

    public int CheckConfig(Angles Angl, RP RP, Vector3 position, Quaternion rotation)
    {
        Angles[] angles = IK(RP, position, rotation);
        int config = 0;
        for (int i = 0; i < 8; i++)
        {
            if (Angl.Diff(angles[i]) < 0.001f)
            {
                config = i;
                break;
            }
        }
        return config;
    }
    /// <summary>
    /// проверка на выход за пределы расчетов (NaN) при невозможности достижения заданной позиции эффектора
    /// </summary>
    /// <param name="ang"></param>
    /// <returns></returns>
    public bool checkIsNaN(Angles ang)
    {
        if (float.IsNaN(ang.thetha1) || float.IsNaN(ang.thetha2) || float.IsNaN(ang.thetha3) ||
            float.IsNaN(ang.thetha4) || float.IsNaN(ang.thetha5) || float.IsNaN(ang.thetha6))
        {
            UnityEngine.Debug.LogError("Выход за пределы расчетов!");
            return false;
        }
        else
        {
            return true;
        }
    }
}