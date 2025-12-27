
//using Assets.Scripts.Providers;
//using Mono.Cecil;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UIElements;

//public class RobotPropertyProvider : BasePropertyProvider
//{

//    //”глы примен€емые каждый FixedUpdate
//    public float J1Angle = 0;
//    public float J2Angle = 90;
//    public float J3Angle = 90;
//    public float J4Angle = 0;
//    public float J5Angle = 0;
//    public float J6Angle = 0;
//    public bool EndEffectorOn = false;
//    public float SpeedEffector = 0.5f;
//    //длины звеньев
//    public float L1 = 450;
//    public float L2 = 447;
//    public float L3 = 1150;
//    public float L4 = 350;
//    public float L5 = 1350;
//    public float L6 = 550;
//    public float SpeedPercent = 100f;

//    //IK
//    public Vector3 XYZ = Vector3.zero;
//    public Vector3 oldXYZ = Vector3.zero;
//    public Quaternion XYZRot;
//    public Quaternion oldXYZRot = Quaternion.identity;
//    public Vector3 absoluteXYZ = Vector3.zero;
//    public Vector3 absoluteOldXYZ = Vector3.zero;
//    //IK
//    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
//    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
//    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };

//    //JOG
//    public CmdLinMovePorpertyProvider JOGpoint;
   
//    public void SyncJOGPosition()
//    {
//        JOGpoint.transform.position = absoluteXYZ;
//    }
//    private float[] ogrAngleSpeed = { 140, 93, 108, 205, 295, 465 }; //гр/с

//    public event Action EndMoveEvent;
//    public void EndMove()
//    {
//        EndMoveEvent?.Invoke();
//    }
    
//    public void ResetPositionEffector()
//    {
//        XYZ = Vector3.zero;
//        oldXYZ = Vector3.zero;
//    }

//    private List<Comand> Programm { get; set; } = new List<Comand>();
//    public void SetRobotProgramm(List<Comand> prog)
//    {
//        Programm = prog;
//    }
//    public List<Comand> GetProgramm()
//    {
//        return Programm;
//    }
//    public void AddComand(ComandMove p)
//    {
//        Programm.Add(p);
//    }
//    public void AddSubProgramm(SubProgramm p)
//    {
//        Programm.Add(p);
//    }
//    public void AddComandInSubProgramm(SubProgramm sp, ComandMove c)
//    {
//        (Programm.FirstOrDefault(p => p.ID == sp.ID) as SubProgramm).Addcomand(c);
//    }
//    public void AddSubprogrammInSubProgramm(SubProgramm sp, SubProgramm spAdd)
//    {
//        (Programm.FirstOrDefault(p => p.ID == sp.ID) as SubProgramm).AddSubProgramm(spAdd);
//    }
    
//}