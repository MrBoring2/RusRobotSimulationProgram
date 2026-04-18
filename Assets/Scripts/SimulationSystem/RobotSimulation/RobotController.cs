using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
public class RobotController : MonoBehaviour
{
    private RobotPropertyProvider _propertyProvider;
    private SimulationManager _simManager => ServiceManager.Current.Get<SimulationManager>();
    private SceneObjectsManager _sceneObjectManager => ServiceManager.Current.Get<SceneObjectsManager>();
    private EventBus _eventBus => ServiceManager.Current.Get<EventBus>();

    private float Speed;//м/с
    
    private Point EffectorPosition;
    private Angles[] angles;

    //
    private Vector3 oldJOGposition = Vector3.zero;
    private Quaternion oldJOGrotation = Quaternion.identity;
    private float[] oldAngles = new float[6] { 0, 0, 0, 0, 0, 0 };


    public string ID => _propertyProvider.Id;
    public bool RunTask { get; set; }
    private bool CommandComplete = true;

    public AnimationCurve SpeedCurve;
    private InverseK_new InvKin;
    
    void Start()
    {
        InvKin = gameObject.GetComponent<InverseK_new>();
        _eventBus.Subscribe<StopProgramm>(StopSim);
        _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);
        _propertyProvider = GetComponent<RobotPropertyProvider>();
        _propertyProvider.JOGpoint.LocalPosition = new Vector3(1, 1, 1);
        SetJogMove();  
    }
    private void FixedUpdate()
    {
        if (_simManager.GetModeSim() == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SetJogMove();
        }
        else if(_simManager.GetModeSim() == MODE.ANGLES_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SetAngleMove();
        }
    }
    //--Команда ожидания--
    public void RobotSetWait(WaitPropertyProvider cmd)
    {
        StartCoroutine(SetWait(cmd.Get()));
    }
    private IEnumerator SetWait(float time)
    {
        yield return new WaitForSeconds(time);
        //_eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
        CommandComplete = true;
    }
    //--Команда изменения состояния эффектора
    public void RobotSetStateEndEffector(StateEndEffectorPropertyProvider cmd)
    {
        _propertyProvider.EndEffectorOn = cmd.Get();
        //_eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
        CommandComplete = true;
    }
    //--Команда линейное движение
    public void RobotSetLinMove(LinearPointPropertyProvider point)
    {
        StartCoroutine(LinMove(point));
    }
    //--Задать позицию ДЖОГа
    public void SetJogMove()
    {
        if (oldJOGposition != _propertyProvider.JOGpoint.LocalPosition || oldJOGrotation != _propertyProvider.JOGpoint.LocalRotationQ)
        {
            angles = InvKin.IKCalc(_propertyProvider.RP, _propertyProvider.JOGpoint.LocalPosition, _propertyProvider.JOGpoint.LocalRotationQ);
            //добавить автовыбор конфигурации или ручной ввод
            InvKin.CheckLimit(angles[0]);
            if (InvKin.checkIsNaN(angles[0]))
            {
                ModifyRobot(_propertyProvider, angles[0].GetFloats());
                angles[0].GetFloats().CopyTo(_propertyProvider.ChangeAngles, 0);
                //_propertyProvider.XYZ = _propertyProvider.oldXYZ = EffectorPosition.Position;
                //_propertyProvider.XYZRot = _propertyProvider.oldXYZRot = EffectorPosition.Rotation;
            }
            oldJOGposition = _propertyProvider.JOGpoint.LocalPosition;
            oldJOGrotation = _propertyProvider.JOGpoint.LocalRotationQ;

        }

    }
    public void SetAngleMove()
    {
        if(!oldAngles.SequenceEqual(_propertyProvider.ChangeAngles))
        ModifyRobot(_propertyProvider, _propertyProvider.ChangeAngles);
        Point position = _propertyProvider.GetActualPosEffector();
        _propertyProvider.JOGpoint.GlobalPosition = position.Position;
        _propertyProvider.JOGpoint.GlobalRotationQ = position.Rotation;
        _propertyProvider.ChangeAngles.CopyTo(oldAngles, 0);

    }
    //--Мгновенное перемещение к переданной точке
    private void TeleportToPoint(PickCommandSignal s)
    {
        if (_simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SceneObject obj = s.Point;
            if (obj.Type == ObjectType.LinearMoveCommand && (ServiceManager.Current.Get<SceneObjectsManager>().Commands.GetSubProgram(obj.ParentId).ParentId == ID))
            {
                if (s.Point.Type == ObjectType.LinearMoveCommand)
                {
                    TeleportToPoint((LinearPointPropertyProvider)s.Point.PropertyProvider);
                }
            }
            /*else
            {
                if(obj.ParentId != null)
                {
                    if(obj.Type == ObjectType.LinearMoveCommand)
                        obj = _sceneObjectsManager.Commands.GetSubProgram(obj.ParentId);
                    else obj = _sceneObjectsManager.GetById(obj.ParentId);
                }

            }*/


        }
    }
    public void TeleportToPoint(LinearPointPropertyProvider p)
    {
        angles = InvKin.IKCalc(_propertyProvider.RP, GetPositionInfo(p).Position, GetPositionInfo(p).Rotation);
        //Выбор конфигурации точки
        InvKin.CheckLimit(angles[0]);
        if (InvKin.checkIsNaN(angles[0]))
        {
            ModifyRobot(_propertyProvider, angles[0].GetFloats());
            _propertyProvider.JOGpoint.LocalPosition = GetPositionInfo(p).Position;
            _propertyProvider.JOGpoint.GlobalRotationQ = GetPositionInfo(p).Rotation;

        }
    }
    //--Получить позицию точки--
    public Point GetPositionInfo(LinearPointPropertyProvider p)
    {
        Speed = p.Speed;
        return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.transform.rotation, Speed = Speed };
    }
    public Point GetPositionInfo(Point p)
    {
        Speed = p.Speed;
        return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.Rotation, Speed = Speed };
    }
    //--Корутина линейного движения--
    IEnumerator LinMove(LinearPointPropertyProvider point)
    {
        Vector3 start = _propertyProvider.JOGpoint.LocalPosition; //_propertyProvider.XYZ;
        Quaternion startRot = _propertyProvider.JOGpoint.LocalRotationQ; //_propertyProvider.XYZRot;
        Quaternion endRot = GetPositionInfo(point).Rotation;
        Vector3 currentXYZ = start;
        Vector3 end = GetPositionInfo(point).Position;
        Vector3 direction = (end - start).normalized;
        Quaternion currentRot = Quaternion.identity;
        float distance = Vector3.Distance(start, end);
        float traveled = 0f;

        ///time
        float timeInWay = distance / Speed;
        float timeCurrent = 0;
        float timeCurrenScale = 0;
        //Сделать проверку точки на достежимость, если точка недоступна, то не выполнять движение и выдавать ошибку

        
        while (traveled < distance)
        {

            //yield return new WaitUntil(()=>allowNextMove);
            /*float t0 = MathF.Floor((traveled / distance) * 100f) / 100f;

            float s = ik.Curva(t0);
            float g = Mathf.Min(1, distance - traveled);
            
            currentXYZ += direction * g;*/
            timeCurrenScale = timeCurrent / timeInWay;
            float positionInLine = SpeedCurve.Evaluate(timeCurrenScale) * distance;
            float step = positionInLine - traveled;
            currentXYZ += direction * step;
            
            // _propertyProvider.XYZRot = Quaternion.identity;
            currentRot = Quaternion.Lerp(startRot, endRot, SpeedCurve.Evaluate(timeCurrenScale));
            //UnityEngine.Debug.LogWarning("XYZ " + _propertyProvider.XYZRot.eulerAngles.y);

            InvKin.Translate(currentXYZ, currentRot);
            angles = InvKin.IKCalc(_propertyProvider.RP, _propertyProvider.JOGpoint.LocalPosition, _propertyProvider.JOGpoint.LocalRotationQ);

            //ik.CalculateInverseKinematics();
            //CheckAngle();

            traveled = positionInLine;
            //
            //Выбор конфигурации точки
            InvKin.CheckLimit(angles[0]);
            if (InvKin.checkIsNaN(angles[0]))
            {
                ModifyRobot(_propertyProvider, angles[0].GetFloats());

            }
           // _propertyProvider.XYZ = currentXYZ;
            //_propertyProvider.XYZRot = currentRot;
            _propertyProvider.JOGpoint.LocalPosition = currentXYZ;
            _propertyProvider.JOGpoint.LocalRotationQ = currentRot;

            //timeCurrent += Time.fixedDeltaTime;
            timeCurrent += Time.deltaTime;
            //yield return new WaitForSeconds((1f/Speed*s)/1000f);
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        //XYZRot = XYZBuf;
       /* _propertyProvider.oldXYZ = end;
        _propertyProvider.oldXYZRot = currentXYZRot;//изм название

        _propertyProvider.XYZ = end;
        _propertyProvider.XYZRot = currentXYZRot;//изм название*/

        //SyncJogPos();
        CommandComplete = true;
    }
    //--Перемещает точку JOG в позицию эффектора, используется для синхронизации позиции точки JOG--
    public void SyncJogPos(Point point)
    {
        _propertyProvider.JOGpoint.LocalPosition = point.Position;
        _propertyProvider.JOGpoint.LocalRotationQ = point.Rotation;
    }
    //--Изменить позицию модели робота--
    public void ModifyRobot(RobotPropertyProvider _propertyProvider, float[] ang)
    {
        _propertyProvider.J1Angle = ang[0];
        _propertyProvider.J2Angle = ang[1];
        _propertyProvider.J3Angle = ang[2];
        _propertyProvider.J4Angle = ang[3];
        _propertyProvider.J5Angle = ang[4];
        _propertyProvider.J6Angle = ang[5];
    }
    //--Выполнить подпрограмму (задачу)--
    public void RunSubProgramm(RobotProgrammElement RPE)
    {
      StartCoroutine(Run(RPE));
    }
    //--Корутина выполнение подпрограммы (задачи)--
    public IEnumerator Run(RobotProgrammElement RPE)
    {
        SubProgramm sub;
        if (RPE != null && RPE.TypeComand == ENUM_COMMANDS.SUBPROGRAMM)
        {
            sub = RPE as SubProgramm;
            foreach (var element in sub.ProgrammElement)
            {
                yield return new WaitUntil(() => CommandComplete && _simManager.GetStatusSim() == SIM_STAT.PLAY);
                CommandComplete = false;
                element.Execute(this);

            }
            _propertyProvider.RobotController.RunTask = false;
        }
        else
        {
            Debug.LogError("Ошибка: переданный элемент не является подпрограммой.");
        }
    }
    public void StopSim(StopProgramm s)
    {
        StopAllCoroutines();
        RunTask = false;
        CommandComplete = true;
    }

    /// <summary>
    /// получение дерева программы
    /// </summary>
    public List<RobotProgrammElement> Programm
    {
        get
        {
            var a = BuildTreeInternal(ID);
            return a;
        }
    }

    private List<RobotProgrammElement> BuildTreeInternal(string parentId)
    {
        List<RobotProgrammElement> programm = new();
        List<RobotProgramObject> itemsDict = _sceneObjectManager.Commands.GetSubPrograms(parentId);
        if (itemsDict == null) return null;
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
}

