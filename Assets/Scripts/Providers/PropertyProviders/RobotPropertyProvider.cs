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

    //Óãëû ïðèìåíÿåìûå êàæäûé FixedUpdate
    public float J1Angle = 0;
    public float J2Angle = 90;
    public float J3Angle = 90;
    public float J4Angle = 0;
    public float J5Angle = 0;
    public float J6Angle = 0;
    public bool EndEffectorOn { get; set; }
    public float SpeedEffector = 0.5f;
    //äëèíû çâåíüåâ
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
    public Vector3 absoluteXYZ => GetAbsolutePosition(XYZ);
    public Vector3 absoluteOldXYZ => GetAbsolutePosition(oldXYZ);
    //IK
    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };

    //JOG
    public JOGPropertyProvider JOGpoint;
    public void SyncJOGPosition()
    {
        JOGpoint.GlobalPosition = absoluteXYZ;
        JOGpoint.RotationQ = XYZRot;
    }

    private float[] ogrAngleSpeed = { 140, 93, 108, 205, 295, 465 }; //ãð/ñ


    public void ResetPositionEffector()
    {
        XYZ = Vector3.zero;
        oldXYZ = Vector3.zero;
    }
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
        var itemsDict = _sceneObjectManager.Items;
        if (itemsDict == null) return Enumerable.Empty<RobotProgrammElement>();

        var result = new List<RobotProgrammElement>();

        // Ïåðåáèðàåì â ïîðÿäêå äîáàâëåíèÿ
        foreach (DictionaryEntry entry in itemsDict)
        {
            var sceneObj = entry.Value as SceneObject;
            if (sceneObj != null &&
                sceneObj.ParentId == Id &&
                (sceneObj.Type == ObjectType.LinearMoveCommand ||
                 sceneObj.Type == ObjectType.StateEndEffectorCommand ||
                 sceneObj.Type == ObjectType.WaitCommand ||
                 sceneObj.Type == ObjectType.Program))
            {
                result.Add(ConvertToRobotProgrammElement(sceneObj));
            }
        }

        return result;
    }


    public IEnumerable<RobotProgrammElement> BuildTree()
    {
        return BuildTreeInternal(Id);
    }

    private IEnumerable<RobotProgrammElement> BuildTreeInternal(string parentId)
    {
        var itemsDict = _sceneObjectManager.Items;
        if (itemsDict == null) yield break;

        // Ñíà÷àëà ñîáèðàåì âñåõ äåòåé â ïðàâèëüíîì ïîðÿäêå
        var childrenInOrder = new List<SceneObject>();

        foreach (DictionaryEntry entry in itemsDict)
        {
            var sceneObj = entry.Value as SceneObject;
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

        // Òåïåðü îáðàáàòûâàåì â ïðàâèëüíîì ïîðÿäêå
        foreach (var child in childrenInOrder)
        {
            var node = ConvertToRobotProgrammElement(child);
            yield return node;

            if (node is SubProgramm subProgramm)
            {
                // Ðåêóðñèâíî ïîëó÷àåì ýëåìåíòû ïîäïðîãðàììû
                var subChildren = BuildTreeInternal(child.Id).ToList();
                foreach (var subChild in subChildren)
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
            var command = new CommandMove(obj.Reference.GetComponent<LinearPointPropertyProvider>(), ENUM_COMMANDS.MOVE_LIN, obj.Id);
            return command;
        }
        else if(obj.Type == ObjectType.StateEndEffectorCommand)
        {
            var command = new ComandSetStateEndEffector(obj.Reference.GetComponent<StateEndEffectorPropertyProvider>(), ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR, obj.Id);
            return command;
        }
        else if (obj.Type == ObjectType.WaitCommand)
        {

            var command = new ComandWait(obj.Reference.GetComponent<WaitPropertyProvider>(), ENUM_COMMANDS.WAIT, obj.Id);
            return command;
        }
        else if (obj.Type == ObjectType.Program)
        {
            // Ïîëó÷àåì äî÷åðíèå ýëåìåíòû â ïðàâèëüíîì ïîðÿäêå
            var subItems = new List<RobotProgrammElement>();

            var itemsDict = _sceneObjectManager.Items;
            if (itemsDict != null)
            {
                foreach (DictionaryEntry entry in itemsDict)
                {
                    var sceneObj = entry.Value as SceneObject;
                    if (sceneObj != null &&
                        sceneObj.ParentId == obj.Id &&
                        (sceneObj.Type == ObjectType.LinearMoveCommand ||
                         sceneObj.Type == ObjectType.StateEndEffectorCommand ||
                         sceneObj.Type == ObjectType.WaitCommand))
                    {
                        subItems.Add(ConvertToRobotProgrammElement(sceneObj));
                    }
                }
            }

            var subProgram = new SubProgramm(subItems, ENUM_COMMANDS.SUBPROGRAMM, obj.Id);
            return subProgram;
        }
        return null;
    }

    /// <summary>
    /// Êëàññ äëÿ ìíîãîóðîâíåâîé ñòðóêòóðû
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

        point.x = pos.y / 1000;
        point.z = pos.x / 1000;
        point.y = pos.z / 1000;

        return transform.TransformPoint(point); ;
    }
}
