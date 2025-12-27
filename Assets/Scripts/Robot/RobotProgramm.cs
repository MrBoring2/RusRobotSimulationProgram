//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Security.Cryptography;
//using UnityEngine;

//public abstract class Comand
//{
//    public ENUM_COMANDS TypeComand;
//    public string ID {  get; set; }
//    public abstract void Execute(RobotController rc);
//}
//public class ComandMove : Comand
//{
//    private CmdLinMovePorpertyProvider Cmd { get; set; }
//    public ComandMove() { }
//    public ComandMove(CmdLinMovePorpertyProvider p, ENUM_COMANDS tc, string id) { 
//        Cmd = p;
//        ID = id;
//        TypeComand = tc;
//    }
    
//    public CmdLinMovePorpertyProvider Get()
//    {
//        return Cmd;
//    }
//    public override void Execute(RobotController rc)
//    {
//        rc.RobotSetLinMove(Cmd);
//    }
//}

//public class ComandSetStateEndEffector : Comand
//{
//    private CmdSetStateEndEffectorPropertyProvider Cmd { get; set; }
//    public ComandSetStateEndEffector() { }
//    public ComandSetStateEndEffector(CmdSetStateEndEffectorPropertyProvider p, ENUM_COMANDS tc, string id)
//    {
//        Cmd = p;
//        ID = id;
//        TypeComand = tc;
//    }

//    public CmdSetStateEndEffectorPropertyProvider Get()
//    {
//        return Cmd;
//    }
//    public override void Execute(RobotController rc)
//    {
//        rc.RobotSetStateEndEffector(Cmd);
//    }
//}
//public class SubProgramm : Comand
//{
//    private List<Comand> Programm { get; set; }= new List<Comand>();
//    public SubProgramm(ENUM_COMANDS tc) {
//        TypeComand = tc;
//    }
//    public SubProgramm(List<Comand> p, ENUM_COMANDS tc, string id)
//    {
//        Programm = p;
//        ID = id;
//        TypeComand = tc;
//    }
  
//    public void AddSubProgramm(SubProgramm sp)
//    {
//        Programm.Add(sp);
//    }
//    public void Addcomand(Comand c)
//    {
//        Programm.Add(c);
//    }
//    public void DeleteElement(Comand rpe)
//    {
//        Programm.Remove(rpe);
//    }
//    public List<Comand> Get()
//    {
//        return Programm;
//    }
//    public override void Execute(RobotController rc)
//    {
//        //rc.RobotSetLinMove(Cmd);
//    }

//}



//public enum SIM
//{
//    START,
//    STOP,
//    PAUSE,
//    RESUME,
//    NONE
//}
//public enum MODE
//{
//    STEP,
//    NO_STEP,
//    JOG_MODE,
//    NONE
//}
//public enum ENUM_COMANDS { 
//    MOVE_PTP,
//    MOVE_LIN,
//    WAIT,
//    CHANGE_STATE_ENDEFFECTOR,
//    SUBPROGRAMM
//}
