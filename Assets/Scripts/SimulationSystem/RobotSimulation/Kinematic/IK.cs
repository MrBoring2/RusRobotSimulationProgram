using System;
using UnityEngine;

public class IK
{
    private IK_config Calc_IK_conf = IK_config.conf_1;

    RobotPropertyProvider _propertyProvider;
    public float L1;
    public float L2;
    public float L3;
    public float L4;
    public float L5;
    public float L6;
    public float[] thetha = new float[6];
    public float[] old_thetha = new float[6];
    public float[] step_thetha = new float[6];
    public IK(RobotPropertyProvider RPP)
    {
        _propertyProvider = RPP;
        L1 = RPP.L1;
        L2 = RPP.L2;
        L3 = RPP.L3;
        L4 = RPP.L4;
        L5 = RPP.L5;
        L6 = RPP.L6;
        thetha = RPP.thetha;
        old_thetha = RPP.old_thetha;
        step_thetha = RPP.step_thetha;
        
    }

    Matrix4x4 CreateTransform()
    {
        Quaternion rotation;    // Создаем матрицу вращения 
        /* rotation.x = POINT.transform.rotation.z;//z
         rotation.y = POINT.transform.rotation.x; //x
         rotation.z = POINT.transform.rotation.y; //y*/
        rotation.x = _propertyProvider.XYZRot.z;//z
        rotation.y = -_propertyProvider.XYZRot.x; //x
        rotation.z = _propertyProvider.XYZRot.y;
        rotation.w = _propertyProvider.XYZRot.w;
        rotation = rotation * Quaternion.Euler(180, 0, _propertyProvider.Rotation.y - 180);
        _propertyProvider.XYZ_robot_Rotate = rotation;
        // rotation = rotation * Quaternion.Euler(RTx, RTy, RTz);
        switch (Calc_IK_conf)
        {
            case IK_config.conf_1:
                return Matrix4x4.Rotate(rotation);
            case IK_config.conf_2:
                return Matrix4x4.Transpose(Matrix4x4.Rotate(rotation));//
            default:
                return Matrix4x4.Rotate(rotation);

        }
        
    }
    public void CalculateInverseKinematics()
    {
        Calc_IK_conf = _propertyProvider.JOG_IK_Configuration;
        try
        {
            // Transformation matrix of the ik point
            /*
            Matrix4x4 T = new Matrix4x4(new Vector4(1, 0, 0, ox),
                                        new Vector4(0, 0, 1, oy),
                                        new Vector4(0, -1, 0, oz),
                                        new Vector4(0, 0, 0, 1));
            */

            /*Matrix4x4 T = new Matrix4x4(new Vector4(-1, 0, 0, XYZ.x),
                                        new Vector4(0, 1, 0, XYZ.y),
                                        new Vector4(0, 0, -1, XYZ.z),
                                        new Vector4(0, 0, 0, 1));*/
            Matrix4x4 T = CreateTransform();

            // Rotation matrix of the ik point
            Matrix4x4 R = new Matrix4x4(new Vector4(T[0, 0], T[1, 0], T[2, 0], 0),
                                        new Vector4(T[0, 1], T[1, 1], T[2, 1], 0),
                                        new Vector4(T[0, 2], T[1, 2], T[2, 2], 0),
                                       new Vector4(0, 0, 0, 1));

            Vector3 o = new Vector3(_propertyProvider.XYZ.x, _propertyProvider.XYZ.y, _propertyProvider.XYZ.z); // точка расчета ИК

            float xc;
            float yc;
            float zc;
            switch (Calc_IK_conf)
            {
                case IK_config.conf_1:
                    xc = o.x - L6 * R[2, 0];
                    yc = o.y - L6 * R[2, 1];
                    zc = o.z - L6 * R[2, 2];
                    break;
                case IK_config.conf_2:
                     xc = o.x + L6 * R[2, 0];
                     yc = o.y + L6 * R[2, 1];
                     zc = o.z + L6 * R[2, 2];
                    break;
                default:
                    xc = o.x - L6 *R[2, 0];
                    yc = o.y - L6 * R[2, 1];
                    zc = o.z - L6 * R[2, 2];
                    break;
            }
            
            
            // calculate thetha1
            thetha[0] = Mathf.Atan2(yc, xc) * Mathf.Rad2Deg;

            // calculate thetha3
            float a = Mathf.Sqrt(L4 * L4 + L5 * L5);
            float r = Mathf.Sqrt(xc * xc + yc * yc) - L1;
            float s = zc - L2;
            float b = Mathf.Sqrt(r * r + s * s);
            float D = (L3 * L3 + a * a - b * b) / (2 * L3 * a);
            float fi = Mathf.Acos(D) * Mathf.Rad2Deg;
            float beta = 180 - fi;
            float alpha = Mathf.Atan2(L4, L5) * Mathf.Rad2Deg;
            // thetha3 = beta - alpha + 90; // Config I
            thetha[2] = -(beta + alpha) + 90; // Config II

            // calculate thetha2
            float fi2 = Mathf.Atan2(s, r) * Mathf.Rad2Deg;
            float D1 = (L3 * L3 + b * b - a * a) / (2 * L3 * b);
            float fi1 = Mathf.Acos(D1) * Mathf.Rad2Deg;
            // thetha2 = fi2 - fi1; // Config I
            thetha[1] = fi2 + fi1; // Config II

            // calculate thetha4, thetha5, thetha6

            float alpha1 = 90, alpha2 = 0, alpha3 = 90, r1 = L1, r2 = L3, r3 = L4, d1 = L2, d2 = 0, d3 = 0;
            // matrix T1
            Matrix4x4 T1 = new Matrix4x4(new Vector4(Mathf.Cos(thetha[0] * Mathf.Deg2Rad), -Mathf.Sin(thetha[0] * Mathf.Deg2Rad) * Mathf.Cos(alpha1 * Mathf.Deg2Rad), Mathf.Sin(thetha[0] * Mathf.Deg2Rad) * Mathf.Sin(alpha1 * Mathf.Deg2Rad), r1 * Mathf.Cos(thetha[0] * Mathf.Deg2Rad)),
                                        new Vector4(Mathf.Sin(thetha[0] * Mathf.Deg2Rad), Mathf.Cos(thetha[0] * Mathf.Deg2Rad) * Mathf.Cos(alpha1 * Mathf.Deg2Rad), -Mathf.Cos(thetha[0] * Mathf.Deg2Rad) * Mathf.Sin(alpha1 * Mathf.Deg2Rad), r1 * Mathf.Sin(thetha[0] * Mathf.Deg2Rad)),
                                        new Vector4(0, Mathf.Sin(alpha1 * Mathf.Deg2Rad), Mathf.Cos(alpha1 * Mathf.Deg2Rad), d1),
                                        new Vector4(0, 0, 0, 1));

            // matrix T2
            Matrix4x4 T2 = new Matrix4x4(new Vector4(Mathf.Cos(thetha[1] * Mathf.Deg2Rad), -Mathf.Sin(thetha[1] * Mathf.Deg2Rad) * Mathf.Cos(alpha2 * Mathf.Deg2Rad), Mathf.Sin(thetha[1] * Mathf.Deg2Rad) * Mathf.Sin(alpha2 * Mathf.Deg2Rad), r2 * Mathf.Cos(thetha[1] * Mathf.Deg2Rad)),
                                        new Vector4(Mathf.Sin(thetha[1] * Mathf.Deg2Rad), Mathf.Cos(thetha[1] * Mathf.Deg2Rad) * Mathf.Cos(alpha2 * Mathf.Deg2Rad), -Mathf.Cos(thetha[1] * Mathf.Deg2Rad) * Mathf.Sin(alpha2 * Mathf.Deg2Rad), r2 * Mathf.Sin(thetha[1] * Mathf.Deg2Rad)),
                                        new Vector4(0, Mathf.Sin(alpha2 * Mathf.Deg2Rad), Mathf.Cos(alpha2 * Mathf.Deg2Rad), d2),
                                        new Vector4(0, 0, 0, 1));

            // matrix T3
            Matrix4x4 T3 = new Matrix4x4(new Vector4(Mathf.Cos(thetha[2] * Mathf.Deg2Rad), -Mathf.Sin(thetha[2] * Mathf.Deg2Rad) * Mathf.Cos(alpha3 * Mathf.Deg2Rad), Mathf.Sin(thetha[2] * Mathf.Deg2Rad) * Mathf.Sin(alpha3 * Mathf.Deg2Rad), r3 * Mathf.Cos(thetha[2] * Mathf.Deg2Rad)),
                                        new Vector4(Mathf.Sin(thetha[2] * Mathf.Deg2Rad), Mathf.Cos(thetha[2] * Mathf.Deg2Rad) * Mathf.Cos(alpha3 * Mathf.Deg2Rad), -Mathf.Cos(thetha[2] * Mathf.Deg2Rad) * Mathf.Sin(alpha3 * Mathf.Deg2Rad), r3 * Mathf.Sin(thetha[2] * Mathf.Deg2Rad)),
                                        new Vector4(0, Mathf.Sin(alpha3 * Mathf.Deg2Rad), Mathf.Cos(alpha3 * Mathf.Deg2Rad), d3),
                                        new Vector4(0, 0, 0, 1));

            // matrix T03
            Matrix4x4 T03 = T1.transpose * T2.transpose * T3.transpose;

            Matrix4x4 R03 = new Matrix4x4(T03.GetColumn(0), T03.GetColumn(1), T03.GetColumn(2), Vector4.zero);

            // matrix R03T
            Matrix4x4 R03T = R03.transpose;

            // matrix R36
            Matrix4x4 R36 = R03T * T.transpose;

            // calculate thetha4
            

            // calculate thetha5
            switch (Calc_IK_conf)
            {
                case IK_config.conf_1:
                    thetha[3] = Mathf.Atan2(-R36[1, 2], -R36[0, 2]) * Mathf.Rad2Deg;
                    thetha[4] = Mathf.Acos(-R36[2, 2]) * Mathf.Rad2Deg - 180;
                    thetha[5] = Mathf.Atan2(-R36[2, 1], R36[2, 0]) * Mathf.Rad2Deg;
                    break;
                case IK_config.conf_2:
                    thetha[3] = 0;
                    thetha[4] = 0;
                    thetha[5] = 0;
                    /* thetha[3] = Mathf.Atan2(-R36[1, 2], -R36[0, 2]) * Mathf.Rad2Deg-180;
                     thetha[4] = Mathf.Acos(-R36[2, 2]) * Mathf.Rad2Deg;
                     thetha[5] = Mathf.Atan2(-R36[2, 1], R36[2, 0]) * Mathf.Rad2Deg;*/
                    break;
                default:
                    thetha[3] = Mathf.Atan2(-R36[1, 2], -R36[0, 2]) * Mathf.Rad2Deg;
                    thetha[4] = Mathf.Acos(-R36[2, 2]) * Mathf.Rad2Deg - 180;
                    thetha[5] = Mathf.Atan2(-R36[2, 1], R36[2, 0]) * Mathf.Rad2Deg;
                    break;
            }

            // calculate thetha6
            //thetha[5] = !nonCalcThetha5 ? Mathf.Atan2(-R36[2, 1], R36[2, 0]) * Mathf.Rad2Deg : thetha[5];
            
            thetha[2] = -thetha[2] + 90;

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Ошибка в расчетах ИК!" + e);
        }
    }
    public bool checkIsNaN()
    {
        if (float.IsNaN(thetha[0]) || float.IsNaN(thetha[1]) || float.IsNaN(thetha[2]) || float.IsNaN(thetha[3]) ||
        float.IsNaN(thetha[4]) || float.IsNaN(thetha[5]))
        {
            UnityEngine.Debug.LogError("Выход за пределы расчетов!");
            return false;
        }
        else
        {
            return true;
        }
    }
    public float InverseSmoothStep(float s)
    {
        if (s <= 0f) return 0f;
        if (s >= 1f) return 1f;

        float left = 0f;
        float right = 1f;
        float t;

        // Точность до 10^-6
        for (int i = 0; i < 20; i++)
        {
            t = (left + right) * 0.5f;
            float value = t * t * (3f - 2f * t);

            if (value < s)
                left = t;
            else
                right = t;
        }

        return (left + right) * 0.5f;
    }
    public float Curva(float t)
    {
        // плавный S-curve
        //float s = t * t * (3 - 2 * t);
        float s = InverseSmoothStep(t);
        if (s > 0.5f)
        {
            return 1f - Mathf.Clamp(s, 0.1f, 1f);
        }
        return Mathf.Clamp(s, 0.1f, 1f);
    }
    public float Curva2(float t)
    {
        return t * t * (3 - 2 * t);
    }

