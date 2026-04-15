using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Rendering;
public class RobotController : MonoBehaviour
{
    private RobotPropertyProvider _propertyProvider;
    private EventBus _eventBus;
    private float Speed;//м/с
    private Vector3 point;
    private Vector3 oldJOGposition = Vector3.zero;
    private Quaternion oldJOGrotation = Quaternion.identity;
    private Point EffectorPosition;
    private Angles[] angles;
    private JOGPropertyProvider _JOGProvider;
    private SimulationManager _simManager;

    public bool RunTask { get; set; }
    private bool CommandComplete = true;

    public AnimationCurve SpeedCurve;
    public InverseK_new InvKin;
    
    void Start()
    {
        _simManager = ServiceManager.Current.Get<SimulationManager>();
        InvKin = gameObject.GetComponent<InverseK_new>();
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<StopProgramm>(StopSim);
        _propertyProvider = GetComponent<RobotPropertyProvider>();
        _JOGProvider = _propertyProvider.JOGpoint;
        //ik = new IK(_propertyProvider);
        //_propertyProvider.XYZ = new Vector3(1,1,1);
        //_propertyProvider.oldXYZ = _propertyProvider.XYZ;
        _propertyProvider.JOGpoint.LocalPosition = new Vector3(1, 1, 1);
        SetJogMove();  
    }
    private void FixedUpdate()
    {
        if (_simManager.GetModeSim() == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
        {
            SetJogMove();
        }
    }
    /// <summary>
    /// Вып. команды ожидания
    /// </summary>
    public void RobotSetWait(WaitPropertyProvider cmd)
    {
        StartCoroutine(SetWait(cmd.Get()));
    }
    private IEnumerator SetWait(float time)
    {
        yield return new WaitForSeconds(time*Time.fixedDeltaTime);
        _eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
    }
    /// <summary>
    /// Вып. команды изменения состояния эффектора
    /// </summary>
    public void RobotSetStateEndEffector(StateEndEffectorPropertyProvider cmd)
    {
        _propertyProvider.EndEffectorOn = cmd.Get();
        //_eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
        CommandComplete = true;
    }
    /// <summary>
    /// Вып. команды линейного движения
    /// </summary>
    public void RobotSetLinMove(LinearPointPropertyProvider point)
    {
        StartCoroutine(LinMove(point));
    }
    /// <summary>
    /// Движение в режиме JOG
    /// </summary>
    /// <param name="point"></param>
    public void SetJogMove()
    {
        if (oldJOGposition != _JOGProvider.LocalPosition || oldJOGrotation != _JOGProvider.LocalRotationQ)
        {
            EffectorPosition =  InvKin.Translate(_JOGProvider.LocalPosition, _JOGProvider.LocalRotationQ);
            angles = InvKin.IKCalc();
            //добавить автовыбор конфигурации или ручной ввод
            InvKin.CheckLimit(angles[0]);
            if (InvKin.checkIsNaN(angles[0]))
            {
                ModifyRobot(_propertyProvider, angles[0]);
                _propertyProvider.XYZ = _propertyProvider.oldXYZ = EffectorPosition.Position;
                _propertyProvider.XYZRot = _propertyProvider.oldXYZRot = EffectorPosition.Rotation;
            }
            oldJOGposition = _JOGProvider.LocalPosition;
            oldJOGrotation = _JOGProvider.LocalRotationQ;

        }

    }

    /// <summary>
    /// Мгновенное перемещение к позиции точки
    /// </summary>
    /// <param name="p"></param>
    public void TeleportToPoint(LinearPointPropertyProvider p)
    {
        EffectorPosition = InvKin.Translate(GetPositionInfo(p));
        angles = InvKin.IKCalc();
        //Выбор конфигурации точки
        InvKin.CheckLimit(angles[0]);
        if (InvKin.checkIsNaN(angles[0]))
        {
            ModifyRobot(_propertyProvider, angles[0]);
            _propertyProvider.XYZ = EffectorPosition.Position;
            _propertyProvider.XYZRot = EffectorPosition.Rotation;

        }
        _propertyProvider.oldXYZ = _propertyProvider.XYZ;
        _propertyProvider.oldXYZRot = _propertyProvider.XYZRot;
        SyncJogPos();
    }
    public Point GetPositionInfo(LinearPointPropertyProvider p)
    {
        //point = _propertyProvider.transform.InverseTransformPoint(p.LocalPosition);////!!!

        Speed = p.Speed;
        return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.transform.rotation, Speed = Speed };

        //_propertyProvider.absoluteXYZ = p.Position;
        //PointType = p.pointType;
    }


