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
    public float J2Angle = -90;
    public float J3Angle = 90;
    public float J4Angle = 0;
    public float J5Angle = 0;
    public float J6Angle = 0;
    public bool EndEffectorOn { get; set; }
    public float SpeedEffector = 0.5f;
    //parameters zveniev
    public RP RP = new RP(450, -350, 0, 447, 1150, 1350, 500);

    //IK
    //public Vector3 XYZ = Vector3.zero;
    //public Vector3 oldXYZ = Vector3.zero;
    //public Quaternion XYZRot;
    //public Quaternion oldXYZRot = Quaternion.identity;
    //public Vector3 absoluteXYZ => GetAbsolutePosition(XYZ);
    //public Vector3 absoluteOldXYZ => GetAbsolutePosition(oldXYZ);

    public float[] thetha = { 0, 0, 0, 0, 0, 0 };
    public float[] old_thetha = { 0, 90, 90, 0, -90, 0 };
    public float[] step_thetha = { 0, 0, 0, 0, 0, 0 };


    //JOG
    public JOGPropertyProvider JOGpoint;

    public float[] ChangeAngles = new float[6] {0, 0, 0, 0, 0, 0 };
    //ogranicheniya anglesSpeed
    public float[] ogrAngleSpeed = { 140, 93, 108, 205, 295, 465 };

    //объект находящийся всегда в захвате для расчте прямой кинематики
    public ForwarKinObj _forwarKinObj;
    public Point GetActualPosEffector()
    {
        return _forwarKinObj.Pos;
    }


    /// <summary>
    /// reset position end Effector
    /// </summary>
    private void Start()
    {
        RobotController = GetComponent<RobotController>();
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();
        displayScale = false;
    }
    

    private Vector3 GetAbsolutePosition(Vector3 pos)
    {
        Vector3 point = new Vector3();
        return transform.TransformPoint(point);
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

    
}