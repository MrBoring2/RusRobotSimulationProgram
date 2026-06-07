using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Camera;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainCameraMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float baseMovementSpeed = 10f;
    [SerializeField] private float minMovementSpeed = 0.1f;
    [SerializeField] private float maxMovementSpeed = 100f;
    [SerializeField] private float speedChangeMultiplier = 1.5f;

    [Header("Мышь")]
    [SerializeField] private float mouseSensitivity = 3f;

    [Header("Перспектива")]
    [SerializeField] private float buttonRotationSpeedDegPerSec = 360f;

    [Header("Орбита")]
    [SerializeField] private float orbitSensitivity = 5f;

    [Header("Зум")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minDistance = 0.1f;
    [SerializeField] private float maxDistance = 1000f;

    [Header("Ускорение на Shift")]
    [SerializeField] private float shiftSpeedMultiplier = 5f;
    [SerializeField] private float accelerationTime = 0.15f;   
    [SerializeField] private float decelerationTime = 0.3f;

    [Header("Камера")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float orthographicSize = 10f;

    private float currentMovementSpeed;

    private float currentSpeedMultiplier = 1f;
    private float speedVelocity = 0f;


    private Vector3 orbitPoint;
    private bool isOrbiting = false;

    private UIStatusManager _uiStatusManager;
    private EventBus _eventBus;

    private float pitch; // X
    private float yaw;   // Y

    private Vector3 targetEulerAngles;
    private bool isButtonRotating = false;
    private bool orthographicMode = false;

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<RotateCameraSingal>(OnRotateCamera);
        _eventBus.Subscribe<ToggleOrthographicSignal>(OnToggleOtrhograthic);
        _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        Vector3 e = transform.localEulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);
        targetEulerAngles = new Vector3(pitch, yaw, 0f);
        currentMovementSpeed = baseMovementSpeed;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnToggleOtrhograthic(ToggleOrthographicSignal signal)
    {
        ToggleOrthographic();
    }

    private void OnRotateCamera(RotateCameraSingal singal)
    {
        RotateToView(singal.Rotation);
    }

    private void Update()
    {
        if (!_uiStatusManager.CheckIsOnUI())
        {
            HandleMouseRotation();
            HandleOrbitRotation();
            HandleZoom();
            HandleSpeedChange();
            HandleLMBSpeedChange();
        }

        if (!_uiStatusManager.isInputMode)
            HandleMovement();

        HandleShiftAcceleration();
        HandleButtonRotation();
    }

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) input += Vector3.back;
        if (Input.GetKey(KeyCode.A)) input += Vector3.left;
        if (Input.GetKey(KeyCode.D)) input += Vector3.right;
        if (Input.GetKey(KeyCode.E)) input += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) input += Vector3.down;

        if (input != Vector3.zero)
        {
            input.Normalize();

            Vector3 move = transform.forward * input.z +
                          transform.right * input.x +
                          Vector3.up * input.y;

            float finalSpeed = currentMovementSpeed * currentSpeedMultiplier;
            transform.position += move * finalSpeed * Time.deltaTime;
        }
    }
    private void HandleShiftAcceleration()
    {
        float targetMultiplier = 1f;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            targetMultiplier = shiftSpeedMultiplier;
        }

        float smoothTime = targetMultiplier > currentSpeedMultiplier ? accelerationTime : decelerationTime;
        currentSpeedMultiplier = Mathf.SmoothDamp(
            currentSpeedMultiplier,
            targetMultiplier,
            ref speedVelocity,
            smoothTime
        );
    }
    private void HandleMouseRotation()
    {
        //if (EventSystem.current.IsPointerOverGameObject()) return;
        if (!Input.GetMouseButton(1))
            return;

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            return;

        //float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * 60f;
        //float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * 60f;
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        //pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion q = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = q;

        isButtonRotating = false;

        targetEulerAngles = new Vector3(pitch, yaw, 0f);

    }
    private void HandleOrbitRotation()
    {
        if (!Input.GetMouseButton(0))
        {
            isOrbiting = false;
            return;
        }

        if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
            return;

        if (!isOrbiting)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                orbitPoint = hit.point;
            }
            else
            {
                orbitPoint = ray.GetPoint(10f);
            }
            isOrbiting = true;
        }

        float mx = Input.GetAxis("Mouse X") * orbitSensitivity;
        float my = Input.GetAxis("Mouse Y") * orbitSensitivity;

        transform.RotateAround(orbitPoint, Vector3.up, mx);
        transform.RotateAround(orbitPoint, transform.right, -my);

        Vector3 e = transform.eulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);
        targetEulerAngles = new Vector3(pitch, yaw, 0f);
        isButtonRotating = false;
    }

    private void HandleZoom()
    {
        if (Input.GetMouseButton(0))
            return;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        if (orthographicMode)
        {
            mainCamera.orthographicSize -= scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Max(0.1f, mainCamera.orthographicSize);
        }
        else
        {
            Vector3 zoomDirection = transform.forward * scroll * zoomSpeed;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                zoomDirection *= 3f;
            }

            transform.position += zoomDirection;
        }
    }
    private void HandleSpeedChange()
    {
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        if (scroll > 0)
            currentMovementSpeed *= speedChangeMultiplier;
        else
            currentMovementSpeed /= speedChangeMultiplier;

        currentMovementSpeed = Mathf.Clamp(currentMovementSpeed, minMovementSpeed, maxMovementSpeed);
    }
    private void HandleLMBSpeedChange()
    {
        // Проверяем, зажата ли левая кнопка мыши и нет модификаторов (Alt, Ctrl)
        if (!Input.GetMouseButton(0))
            return;

        // Не реагируем, если зажат Alt (режим орбиты) или Ctrl (стандартное изменение скорости)
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            return;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        // Изменяем скорость движения камеры
        if (scroll > 0)
            currentMovementSpeed *= speedChangeMultiplier;
        else
            currentMovementSpeed /= speedChangeMultiplier;

        currentMovementSpeed = Mathf.Clamp(currentMovementSpeed, minMovementSpeed, maxMovementSpeed);

    }

    private void HandleButtonRotation()
    {
        if (!isButtonRotating) return;

        Quaternion targetRot = Quaternion.Euler(NormalizeAngle(targetEulerAngles.x),
                                                NormalizeAngle(targetEulerAngles.y),
                                                0f);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, buttonRotationSpeedDegPerSec * Time.deltaTime);

        float angle = Quaternion.Angle(transform.rotation, targetRot);
        if (angle < 0.5f)
        {
            transform.rotation = targetRot;
            isButtonRotating = false;

            Vector3 e = transform.localEulerAngles;
            pitch = NormalizeAngle(e.x);
            yaw = NormalizeAngle(e.y);
            targetEulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    public void RotateToView(Vector3 eulerAngles)
    {
        if (eulerAngles.x <= -90f) eulerAngles.x = -89.999f;
        if (eulerAngles.x >= 90f) eulerAngles.x = 89.999f;

        targetEulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, 0f);
        isButtonRotating = true;
    }

    public void ToggleOrthographic()
    {
        orthographicMode = !orthographicMode;
        if (mainCamera != null)
        {
            mainCamera.orthographic = orthographicMode;
            mainCamera.orthographicSize = orthographicSize;
        }
    }

    private static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a <= -180f) a += 360f;
        return a;
    }
    public void FocusOnObject(Vector3 targetPosition, float distance = 5f)
    {
        Vector3 direction = transform.forward;
        transform.position = targetPosition - direction * distance;
    }
}
