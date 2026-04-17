using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static UnityEngine.EventSystems.EventTrigger;
public class RobotElement
{
    [SerializeField]
    public string Id;
    public ObjectType Type;
    public GameObject Reference;
    public List<RobotElement> Children = new List<RobotElement>();
}
public class RobotPropertyProvider : BasePropertyProvider
{
    private SceneObjectsManager _sceneObjectManager;
    
    public RobotController RobotController { get; set; }
    public float RotSpeedPercent { get; set; } = 100f;

    //ugli obnovlaemie in FixedUpdate
    public float J1Angle = 0;
    public float J2Angle = 90;
    public float J3Angle = 90;
    public float J4Angle = 0;
    public float J5Angle = 0;
    public float J6Angle = 0;
    public bool EndEffectorOn { get; set; }
    public float SpeedEffector = 0.5f;
    //parameters zveniev
    public float L1 = 450;
    public float L2 = 447;
    public float L3 = 1150;
    public float L4 = 350;
    public float L5 = 1350;
    public float L6 = 550;

    //IK
    public Vector3 XYZ = Vector3.zero;
    public Vector3 oldXYZ = Vector3.zero;
    public Quaternion XYZRot;
    public Quaternion oldXYZRot = Quaternion.identity;
    public Vector3 absoluteXYZ => GetAbsolutePosition(XYZ);
    public Vector3 absoluteOldXYZ => GetAbsolutePosition(oldXYZ);

    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };
    /// <summary>
    //public Quaternion XYZ_robot_Rotate;
    /// </summary>

    //JOG
    public JOGPropertyProvider JOGpoint;

    //public IK_config JOG_IK_Configuration = IK_config.conf_1;

    //ogranicheniya anglesSpeed
    private float[] ogrAngleSpeed = { 140, 93, 108, 205, 295, 465 };

    //Статус робота при выполнении программы
    
    /// <summary>
    /// reset position end Effector
    /// </summary>
    public void ResetPositionEffector()
    {
        XYZ = Vector3.zero;
        oldXYZ = Vector3.zero;
    }
    private void Start()
    {
        RobotController = GetComponent<RobotController>();
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        displayScale = false;
    }
    public void SetRobotId(string id)
    {
        Id = id;
    }
    /// <summary>
    /// получение дерева программы
    /// </summary>
    public List<RobotProgrammElement> Programm
    {
        get
        {
            var a = BuildTreeInternal(Id);
            return a;
        }
    }

    private List<RobotProgrammElement> BuildTreeInternal(string parentId)
    {
        List<RobotProgrammElement> programm = new();
        List<RobotProgramObject> itemsDict = _sceneObjectManager.Commands.GetSubPrograms(parentId);
        if (itemsDict == null) return null;

        // Сначала собираем всех детей в правильном порядке
        /*List<SceneObject> childrenInOrder = new();

        foreach (CommandObject entry in itemsDict)
        {
            var sceneObj = entry as SceneObject;
            if (sceneObj != null &&
                sceneObj.Reference.activeSelf == true &&
                sceneObj.ParentId == parentId &&
                (sceneObj.Type == ObjectType.LinearMoveCommand ||
                 sceneObj.Type == ObjectType.StateEndEffectorCommand ||
                 sceneObj.Type == ObjectType.WaitCommand ||
                 sceneObj.Type == ObjectType.Program))
            {
                childrenInOrder.Add(sceneObj);
            }
        }
        // Теперь обрабатываем в правильном порядке
        foreach (var child in childrenInOrder)
        {
            ConvertToRobotProgrammElement(child, programm);
        }*/
        foreach (var item in itemsDict) 
        {
            var subProgram = new SubProgramm(new List<RobotProgrammElement>(), ENUM_COMMANDS.SUBPROGRAMM, item.Id);
            foreach (var item2 in item.Items)
            {
                ConvertToRobotProgrammElement(item2, subProgram.ProgrammElement);
            }
            programm.Add(subProgram);
        }
        return programm;
    }

    private void ConvertToRobotProgrammElement(CommandObject obj, List<RobotProgrammElement> programm)
    {
        if (obj.Type == ObjectType.LinearMoveCommand)
        {
            var command = new CommandMove(obj.Reference.GetComponent<LinearPointPropertyProvider>(), ENUM_COMMANDS.MOVE_LIN, obj.Id);
            programm.Add(command);
        }
        else if (obj.Type == ObjectType.StateEndEffectorCommand)
        {
            var command = new ComandSetStateEndEffector(obj.Reference.GetComponent<StateEndEffectorPropertyProvider>(), ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR, obj.Id);
            programm.Add(command);
        }
        else if (obj.Type == ObjectType.WaitCommand)
        {
            var command = new CommandWait(obj.Reference.GetComponent<WaitPropertyProvider>(), ENUM_COMMANDS.WAIT, obj.Id);
            programm.Add(command);
        }
    }






    //------------------------------------------------------------------------------------------------------------------------//

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
        };
    }

    public override IEnumerable<CustomProperty> GetCustomProperties()
    {
        return null;
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        
    }

    private Vector3  GetAbsolutePosition(Vector3 pos)
    {
 
        Vector3 point = new Vector3();

        //point.x = pos.y / 1000;
        //point.z = pos.x / 1000;
        //point.y = pos.z / 1000;

        return transform.TransformPoint(point); ;
    }
}

public enum IK_config
{
    conf_1,
    conf_2
}