    /// <summary>
    /// Линейное движение к точке
    /// </summary>
    /// <returns></returns>
    IEnumerator LinMove(LinearPointPropertyProvider point)
    {
        Vector3 start = _propertyProvider.XYZ;
        Quaternion startRot = _propertyProvider.XYZRot;
        Quaternion endRot = GetPositionInfo(point).Rotation;
        Vector3 currentXYZ = start;
        Vector3 end = InvKin.Translate(GetPositionInfo(point)).Position;
        Vector3 direction = (end - start).normalized;
        Quaternion currentRot = Quaternion.identity;
        float distance = Vector3.Distance(start, end);
        float traveled = 0f;

        ///time
        float timeInWay = distance / Speed;
        float timeCurrent = 0;
        float timeCurrenScale = 0;
        //Сделать проверку точки на доступность, если точка недоступна, то не выполнять движение и выдавать ошибку

        
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
            angles = InvKin.IKCalc();

            //ik.CalculateInverseKinematics();
            //CheckAngle();

            traveled = positionInLine;
            //
            //Выбор конфигурации точки
            InvKin.CheckLimit(angles[0]);
            if (InvKin.checkIsNaN(angles[0]))
            {
                ModifyRobot(_propertyProvider, angles[0]);

            }
            _propertyProvider.XYZ = currentXYZ;
            _propertyProvider.XYZRot = currentRot;
            //_propertyProvider.oldXYZ = _propertyProvider.XYZ;
            // _propertyProvider.oldXYZRot = _propertyProvider.XYZRot;
            //

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

        SyncJogPos();
        //_eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
        CommandComplete = true;
    }
    public void StopSim(StopProgramm s)
    {
        StopAllCoroutines();
        SyncJogPos();
        RunTask = false;
        CommandComplete = true;
    }
    /// <summary>
    /// перемещает точку JOG в позицию эффектора, используется для синхронизации позиции точки JOG при выполнении других типов движения
    /// </summary>
    public void SyncJogPos()
    {
        //_propertyProvider.JOGpoint.GlobalPosition = _propertyProvider.absoluteXYZ;
        //_propertyProvider.JOGpoint.GlobalRotationQ = _propertyProvider.XYZRot;
        _propertyProvider.JOGpoint.LocalPosition = _propertyProvider.XYZ;
        _propertyProvider.JOGpoint.LocalRotationQ = _propertyProvider.XYZRot;
    }
    /// <summary>
    /// Ручное управление
    /// </summary>
    /// 
    public void ModifyRobot(RobotPropertyProvider _propertyProvider, Angles ang)
    {
        _propertyProvider.J1Angle = ang.thetha1;
        _propertyProvider.J2Angle = ang.thetha2;
        _propertyProvider.J3Angle = ang.thetha3;
        _propertyProvider.J4Angle = ang.thetha4;
        _propertyProvider.J5Angle = ang.thetha5;
        _propertyProvider.J6Angle = ang.thetha6;
    }

    public void RunSubProgramm(RobotProgrammElement RPE)
    {
      StartCoroutine(Run(RPE));
    }
    public IEnumerator Run(RobotProgrammElement RPE)
    {
        SubProgramm sub;
        if (RPE != null && RPE.TypeComand == ENUM_COMMANDS.SUBPROGRAMM)
        {
            sub = RPE as SubProgramm;
            foreach (var element in sub.CommandsElements)
            {
                yield return new WaitUntil(() => CommandComplete && _simManager.GetStatusSim() == SIM_STAT.PLAY);
                CommandComplete = false;
                element.Execute(this);
                
            }
            _propertyProvider.RobotController.RunTask = false;
        }
        else
        {
            UnityEngine.Debug.LogError("Ошибка: переданный элемент не является подпрограммой.");
        }
    }
}

