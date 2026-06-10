using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.SimulationSystem.RobotSimulation;
using Assets.Scripts.StageControlSystem.UI;
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
    public bool VisibleZone { get; set; } = false;
    //Ограничение скорости осей
    public Angles AnglesSpeedLimit { get; set; } = new(90, 60, 60, 120, 96, 210);
    //Ограничение ускорения осей
    public Angles AngleAcceler { get; set; } = new(155, 145, 185, 310, 270, 465);
    //Ограничение торможения осей
    public Angles AngleBrake { get; set; } = new(155, 145, 185, 310, 270, 465);
    /// <summary>
    /// Углы которые можно менять из интерфейса
    /// </summary>
    public float[] ChangeAngles = new float[6] { 0, 0, 0, 0, 0, 0 };
    //=================== ПАРАМЕТРЫ ===================

    //окно
    private RobotCollisionInfoPanelEvents _collisionWindow;

    /// <summary>
    /// Углы обновляемы кажды кадр
    /// </summary>
    public float J1Angle { get; set; } = 0;
    public float J2Angle { get; set; } = -90;
    public float J3Angle { get; set; } = 90;
    public float J4Angle { get; set; } = 0;
    public float J5Angle { get; set; } = 0;
    public float J6Angle { get; set; } = 0;

    /// <summary>
    /// Углы для работы с UI
    /// </summary>
    public float J1AngleUI { get => ChangeAngles[0]; set => ChangeAngles[0] = value; }
    public float J2AngleUI { get => ChangeAngles[1]+90; set => ChangeAngles[1] = value-90; }
    public float J3AngleUI { get => ChangeAngles[2]-90; set => ChangeAngles[2] = value+90; }
    public float J4AngleUI { get => ChangeAngles[3]; set => ChangeAngles[3] = value; }
    public float J5AngleUI { get => ChangeAngles[4]; set => ChangeAngles[4] = value; }
    public float J6AngleUI { get => ChangeAngles[5]; set => ChangeAngles[5] = value; }
    /// <summary>
    /// Параметры звеньев робота
    /// </summary>
    public RP RP = new(450, -350, 0, 447, 1150, 1350, 715);
    public float[] AnglesLimit = new[] {-175f,175f,-140f,-20f, -60f, 170f, -180f, 180f, -105f, 105f, -180f, 180f }; 
    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] AnglesLimitUI = new[] { -175f, 175f, -50f, 60f, -150f, 80f, -180, 180, -105f, 105f, -180, 180 };

    //JOG
    public JOGPropertyProvider JOGpoint;
    //Коллизии
    public RobotCollisionController RCC;
    public Dictionary<string, List<string>> stringCollisionObjects = new();
    //объект находящийся всегда в захвате для расчте прямой кинематики
    public ForwarKinObj _forwarKinObj;
    /// <summary>
    /// получить текущую позицию захвата, которая обновляется при каждом кадре, и которая используется для расчета прямой кинематики МИРОВАЯ
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
        RCC = gameObject.GetComponent<RobotCollisionController>();
        stringCollisionObjects = RCC.stringCollisionObjects;
        displayScale = false;
    }

    //------------------------------------------------------------------------------------------------------------------------//
    void Update()
    {
        if (_collisionWindow != null && _collisionWindow.IsVisible)
        {
            _collisionWindow.UpdateCollisionData(RCC.stringCollisionObjects);
        }
    }
    public override ProviderSaveData CaptureCustomState()
    {
        return new ProviderSaveData
        {
            ProviderType = nameof(RobotPropertyProvider),
            StringValues =
            {
                ["AnglesSpeedLimit"] = string.Join(",", AnglesSpeedLimit.GetFloats()),
                ["AngleAcceler"] = string.Join(",", AngleAcceler.GetFloats()),
                ["AngleBrake"] = string.Join(",", AngleBrake.GetFloats())
            }
        };
    }

    public override List<CustomProperty> GetCustomProperties()
    {
        return new List<CustomProperty>()
        {
            new CustomProperty("VisibleZone",
                "Зона досягаемости",
                typeof(bool),
                () => VisibleZone,
                val => VisibleZone = (bool)val),
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
                new ButtonProperty("ShowCollisions", "Коллизии", "Показать коллизии", () => ShowCollisionWindow())

        };
    }
    public void ShowCollisionWindow()
    {
        var modalService = ServiceManager.Current.Get<ModalWindowServiceManager>();
        var parameters = new ModalParameters();
        parameters.Set("collisionData", stringCollisionObjects);

        modalService.ShowWindow<object>(
            "robot-collision-info-window",
            "Коллизии робота",
            parameters,
            null);
        _collisionWindow = modalService.GetWindow<RobotCollisionInfoPanelEvents>("robot-collision-info-window");
    }
    public override void RestoreCustomState(ProviderSaveData data)
    {
        if (data.StringValues.TryGetValue("AnglesSpeedLimit", out var v1))
        {
            AnglesSpeedLimit = new();
            AnglesSpeedLimit.UpdateFromString((string)v1);
        }
        if (data.StringValues.TryGetValue("AngleAcceler", out var v2))
        {
            AngleAcceler = new();
            AngleAcceler.UpdateFromString((string)v2);
        }
        if (data.StringValues.TryGetValue("AngleBrake", out var v3))
        {
            AngleBrake = new();
            AngleBrake.UpdateFromString((string)v3);
        }
        if (data.FloatValues.TryGetValue("J1AngleUI", out var v4))
            J1AngleUI = v4;
        if (data.FloatValues.TryGetValue("J2AngleUI", out var v5))
            J2AngleUI = v5;
        if (data.FloatValues.TryGetValue("J3AngleUI", out var v6))
            J3AngleUI = v6;
        if (data.FloatValues.TryGetValue("J4AngleUI", out var v7))
            J4AngleUI = v7;
        if (data.FloatValues.TryGetValue("J5AngleUI", out var v8))
            J5AngleUI = v8;
        if (data.FloatValues.TryGetValue("J6AngleUI", out var v9))
            J6AngleUI = v9;
    }
}