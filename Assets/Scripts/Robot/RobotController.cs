using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class RobotController : MonoBehaviour
{
    private bool allowNextCommand;
    private RobotPropertyProvider _propertyProvider;
    private float Speed;
    private TypePoint PointType;
    private Vector3 point;
    private IK ik;
    private Vector3 oldJOGposition = Vector3.zero;


    void Start()
    {
        _propertyProvider = GetComponent<RobotPropertyProvider>();
        ik = new IK(_propertyProvider);
    }
    public void RobotSetStateEndEffector(StateEndEffectorPropertyProvider cmd)
    {
        _propertyProvider.EndEffectorOn = cmd.Get();
        EndMoveToPoint();
    }
    public void SetJogMove(LinearPointPropertyProvider point)
    {
        if(oldJOGposition != point.Position)
        {
            GetPositionInfo(point);
            ik.CalculateInverseKinematics();
            ik.CheckAngle();
            oldJOGposition = point.Position;
            _propertyProvider.oldXYZ = _propertyProvider.XYZ;
        }
        
    }
    public void RobotSetLinMove(LinearPointPropertyProvider point)
    {
        GetPositionInfo(point);
        if(PointType == TypePoint.LIN)
        {
           StartCoroutine(LinMove());
            
        }
    }
    private void GetPositionInfo(LinearPointPropertyProvider p)
    {
        point = _propertyProvider.transform.InverseTransformPoint(p.Position);////!!!
        _propertyProvider.XYZ.y = point.x * 1000;
        _propertyProvider.XYZ.z = point.y * 1000;
        _propertyProvider.XYZ.x = point.z * 1000;
        _propertyProvider.XYZRot = p.transform.rotation;
        Speed = p.Speed;
        PointType = p.pointType;
    }
    private void GetAbsolutePosition(Vector3 pos)
    {
        Vector3 point = new Vector3();
        
        point.x = pos.y / 1000;
        point.z = pos.x / 1000;
        point.y = pos.z / 1000;
        
        _propertyProvider.absoluteXYZ = _propertyProvider.transform.TransformPoint(point); ;
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

            yield return new WaitUntil(()=>GetStatusSim());
            float t0 = MathF.Floor((traveled / distance) * 100f) / 100f;

            float s = ik.Curva(t0);
            float g = Mathf.Min(Speed * s, distance - traveled);

            currentXYZ += direction * g;
            _propertyProvider.XYZ = currentXYZ;
            _propertyProvider.oldXYZ = currentXYZ;
            GetAbsolutePosition(currentXYZ);
            _propertyProvider.XYZRot = Quaternion.identity;
            _propertyProvider.XYZRot = Quaternion.Lerp(_propertyProvider.oldXYZRot, XYZBuf, ik.Curva2(t0));
            //UnityEngine.Debug.LogWarning("XYZ " + _propertyProvider.XYZRot.eulerAngles.y);

            ik.CalculateInverseKinematics(); ;
            //CheckAngle();

            traveled += g;
            ik.CheckAngle();
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        //XYZRot = XYZBuf;
        _propertyProvider.oldXYZ = _propertyProvider.XYZ;
        _propertyProvider.oldXYZRot = _propertyProvider.XYZRot;
        EndMoveToPoint();
    }
    private bool GetStatusSim()
    {
        return allowNextCommand;
    }
    public void StatusSim(SIM sim)
    {
        switch (sim)
        {
            case SIM.START: StartSim(); break;
            case SIM.STOP: StopSim(); break;
            case SIM.PAUSE: PauseSim(); break;
            case SIM.RESUME: ResumeSim(); break;
            default: break;
        }
    }
    private void PauseSim()
    {
        allowNextCommand = false;
    }
    private void StopSim()
    {
        allowNextCommand = false;
        //_propertyProvider.ResetPositionEffector();
        StopAllCoroutines();
    }
    private void StartSim()
    {
        allowNextCommand = true;
    }
    private void ResumeSim()
    {
        allowNextCommand = true;
    }
    private void StepSim()
    {
        allowNextCommand = true;

    }
    private void EndMoveToPoint()
    {
        _propertyProvider.EndMove();
    }
}