    private void ModifyRobot(float[] angles)
    {
        _propertyProvider.J1Angle = (old_thetha[0] += angles[0]);
        _propertyProvider.J2Angle = (old_thetha[1] += angles[1]);
        _propertyProvider.J3Angle = (old_thetha[2] += angles[2]);
        _propertyProvider.J4Angle = (old_thetha[3] += angles[3]);
        _propertyProvider.J5Angle = (old_thetha[4] += angles[4]);
        _propertyProvider.J6Angle = (old_thetha[5] += angles[5]);
    }
    public bool CheckAngle()
    {
        if (checkIsNaN())
        {
            CheckLimit();
            for (int i = 0; i < 5; i++)
            {
                step_thetha[i] = (thetha[i] - old_thetha[i]);
            }
            step_thetha[5] = (((thetha[5] - old_thetha[5]) % 360 + 540) % 360 - 180);
            ModifyRobot(step_thetha);
            return true;

        }
        else
        {
            return false;
        }
    }

    public void CheckLimit()
    {
        if (thetha[0] < -175)
        {
            thetha[0] = -175;
            UnityEngine.Debug.LogWarning("ОГР А1");
        }
        if (thetha[0] > 175)
        {
            thetha[0] = 175;
            UnityEngine.Debug.LogWarning("ОГР А1");
        }
        if (thetha[1] < 20)
        {
            thetha[1] = 20;
            UnityEngine.Debug.LogWarning("ОГР А2");
        }
        if (thetha[1] > 140)
        {
            thetha[1] = 140;
            UnityEngine.Debug.LogWarning("ОГР А2");
        }
        if (thetha[2] > 170)
        {
            thetha[2] = 170;
            UnityEngine.Debug.LogWarning("ОГР А3");
        }
        if (thetha[2] < -60)
        {
            thetha[2] = -60;
            UnityEngine.Debug.LogWarning("ОГР А3");
        }
        if (thetha[3] < -180)
        {
            thetha[3] = -180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        }
        if (thetha[3] > 180)
        {
            thetha[3] = 180;
            UnityEngine.Debug.LogWarning("ОГР А4");
        }
        if (thetha[4] < -105)
        {
            thetha[4] = -105;
            UnityEngine.Debug.LogWarning("ОГР А5");
        }
        if (thetha[4] > 105)
        {
            thetha[4] = 105;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }

        if (thetha[5] < -180)
        {
            thetha[5] = -180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }
        if (thetha[5] > 180)
        {
            thetha[5] = 180;
            UnityEngine.Debug.LogWarning("ОГР А6");
        }


    }

}
