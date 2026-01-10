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

public class RobotController : MonoBehaviour
{
    private RobotPropertyProvider _propertyProvider;
    private EventBus _eventBus;
    private bool allowNextMove;
    private float Speed;
    private Vector3 point;
    private IK ik;
    private Vector3 oldJOGposition = Vector3.zero;


    void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _propertyProvider = GetComponent<RobotPropertyProvider>();
        ik = new IK(_propertyProvider);
        //_propertyProvider.XYZ = new Vector3(1,1,1);
        //_propertyProvider.oldXYZ = _propertyProvider.XYZ;
        _propertyProvider.JOGpoint.Position = new Vector3(1000, 1000, 1000);
        SetJogMove(_propertyProvider.JOGpoint);
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
        _eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
    }
    /// <summary>
    /// Вып. команды линейного движения
    /// </summary>
    public void RobotSetLinMove(LinearPointPropertyProvider point)
    {
        GetPositionInfo(point);
        StartCoroutine(LinMove());
    }
    /// <summary>
    /// Ручное управление
    /// </summary>
    public void SetJogMove(JOGPropertyProvider point)
    {
        if (oldJOGposition != point.Position || _propertyProvider.XYZRot != point.RotationQ)
        {
            GetPositionJOG(point);
            ik.CalculateInverseKinematics();
            if (ik.CheckAngle())
            {
                ik.CheckLimit();
                oldJOGposition = point.Position;
                _propertyProvider.oldXYZ = _propertyProvider.XYZ;
                //_propertyProvider.absoluteOldXYZ = _propertyProvider.absoluteXYZ;
            }
            else
            {
                //_propertyProvider.XYZ = _propertyProvider.oldXYZ;
                ik.thetha = ik.old_thetha.ToArray();
                point.GlobalPosition = _propertyProvider.absoluteOldXYZ;
            }

        }

    }
    public void GetPositionInfo(LinearPointPropertyProvider p)
    {
        point = _propertyProvider.transform.InverseTransformPoint(p.Position);////!!!
        _propertyProvider.XYZ.y = point.x * 1000;
        _propertyProvider.XYZ.z = point.y * 1000;
        _propertyProvider.XYZ.x = point.z * 1000;
        _propertyProvider.XYZRot = p.transform.rotation;
        Speed = p.Speed;

        //_propertyProvider.absoluteXYZ = p.Position;
        //PointType = p.pointType;
    }
    private void GetPositionJOG(JOGPropertyProvider p)
    {
        point = _propertyProvider.transform.InverseTransformPoint(p.GlobalPosition);////!!!
        _propertyProvider.XYZ.y = point.x * 1000;
        _propertyProvider.XYZ.z = point.y * 1000;
        _propertyProvider.XYZ.x = point.z * 1000;
        _propertyProvider.XYZRot = p.transform.rotation;

        //_propertyProvider.absoluteXYZ = p.Position;
    }
   



    IEnumerator LinMove()
    {
        Vector3 start = _propertyProvider.oldXYZ;
        Vector3 currentXYZ = start;
        Vector3 end = _propertyProvider.XYZ;
        Vector3 direction = (end - start).normalized;

        float distance = Vector3.Distance(start, end);
        float traveled = 0f;

        
        if (!ik.checkIsNaN())
        {
            yield break;
        }

        Quaternion XYZBuf = _propertyProvider.XYZRot;
        while (traveled < distance)
        {

            yield return new WaitUntil(()=>allowNextMove);
            float t0 = MathF.Floor((traveled / distance) * 100f) / 100f;

            float s = ik.Curva(t0);
            float g = Mathf.Min(Speed * s, distance - traveled);

            currentXYZ += direction * g;
            _propertyProvider.XYZ = currentXYZ;
            _propertyProvider.oldXYZ = currentXYZ;

            _propertyProvider.XYZRot = Quaternion.identity;
            _propertyProvider.XYZRot = Quaternion.Lerp(_propertyProvider.oldXYZRot, XYZBuf, ik.Curva2(t0));
            //UnityEngine.Debug.LogWarning("XYZ " + _propertyProvider.XYZRot.eulerAngles.y);

            ik.CalculateInverseKinematics();
            //CheckAngle();

            traveled += g;
            ik.CheckAngle();

            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        //XYZRot = XYZBuf;
        _propertyProvider.oldXYZ = _propertyProvider.XYZ;
        _propertyProvider.oldXYZRot = _propertyProvider.XYZRot;
        _propertyProvider.SyncJOGPosition();
        _eventBus.Invoke(new RobotEndMove { RoboID = _propertyProvider.Id });
    }
    public void SetAllowNextMove(bool allow)
    {
        allowNextMove = allow;
    }
    public void StopSim()
    {
        allowNextMove = false;
        StopAllCoroutines();
    }
}

