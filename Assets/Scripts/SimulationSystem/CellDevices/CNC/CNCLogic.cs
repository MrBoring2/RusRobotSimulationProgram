using Assets.Scripts.CustomEventBus;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;


public class CNCLogic : CellDeviceBase
{
    public List<GameObject> Fingers;
    public Dictionary<GameObject, Move> FingerMoves = new();
    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();
    private CNCPropertyProvider _PP;
    private GameObject Detail;
    private float timer;
    private bool stateDoor = false;
    void Start()
    {
        base.Start();
        _PP = GetComponent<CNCPropertyProvider>();
        foreach (var finger in Fingers)
        {
            collisionObjects.Add(finger, new List<GameObject>());
            FingerMoves.Add(finger, finger.GetComponent<Move>());
        }
        _eventBus.Subscribe<CNCCollisionOnEvent>(EnterCollision);
         _eventBus.Subscribe<CNCCollisionExitEvent>(ExitCollision);
    }
    private void FixedUpdate()
    {
        if (SIM_STATUS == SIM_STAT.STOP)
        {
            MoveFinger(_PP.chuckOnContr);
            HandleGripLogic(_PP.chuckOnContr);
            return;
        }

        Sim();

    }
    void Sim()
    {
        _PP.ChuckOn = _signalBus.GetSignal(_PP.NameSignalChuckOn);
        _PP.CNCStart = _signalBus.GetSignal(_PP.NameSignalCNCStart);
        MoveFinger(_PP.ChuckOn);
        HandleGripLogic(_PP.ChuckOn);
        WorkLogic();
        UpdateSignal(_PP.NameSignalCNCEndWork, _PP.CNCEndWork);
        UpdateSignal(_PP.NameSignalDetailInChuck, _PP.DetailInChuck);
    }

    void EnterCollision(CNCCollisionOnEvent s)
    {
        if (Fingers.Contains(s.Finger))
        {
            if (!collisionObjects[s.Finger].Contains(s.CollidedObject))
            {
                collisionObjects[s.Finger].Add(s.CollidedObject);
            }
        }
    }

    void ExitCollision(CNCCollisionExitEvent s)
    {
        if (Fingers.Contains(s.Finger))
        {
            if (collisionObjects[s.Finger].Contains(s.CollidedObject))
            {
                collisionObjects[s.Finger].Remove(s.CollidedObject);
            }
        }
    }

    private void MoveFinger(bool grip)
    {
        if(grip && !_PP.DetailInChuck)
        {
            foreach (var finger in Fingers)
            {
                FingerMoves[finger].Close();
            }
        }
        else if (!grip)
        {
            foreach (var finger in Fingers)
            {
                FingerMoves[finger].Open();
            }
        }
    }
    private void HandleGripLogic(bool grip)
    {
        if (grip && !_PP.DetailInChuck)
        {
            // ѕровер€ем, зажали ли деталь с двух сторон
            foreach (var obj in collisionObjects[Fingers[0]])
            {
                if (obj.layer == LayerMask.NameToLayer("Detail") &&
                    collisionObjects[Fingers[1]].Contains(obj) && collisionObjects[Fingers[2]].Contains(obj))
                {
                    _PP.DetailInChuck = true;
                    Detail = obj;
                    Detail.GetComponent<Rigidbody>().isKinematic = true;
                    Detail.transform.SetParent(transform);
                    break;
                }
            }
        }
        else if (!grip && _PP.DetailInChuck)
        {
            if (Detail != null)
            {

                if (Detail.transform.parent == transform) 
                { 
                    Detail.GetComponent<Rigidbody>().isKinematic = false;
                    Detail.transform.SetParent(null);

                } 
            }
            _PP.DetailInChuck = false;
            Detail = null;
        }
    }
     void WorkLogic()
    {
        if (_PP.CNCStart)
        {
            if (timer < _PP.WorkTime)
            {
                if(stateDoor == false)
                {
                    stateDoor = true;
                    _ = CloseDoor();
                }
                timer += Time.fixedDeltaTime;
                _PP.CNCEndWork = false;
            }
            else
            {
                if(stateDoor == true)
                {
                    stateDoor = false;
                    _ = OpenDoor();
                }
            }
        }
    }

    async Awaitable OpenDoor()
    {
        await Awaitable.WaitForSecondsAsync(3f);
        _PP.CNCEndWork = true;
    }
    async Awaitable CloseDoor()
    {
        await Awaitable.WaitForSecondsAsync(3f);
    }
    protected override void StartSim(StartProgramm s)
    {
        _PP.chuckOnContr = false;
        timer = 0;
    }
    protected override void StopSim(StopProgramm s)
    {

    }
    void UpdateSignal(string NameSignal, bool b)
    {

        _signalBus.SetSignal(NameSignal, b);
    }
}

public class CNCCollisionOnEvent
{
    public GameObject Finger { get; }
    public GameObject CollidedObject { get; }
    public CNCCollisionOnEvent(GameObject finger, GameObject collidedObject)
    {
        Finger = finger;
        CollidedObject = collidedObject;
    }
}
public class CNCCollisionExitEvent
{
    public GameObject Finger { get; }
    public GameObject CollidedObject { get; }
    public CNCCollisionExitEvent(GameObject finger, GameObject collidedObject)
    {
        Finger = finger;
        CollidedObject = collidedObject;
    }
}