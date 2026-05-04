using UnityEngine;

public class SensorLed : MonoBehaviour
{
    [Header("LED Settings")]
    [SerializeField] private Material _ledMaterial;           
    [SerializeField] private Color _offColor = Color.gray;    
    [SerializeField] private float _onIntensity = 5f;        
    [SerializeField] private float _offIntensity = 0f;     

    private SensorPropertyProvider _propertyProvider;
    private Renderer _renderer;
    private Material _instanceMaterial;                     
    private bool _currentState = false;

    void Start()
    {
        _propertyProvider = GetComponentInParent<SensorPropertyProvider>();

        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
            _renderer = GetComponentInChildren<Renderer>();

        // Создаём экземпляр материала, чтобы не менять оригинал
        _instanceMaterial = new Material(_ledMaterial);
        _renderer.material = _instanceMaterial;

        UpdateLedState(false);
    }

    void FixedUpdate()
    {
        bool newState = _propertyProvider.IsActive; 

        if (newState != _currentState)
        {
            _currentState = newState;
            UpdateLedState(_currentState);
        }
    }

    void UpdateLedState(bool isOn)
    {
        Color baseColor = isOn ? _propertyProvider.OnColor : _offColor;
        float intensity = isOn ? _onIntensity : _offIntensity;

        Color emissionColor = baseColor * intensity;

        _instanceMaterial.SetColor("_BaseColor", baseColor);
        _instanceMaterial.SetColor("_EmissionColor", emissionColor);
    }
    private void OnDestroy()
    {
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }
}