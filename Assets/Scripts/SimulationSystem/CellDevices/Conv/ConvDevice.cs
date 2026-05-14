using Assets.Scripts.CustomEventBus.Signals.Robot;
using System.Collections.Generic;
using UnityEngine;

public class ConvDevice : CellDeviceBase
{
    ConvPropertyProvider _propertyProvider;

    public GameObject Trigger;
    [SerializeField]
    public List<Rigidbody> items = new List<Rigidbody>();



    private void Start()
    {
        base.Start();
        _propertyProvider = GetComponent<ConvPropertyProvider>();
    }



    void FixedUpdate()
    {

        if (SIM_STATUS != SIM_STAT.PLAY) return;

        Sim();

    }

    void Sim()
    {
        bool signal = _propertyProvider.isAct || _signalBus.GetSignal(_propertyProvider.NameSignal);

        if (signal)
        {
            // Преобразуем направление из локальных в мировые координаты
            Vector3 worldDirection = transform.TransformDirection(Vector3.forward);
            Vector3 velocity = worldDirection.normalized * _propertyProvider.Speed;

            foreach (Rigidbody rb in items)
            {
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(velocity.x, 0, velocity.z);
                    _propertyProvider.Vector = worldDirection;
                }
            }
        }
        else
        {
            foreach (Rigidbody rb in items)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
        }
            items.RemoveAll(rb => rb == null);
    }

    protected override void StopSim(StopProgramm s)
    {

    }
    protected override void StartSim(StartProgramm s)
    {
    }

}