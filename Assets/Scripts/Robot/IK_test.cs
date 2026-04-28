using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;



/// <summary>
/// Преобразование из системы координат Юнити в Координатную систему робота
/// </summary>
public class InverseK_new : MonoBehaviour
{
    //Translate
    public float X = 0, Y = 0, Z = 0;
    public float UX = 0, UY = 0, UZ = 0;
    public Quaternion RotateQ = Quaternion.Euler(0,0,0);
    public Matrix4x4 RotateMatrix = Matrix4x4.zero;

    //Рез. ИК
    public Angles[] Angles = new Angles[6];
    public InverseK_new()
    {
        for (int i = 0; i < 6; i++)
        {
            Angles[i] = new Angles();
        }
    }
    /// <summary>
    /// Перевод из системы координат Юнити в Координатную систему робота и подготовка данных для расчета ИК, передавать локальные координаты относительно основания робота
    /// </summary>
    /// <param name="_propertyProvider"></param>
    /// <returns></returns>
    public Point Translate(Point point) //метод не нужен
    {
        //X = _propertyProvider.JOGpoint.LocalPosition.z;
        //Y = _propertyProvider.JOGpoint.LocalPosition.x;
        //Z = _propertyProvider.JOGpoint.LocalPosition.y;

        //UX = _propertyProvider.JOGpoint.Rotation.z;
        //UY = _propertyProvider.JOGpoint.Rotation.x;
        //UZ = _propertyProvider.JOGpoint.Rotation.y;

        //RotateQ.x = _propertyProvider.JOGpoint.LocalRotationQ.z;
        //RotateQ.y = _propertyProvider.JOGpoint.LocalRotationQ.x;
        //RotateQ.z = _propertyProvider.JOGpoint.LocalRotationQ.y;
        //RotateQ.w = _propertyProvider.JOGpoint.LocalRotationQ.w;
        //RotateMatrix = Matrix4x4.Rotate(RotateQ);
        //return new IK_Effector(new Vector3(X, Y, Z), RotateQ);
        X = point.Position.x;
        Y = point.Position.y;
        Z = point.Position.z;

        //UX = point.Rotation.z;
        //UY = point.Rotation.x;
        //UZ = point.Rotation.y;

        RotateQ.x = point.Rotation.x;
        RotateQ.y = point.Rotation.y;
        RotateQ.z = point.Rotation.z;
        RotateQ.w = point.Rotation.w;
        return new Point(new Vector3(X, Y, Z), RotateQ);

    }
    public Point Translate(Vector3 position, Quaternion  rotation)
    {
        //X = _propertyProvider.JOGpoint.LocalPosition.z;
        //Y = _propertyProvider.JOGpoint.LocalPosition.x;
        //Z = _propertyProvider.JOGpoint.LocalPosition.y;

        //UX = _propertyProvider.JOGpoint.Rotation.z;
        //UY = _propertyProvider.JOGpoint.Rotation.x;
        //UZ = _propertyProvider.JOGpoint.Rotation.y;

        //RotateQ.x = _propertyProvider.JOGpoint.LocalRotationQ.z;
        //RotateQ.y = _propertyProvider.JOGpoint.LocalRotationQ.x;
        //RotateQ.z = _propertyProvider.JOGpoint.LocalRotationQ.y;
        //RotateQ.w = _propertyProvider.JOGpoint.LocalRotationQ.w;
        //RotateMatrix = Matrix4x4.Rotate(RotateQ);
        //return new IK_Effector(new Vector3(X, Y, Z), RotateQ);
        //X = position.z;
        //Y = position.x;
        //Z = position.y;

        Quaternion correct = Quaternion.Euler(0, 0, 0);

        X = position.x;
        Y = position.y;
        Z = position.z;

        //UX = rotation.z;
        //UY = rotation.x;
        //UZ = rotation.y;

        //RotateQ.x = rotation.x;
        //RotateQ.y = rotation.y;
        //RotateQ.z = rotation.z;
        //RotateQ.w = rotation.w;
        RotateQ = correct * rotation;
        return new Point(new Vector3(X, Y, Z), RotateQ);

    }

