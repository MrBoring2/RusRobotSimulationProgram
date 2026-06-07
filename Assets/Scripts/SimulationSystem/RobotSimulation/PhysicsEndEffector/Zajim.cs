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
    public float closePosition = 0.5f;
    public float stiffness = 80f;        // УМЕНЬШИЛИ
    public float damping = 15f;          // УМЕНЬШИЛИ
    public float maxForce = 1500f;       // Сильно уменьшили
    public float releasePushForce = 300f; // Сила, чтобы "оттолкнуть" пальцы при разжатии

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

        float target = gripCommand ? closePosition : openPosition;

        MoveFinger(leftRb, leftStartPos, target);
        MoveFinger(rightRb, rightStartPos, target);

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

        // При разжатии добавляем небольшую отталкивающую силу
        if (!_propertyProvider.EndEffectorOn)
            force += (rb.transform.position - transform.position).normalized * releasePushForce * 0.3f;

        rb.AddForce(force, ForceMode.Acceleration);
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
                // Небольшой импульс вниз, чтобы деталь "отлепилась"
                detailRb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
            }
        }

        detailInGrip = false;
        Detail = null;
    }
}