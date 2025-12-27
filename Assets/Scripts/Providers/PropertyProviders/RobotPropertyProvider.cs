using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
public class RobotElement
{
    public string Id;
    public ObjectType Type;
    public GameObject Reference;
    public List<RobotElement> Children = new List<RobotElement>();
}
public class RobotPropertyProvider : BasePropertyProvider
{
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectManager;
    public IEnumerable<RobotProgrammElement> Programm 
    {
        get
        {
            var a = BuildTreeInternal(Id);
            return a;
        }
        
     }
    public float RotSpeedPercent { get; set; } = 100f;

    //Углы применяемые каждый FixedUpdate
    public float J1Angle = 0;
    public float J2Angle = 90;
    public float J3Angle = 90;
    public float J4Angle = 0;
    public float J5Angle = 0;
    public float J6Angle = 0;
    public bool EndEffectorOn = false;
    public float SpeedEffector = 0.5f;
    //длины звеньев
    public float L1 = 450;
    public float L2 = 447;
    public float L3 = 1150;
    public float L4 = 350;
    public float L5 = 1350;
    public float L6 = 550;
    public float SpeedPercent = 100f;

    //IK
    public Vector3 XYZ = Vector3.zero;
    public Vector3 oldXYZ = Vector3.zero;
    public Quaternion XYZRot;
    public Quaternion oldXYZRot = Quaternion.identity;
    public Vector3 absoluteXYZ = Vector3.zero;
    public Vector3 absoluteOldXYZ = Vector3.zero;
    //IK
    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };

    //JOG
    public LinearPointPropertyProvider JOGpoint;
    public void SyncJOGPosition()
    {
        JOGpoint.transform.position = absoluteXYZ;
    }

    private float[] ogrAngleSpeed = { 140, 93, 108, 205, 295, 465 }; //гр/с

    public event Action EndMoveEvent;
    public void EndMove()
    {
        EndMoveEvent?.Invoke();
    }

    public void ResetPositionEffector()
    {
        XYZ = Vector3.zero;
        oldXYZ = Vector3.zero;
    }

    //private List<Comand> Programm { get; set; } = new List<Comand>();
    //public void SetRobotProgramm(List<Comand> prog)
    //{
    //    Programm = prog;
    ////}
    //public List<Comand> GetProgramm()
    //{
    //    return Programm;
    //}
    //public void AddComand(ComandMove p)
    //{
    //    Programm.Add(p);
    //}
    //public void AddSubProgramm(SubProgramm p)
    //{
    //    Programm.Add(p);
    //}
    //public void AddComandInSubProgramm(SubProgramm sp, ComandMove c)
    //{
    //    (Programm.FirstOrDefault(p => p.ID == sp.ID) as SubProgramm).Addcomand(c);
    //}
    //public void AddSubprogrammInSubProgramm(SubProgramm sp, SubProgramm spAdd)
    //{
    //    (Programm.FirstOrDefault(p => p.ID == sp.ID) as SubProgramm).AddSubProgramm(spAdd);
    //}

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        displayScale = false;
    }
    public void SetRobotId(string id)
    {
        Id = id;
    }

    public IEnumerable<RobotProgrammElement> GetRootElements()
    {
        return _sceneObjectManager.Items.Values
             .Where(x => x.ParentId == Id &&
                         (x.Type == ObjectType.LinearMoveCommand || x.Type == ObjectType.StateEndEffectorCommand || x.Type == ObjectType.Program))
             .Select(x => ConvertToRobotProgrammElement(x));
    }

    //public void Comm()
    //{
    //    var root = BuildTreeInternal(Id);
    //    foreach (var element in root)
    //    {
    //        if (element is SubProgramm prog)
    //        {
    //            var elements = prog.Get();
    //            //рекурсивно обходилм
    //        }
    //        else if (element is RobotCommand comm)
    //        {
                
    //            //команду получили
    //        }
    //    }
    //}

    public IEnumerable<RobotProgrammElement> BuildTree()
    {
        return BuildTreeInternal(Id);
    }

    private IEnumerable<RobotProgrammElement> BuildTreeInternal(string parentId)
    {
        var children = _sceneObjectManager.Items.Values
            .Where(x => x.Reference.activeSelf == true && x.ParentId == parentId &&
                        (x.Type == ObjectType.LinearMoveCommand || x.Type == ObjectType.StateEndEffectorCommand || x.Type == ObjectType.Program));

        foreach (var child in children)
        {
            var node = ConvertToRobotProgrammElement(child);
            yield return node;

            if (node is SubProgramm subProgramm)
            {
                foreach (var subChild in subProgramm.Get())
                {
                    yield return subChild;
                }
            }
        }
    }
    private RobotProgrammElement ConvertToRobotProgrammElement(SceneObject obj)
    {
        if (obj.Type == ObjectType.LinearMoveCommand)
        {
            var command = new CommandMove(obj.Reference.GetComponent<LinearPointPropertyProvider>(), ENUM_COMANDS.MOVE_LIN, obj.Id);
            return command;
        }
        else if(obj.Type == ObjectType.StateEndEffectorCommand)
        {
            var command = new ComandSetStateEndEffector(obj.Reference.GetComponent<StateEndEffectorPropertyProvider>(), ENUM_COMANDS.CHANGE_STATE_ENDEFFECTOR, obj.Id);
            return command;
        }
        else if (obj.Type == ObjectType.Program)
        {
            var subProgram = new SubProgramm(
                _sceneObjectManager.Items.Values
                    .Where(x => x.ParentId == obj.Id && (x.Type == ObjectType.LinearMoveCommand || x.Type == ObjectType.StateEndEffectorCommand))
                    .Select(x => ConvertToRobotProgrammElement(x))
                    .ToList(),
                ENUM_COMANDS.SUBPROGRAMM,
                obj.Id
            );
            return subProgram;
        }
        return null;
    }

    /// <summary>
    /// Класс для многоуровневой структуры
    /// </summary>


    //public void AddProgram(SubProgramm prog)
    //{
    //    robotProgramms.Add(prog);
    //    _eventBus.Invoke(new AddProgram());
    //}
    //public void AddProgram(string parentId, SubProgramm progChild)
    //{
    //    var realParent = FindProgramRecursive(parentId);
    //    if (realParent == null)
    //    {
    //        Debug.LogError("Parent program not found");
    //        return;
    //    }

    //    realParent.AddSubProgramm(progChild);
    //    _eventBus.Invoke(new AddProgram());
    //}
    //public void AddCommand(string progId, RobotCommand comm)
    //{
    //    var parent = FindProgramRecursive(progId);
    //    if (parent == null)
    //    {
    //        Debug.LogWarning($"Program {progId} not found");
    //        return;
    //    }

    //    parent.ADDcomand(comm);
    //    _eventBus.Invoke(new AddCommand());
    //}

    //public SubProgramm FindProgramRecursive(string id)
    //{
    //    foreach (var elem in robotProgramms)
    //    {
    //        if (elem is SubProgramm prog)
    //        {
    //            var found = FindInProgram(prog, id);
    //            if (found != null)
    //                return found;
    //        }
    //    }
    //    return null;
    //}

    //private SubProgramm FindInProgram(SubProgramm prog, string id)
    //{
    //    if (prog.ID == id)
    //        return prog;

    //    foreach (var child in prog.Get())
    //    {
    //        if (child is SubProgramm childProg)
    //        {
    //            var found = FindInProgram(childProg, id);
    //            if (found != null)
    //                return found;
    //        }
    //    }
    //    return null;
    //}

    //public void AddCommand(RobotCommand comm)
    //{
    //    robotProgramms.Add(comm);
    //    _eventBus.Invoke(new AddCommand());
    //}
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
            FloatValues =
            {
                ["RotSpeedPercent"] = RotSpeedPercent
            }
        };
    }

    public override IEnumerable<CustomProperty> GetCustomProperties()
    {
        yield return new CustomProperty(
            "RotSpeedPercent",
            typeof(float),
            () => RotSpeedPercent,
            val => RotSpeedPercent = (float)val
        );
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.FloatValues.TryGetValue("RotSpeedPercent", out var v))
            RotSpeedPercent = v;
    }
}
