using Assets.Scripts.CustomEventBus.Signals.Robot;
using UnityEngine;

public class SensorDevice : CellDeviceBase
{
    SensorPropertyProvider _propertyProvider;

    public GameObject DetectObject;
    MeshRenderer DetectObjectmeshRenderer;
    Collider DetectObjectCollider;
    public GameObject MeshSensor;
    public GameObject LedSensor;

    private void Start()
    {
        base.Start();
        _propertyProvider = GetComponent<SensorPropertyProvider>();
        DetectObjectCollider = DetectObject.GetComponent<Collider>();
        DetectObjectmeshRenderer = DetectObject.GetComponent<MeshRenderer>();
        DetectObject.GetComponent<Collider>().isTrigger = true;
    }

    void UpdateVisualization()
    {
        DetectObjectmeshRenderer.enabled = _propertyProvider._showVisualization;
        if (!_propertyProvider._showVisualization) return;

        float length = _propertyProvider.DetectionLength;
        float height = _propertyProvider.DetectionHeight;

        DetectObject.transform.localPosition = new Vector3(0, 0, length / 2f);
        DetectObject.transform.localScale = new Vector3(0.001f, height-0.001f, length);
    }

    private void UpdateMesh()
    {
        float height = _propertyProvider.DetectionHeight;

        // Меняем размер тела датчика
        MeshSensor.transform.localScale = new Vector3(MeshSensor.transform.localScale.x, height, MeshSensor.transform.localScale.z);

        // Обновляем позицию лампочки
        UpdateLedPosition();
    }

    private void UpdateLedPosition()
    {
        float height = _propertyProvider.DetectionHeight;
        LedSensor.transform.localPosition = new Vector3(0,height/2,0);
    }

    void FixedUpdate()
    {
        UpdateMesh();
        UpdateVisualization();

        if (SIM_STATUS != SIM_STAT.PLAY) return;

        Detect();
        UpdateSignal();
    }

    void Detect()
    {
        Collider[] colliders = Physics.OverlapBox(
            DetectObjectCollider.bounds.center,
            DetectObjectCollider.bounds.extents,
            DetectObjectCollider.transform.rotation
        );
        bool detected = false;
        foreach (var collider in colliders)
        {
            GameObject go = collider.gameObject;
            if (go == MeshSensor ||
                go == LedSensor ||
                go == this.gameObject || go.layer == LayerMask.NameToLayer("Manipulator"))
                continue;
            if (collider.isTrigger) continue; //Игнорируем триггеры
            detected = true;
        }
        _propertyProvider.IsActive = detected;

    }

    void UpdateSignal()
    {
        bool signalValue = _propertyProvider.InvertSignal
            ? !_propertyProvider.IsActive
            : _propertyProvider.IsActive;

        _signalBus.SetSignal(_propertyProvider.NameSignal, signalValue);
    }
    protected override void StopSim(StopProgramm s)
    {
        
    }
    protected override void StartSim(StartProgramm s)
    {
        _propertyProvider.IsActive = false;
    }

}