    /// <summary>
    /// Расчет инверсной кинематики n-конфигураций
    /// </summary>
    /// <param name="ZP"></param>
    /// <returns></returns>
    public Angles[] IKCalc()
    {
        //рачсет точки расположения основания сферического запястья
        Vector3 C0 = new Vector3();
        RotateMatrix = Matrix4x4.Rotate(new Quaternion(x:RotateQ.z, y:RotateQ.x, z:RotateQ.y, w:RotateQ.w));
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
        Angles[0].thetha1 = Mathf.Atan2(C0.y, C0.x) - f1;

        float k = Mathf.Sqrt(RP.a2 * RP.a2 + RP.c3 * RP.c3);
        float s1 = Mathf.Sqrt((nx1 * nx1) + ((C0.z - RP.c1) * (C0.z - RP.c1)));
        float acosValue = (s1 * s1 + RP.c2 * RP.c2 - k * k) / (2 * s1 * RP.c2);
        acosValue = Mathf.Clamp(acosValue, -1f, 1f); // Защита от выхода за пределы
        //thetha2
        Angles[0].thetha2 = (Mathf.Atan2(nx1, C0.z - RP.c1) - Mathf.Acos(acosValue));

        float acosValue3 = (s1 * s1 - RP.c2 * RP.c2 - k * k) / (2 * RP.c2 * k);
        // thetha3
        Angles[0].thetha3 = Mathf.Acos(acosValue3) - Mathf.Atan2(RP.a2, RP.c3) ; 


        ////////////////////////////////////////////
        ///
        //  s1, c1, s23, c23 
        float s1i = Mathf.Sin(Angles[0].thetha1);
        float c1i = Mathf.Cos(Angles[0].thetha1);
        float s23i = Mathf.Sin(Angles[0].thetha2 + Angles[0].thetha3);
        float c23i = Mathf.Cos(Angles[0].thetha2 + Angles[0].thetha3);

        // Calculate m_i
        float mi = RotateMatrix[0, 2] * s23i * c1i + RotateMatrix[1, 2] * s23i * s1i + RotateMatrix[2, 2] * c23i;

        // thetha4
        float theta4_numerator = RotateMatrix[0, 2] * c23i * c1i + RotateMatrix[1, 2] * c23i * s1i - RotateMatrix[2, 2] * s23i;
        float theta4_denominator = RotateMatrix[1, 2] * c1i - RotateMatrix[0, 2] * s1i;
        Angles[0].thetha4 = Mathf.Atan2(theta4_denominator, theta4_numerator);

        //  thetha5

        Angles[0].thetha5 = Mathf.Atan2(Mathf.Sqrt(1 - mi * mi), mi) ;

        //  thetha6
        float theta6_numerator = -RotateMatrix[0, 0] * s23i * c1i - RotateMatrix[1, 0] * s23i * s1i - RotateMatrix[2, 0] * c23i;
        float theta6_denominator = RotateMatrix[0, 1] * s23i * c1i + RotateMatrix[1, 1] * s23i * s1i + RotateMatrix[2, 1] * c23i;
        Angles[0].thetha6 = Mathf.Atan2(theta6_denominator, theta6_numerator);


        ///////////////////////////
        ///


        Angles[0].thetha1 = (Angles[0].thetha1 * 180 / Mathf.PI);
        Angles[0].thetha2 = (Angles[0].thetha2 * 180 / Mathf.PI) - 90;
        Angles[0].thetha3 = (Angles[0].thetha3 * 180 / Mathf.PI);
        Angles[0].thetha4 = (Angles[0].thetha4 * 180 / Mathf.PI);
        Angles[0].thetha5 = (Angles[0].thetha5 * 180 / Mathf.PI);
        Angles[0].thetha6 = (Angles[0].thetha6 * 180 / Mathf.PI);
        return Angles;
    }
    /// <summary>
    /// ограничение углов в соответствии с техническими характеристиками робота
    /// </summary>
    /// <param name="ang"></param>
    public void CheckLimit(Angles ang)
    {
        if (ang.thetha1 < -175)
        {
            ang.thetha1 = -175;
            UnityEngine.Debug.LogWarning("ОГР А1");
        }
        if (ang.thetha1 > 175)
        {
            ang.thetha1 = 175;
            UnityEngine.Debug.LogWarning("ОГР А1");
        }
        if (ang.thetha2 > -20)
        {
            ang.thetha2 = -20;
            UnityEngine.Debug.LogWarning("ОГР А2");
        }
        if (ang.thetha2 < -140)
        {
            ang.thetha2 = -140;
            UnityEngine.Debug.LogWarning("ОГР А2");
        }
        if (ang.thetha3 > 170)
        {
            ang.thetha3 = 170;
            UnityEngine.Debug.LogWarning("ОГР А3");
        }
        if (ang.thetha3 < -60)
        {
            ang.thetha3 = -60;
            UnityEngine.Debug.LogWarning("ОГР А3");
        }
        if (ang.thetha4 < -180)
        {
            ang.thetha4 = -180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        }
        if (ang.thetha4 > 180)
        {
            ang.thetha4 = 180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        }
        if (ang.thetha5 < -105)
        {
            ang.thetha5 = -105;
            UnityEngine.Debug.LogWarning("ОГР А5");
        }
        if (ang.thetha5 > 105)
        {
            ang.thetha5 = 105;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }
        if (ang.thetha6 < -180)
        {
            ang.thetha6 = -180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }
        if (ang.thetha6 > 180)
        {
            ang.thetha6 = 180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }
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