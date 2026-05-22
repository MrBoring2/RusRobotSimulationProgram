using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.SimulationSystem.RobotSimulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
public class RobotPropertyProvider : BasePropertyProvider
{
    public RobotController RobotController { get; set; }

    //=================== ПАРАМЕТРЫ ===================
    public bool EndEffectorOn { get; set; }
    public float SpeedEffector = 0.5f;
    //ogranicheniya anglesSpeed
    public Angles AnglesSpeedLimit { get; set; } = new(90, 60, 60, 120, 96, 210);
    public Angles AngleAcceler { get; set; } = new(155, 145, 185, 310, 270, 465);
    public Angles AngleBrake { get; set; } = new(155, 145, 185, 310, 270, 465);
    /// <summary>
    /// Углы которые можно менять из интерфейса
    /// </summary>
    public float[] ChangeAngles = new float[6] { 0, 0, 0, 0, 0, 0 };
    //=================== ПАРАМЕТРЫ ===================

    /// <summary>
    /// Углы обновляемы кажды кадр
    /// </summary>
    public float J1Angle { get; set; } = 0;
    public float J2Angle { get; set; } = -90;
    public float J3Angle { get; set; } = 90;
    public float J4Angle { get; set; } = 0;
    public float J5Angle { get; set; } = 0;
    public float J6Angle { get; set; } = 0;
    public float[] GetAnglesAnim()
    {
        return new float[] { J1Angle, J2Angle, J3Angle, J4Angle, J5Angle, J6Angle };
    }

    /// <summary>
    /// Параметры звеньев робота
    /// </summary>
    public RP RP = new(450, -350, 0, 447, 1150, 1350, 500);

    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };


    //JOG
    public JOGPropertyProvider JOGpoint;
    //объект находящийся всегда в захвате для расчте прямой кинематики
    public ForwarKinObj _forwarKinObj;
    /// <summary>
    /// получить текущую позицию захвата, которая обновляется при каждом кадре, и которая используется для расчета прямой кинематики
    /// </summary>
    /// <returns></returns>
    public Point GetActualPosEffector()
    {
        return _forwarKinObj.Pos;
    }
    private void Start()
    {
        base.Start();
        RobotController = GetComponent<RobotController>();
        displayScale = false;
    }

    //------------------------------------------------------------------------------------------------------------------------//

    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        return new List<CustomProperty>()
        {
            new CustomProperty("AnglesSpeedLimit",
                "Максимальная скорость осей",
                typeof(string),
                () => string.Join(",", AnglesSpeedLimit.GetFloats()),
                val =>
                {
                    if(AnglesSpeedLimit == null) UnityEngine.Debug.LogWarning("РАВЕН НАЛЛ");
                    if(AnglesSpeedLimit.UpdateFromString((string)val) != 0) Notification.ShowError(" Убедитесь, что вы ввели 6 чисел, разделенных запятыми, и что все числа положительные (J1, J2, J3, J4, J5, J6).");
                }),
            new CustomProperty("AngleAcceler",
                "Линейное усорение осей",
                typeof(string),
                () => string.Join(",", AngleAcceler.GetFloats()),
                 val =>
                {
                    if(AngleAcceler.UpdateFromString((string)val) != 0) Notification.ShowError(" Убедитесь, что вы ввели 6 чисел, разделенных запятыми, и что все числа положительные (J1, J2, J3, J4, J5, J6).");
                }),
            new CustomProperty("AngleBrake",
                "Линейное торможение осей",
                typeof(string),
                () => string.Join(",", AngleBrake.GetFloats()),
                val =>
                {
                    if(AngleBrake.UpdateFromString((string)val) != 0) Notification.ShowError(" Убедитесь, что вы ввели 6 чисел, разделенных запятыми, и что все числа положительные (J1, J2, J3, J4, J5, J6).");
                }),
           
        };
    }

    public override void RestoreCustomState(ProviderSaveData data)
    {

    }


}