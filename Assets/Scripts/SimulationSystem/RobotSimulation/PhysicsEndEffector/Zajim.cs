using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;

public class Gripper : MonoBehaviour
{
    [Header("References")]
    public RobotPropertyProvider _propertyProvider;
    public GameObject _leftFinger;
    public GameObject _leftFingerTrig;
    public GameObject _rightFinger;
    public GameObject _rightFingerTrig;

    [Header("Settings")]
    public float openPosition = 0f;
    public float closePosition = 0.45f;
    public float stiffness = 120f;
    public float damping = 20f;
    public float maxForce = 2500f;

    // Новые параметры для принудительного разжатия
    public float releaseSpeed = 8f;           // Скорость принудительного открытия
    public float releaseDuration = 0.35f;     // Сколько времени принудительно разжимать

    private Rigidbody leftRb;
    private Rigidbody rightRb;
    private Vector3 leftStartPos;
    private Vector3 rightStartPos;

    private bool detailInGrip = false;
    private GameObject Detail;

    private EventBus _eventBus;
    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();

    private bool isReleasing = false;
    private float releaseTimer = 0f;

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();

        leftRb = _leftFinger.GetComponent<Rigidbody>();
        rightRb = _rightFinger.GetComponent<Rigidbody>();

        leftStartPos = _leftFinger.transform.localPosition;
        rightStartPos = _rightFinger.transform.localPosition;

        collisionObjects.Add(_leftFingerTrig, new List<GameObject>());
        collisionObjects.Add(_rightFingerTrig, new List<GameObject>());

        _eventBus.Subscribe<RobotCollisionEvent>(EnterCollision);
        _eventBus.Subscribe<RobotCollisionExitEvent>(ExitCollision);
    }

    void EnterCollision(RobotCollisionEvent s)
    {
        if (collisionObjects.ContainsKey(s.Object))
        {
            if (!collisionObjects[s.Object].Contains(s.CollidedObject))
                collisionObjects[s.Object].Add(s.CollidedObject);
        }
    }

    void ExitCollision(RobotCollisionExitEvent s)
    {
        if (collisionObjects.ContainsKey(s.Object))
            collisionObjects[s.Object].Remove(s.CollidedObject);
    }

    void FixedUpdate()
    {
        bool gripCommand = _propertyProvider.EndEffectorOn;

        if (isReleasing)
        {
            UpdateReleasing();
            HandleGripLogic(gripCommand);
            return;
        }

        if (gripCommand)
        {
            // Закрываем
            if (!detailInGrip)
            {
                MoveFinger(leftRb, leftStartPos, closePosition);
                MoveFinger(rightRb, rightStartPos, closePosition);
            }
            // Если держим — ничего не делаем (не давим)
        }
        else
        {
            // Разжимаем — начинаем принудительное разжатие
            if (!isReleasing)
            {
                StartRelease();
            }
        }

        HandleGripLogic(gripCommand);
    }

    private void MoveFinger(Rigidbody rb, Vector3 startLocalPos, float targetX)
    {
        Vector3 targetLocal = startLocalPos + new Vector3(targetX, 0, 0);
        Vector3 targetWorld = rb.transform.parent.TransformPoint(targetLocal);

        Vector3 error = targetWorld - rb.worldCenterOfMass;
        Vector3 targetVel = error * stiffness;
        Vector3 velError = targetVel - rb.linearVelocity;
        Vector3 force = velError * damping;

        if (force.magnitude > maxForce)
            force = force.normalized * maxForce;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    // === ПРИНУДИТЕЛЬНОЕ РАЗЖАТИЕ ===
    private void StartRelease()
    {
        isReleasing = true;
        releaseTimer = releaseDuration;

        // Делаем кинематическими на время разжатия
        leftRb.isKinematic = true;
        rightRb.isKinematic = true;
    }

    private void UpdateReleasing()
    {
        releaseTimer -= Time.fixedDeltaTime;

        // Плавно двигаем к открытой позиции
        MoveFingerToPosition(leftRb, leftStartPos, openPosition, releaseSpeed);
        MoveFingerToPosition(rightRb, rightStartPos, openPosition, releaseSpeed);

        if (releaseTimer <= 0f)
        {
            EndRelease();
        }
    }

    private void MoveFingerToPosition(Rigidbody rb, Vector3 startLocalPos, float targetX, float speed)
    {
        Vector3 targetLocal = startLocalPos + new Vector3(targetX, 0, 0);
        Vector3 targetWorld = rb.transform.parent.TransformPoint(targetLocal);

        rb.transform.position = Vector3.MoveTowards(rb.transform.position, targetWorld, speed * Time.fixedDeltaTime);
    }

    private void EndRelease()
    {
        isReleasing = false;

        // Возвращаем в динамический режим
        leftRb.isKinematic = false;
        rightRb.isKinematic = false;

        // Обнуляем скорость
        leftRb.linearVelocity = Vector3.zero;
        rightRb.linearVelocity = Vector3.zero;
    }

    private void HandleGripLogic(bool gripCommand)
    {
        if (gripCommand && !detailInGrip)
        {
            TryGrabObject();
        }
        else if (!gripCommand && detailInGrip)
        {
            ReleaseObject();
        }
    }

    private void TryGrabObject()
    {
        foreach (var obj in collisionObjects[_leftFingerTrig])
        {
            if (obj.layer == LayerMask.NameToLayer("Detail") &&
                collisionObjects[_rightFingerTrig].Contains(obj))
            {
                Detail = obj;
                var detailRb = Detail.GetComponent<Rigidbody>();

                if (detailRb != null)
                {
                    detailRb.isKinematic = true;
                    Detail.transform.SetParent(transform);
                }

                detailInGrip = true;
                break;
            }
        }
    }

    private void ReleaseObject()
    {
        if (Detail == null)
        {
            detailInGrip = false;
            return;
        }

        if (Detail.transform.parent == transform)
        {
            Detail.transform.SetParent(null);
            var detailRb = Detail.GetComponent<Rigidbody>();
            if (detailRb != null)
            {
                detailRb.isKinematic = false;
                detailRb.AddForce(Vector3.down * 0.8f, ForceMode.Impulse);
            }
        }

        detailInGrip = false;
        Detail = null;
    }
}