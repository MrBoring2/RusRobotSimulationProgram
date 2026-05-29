using UnityEngine;

public class OvenLogic : CellDeviceBase
{
    public TrigOven Trig1;
    public TrigOven Trig2;
    public TrigOven Trig3;
    public TrigOven Trig4;
    private float t1;
    private float t2;
    private float t3;
    private float t4;
    OvenPropertyProvider _propertyProvider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        base.Start();
        _propertyProvider = gameObject.GetComponent<OvenPropertyProvider>();
    }



    void FixedUpdate()
    {

        if (SIM_STATUS != SIM_STAT.PLAY) return;

        Sim();

    }
    void Sim()
    {
        if (Trig1.Detect() && t1 < _propertyProvider.TimeHeating)
        {
            t1 += Time.fixedDeltaTime;
            UpdateSignal(_propertyProvider.NameSignal1, false);
        }
        else if(Trig1.Detect() && t1 >= _propertyProvider.TimeHeating)
        {
            UpdateSignal(_propertyProvider.NameSignal1, true);
        }
        else
        {
            t1 = 0;
            UpdateSignal(_propertyProvider.NameSignal1, false);
        }
        if (Trig2.Detect() && t2 < _propertyProvider.TimeHeating)
        {
            t2 += Time.fixedDeltaTime;
            UpdateSignal(_propertyProvider.NameSignal2, false);
        }
        else if (Trig2.Detect() && t2 >= _propertyProvider.TimeHeating)
        {
            UpdateSignal(_propertyProvider.NameSignal2, true);
        }
        else
        {
            t2 = 0;
            UpdateSignal(_propertyProvider.NameSignal2, false);
        }
        if (Trig3.Detect() && t3 < _propertyProvider.TimeHeating)
        {
            t3 += Time.fixedDeltaTime;
            UpdateSignal(_propertyProvider.NameSignal3, false);
        }
        else if (Trig3.Detect() && t3 >= _propertyProvider.TimeHeating)
        {
            UpdateSignal(_propertyProvider.NameSignal3, true);
        }
        else
        {
            t3 = 0;
            UpdateSignal(_propertyProvider.NameSignal3, false);
        }
        if (Trig4.Detect() && t4 < _propertyProvider.TimeHeating)
        {
            t4 += Time.fixedDeltaTime;
            UpdateSignal(_propertyProvider.NameSignal4, false);
        }
        else if (Trig4.Detect() && t4 >= _propertyProvider.TimeHeating)
        {
            UpdateSignal(_propertyProvider.NameSignal4, true);
        }
        else
        {
            t4 = 0;
            UpdateSignal(_propertyProvider.NameSignal4, false);
        }
    }
    protected override  void StartSim(StartProgramm s)
    {
        t1 = t2 = t3 = t4 = 0;
    }
    void UpdateSignal(string NameSignal, bool b)
    {

        _signalBus.SetSignal(NameSignal, b);
    }
    protected override void StopSim(StopProgramm s)
    {
        t1 = t2 = t3 = t4 = 0;
    }
}
