using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;

public class Gripper : MonoBehaviour
{
    public RobotPropertyProvider _propertyProvider;

    public GameObject _leftFinger;
    public GameObject _leftFingerTrig;
    public GameObject _rightFinger;
    public GameObject _rightFingerTrig;

    private Rigidbody leftRb;
    private Rigidbody rightRb;

    private Vector3 leftStartPos;
    private Vector3 rightStartPos;

    public float openPosition = 0f;      // открыто
    public float closePosition = 0.5f;   // закрыто (в метрах!)

    public float stiffness = 140f;       // Жёсткость
    public float damping = 22f;          // Демпфирование (против дрожи)
    public float maxForce = 3000f;       // Максимальная сила толчка

    private bool detailInGrip = false;
    private GameObject Detail;

    private EventBus _eventBus;
    private NotificationSystemManager _notification;

    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();
    private Dictionary<GameObject, Rigidbody> FingerDictionary = new();

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _notification = ServiceManager.Current.Get<NotificationSystemManager>();

        leftRb = _leftFinger.GetComponent<Rigidbody>();
        rightRb = _rightFinger.GetComponent<Rigidbody>();

        leftStartPos = _leftFinger.transform.localPosition;
        rightStartPos = _rightFinger.transform.localPosition;

        collisionObjects.Add(_leftFingerTrig, new List<GameObject>());
        collisionObjects.Add(_rightFingerTrig, new List<GameObject>());

        _eventBus.Subscribe<RobotCollisionEvent>(EnterCollision);
        _eventBus.Subscribe<RobotCollisionExitEvent>(ExitCollision);
        FingerDictionary.Add(_leftFinger, leftRb);
        FingerDictionary.Add(_rightFinger, rightRb);
    }


    void EnterCollision(RobotCollisionEvent s)
    {
        if (s.Object == _leftFingerTrig || s.Object == _rightFingerTrig)
        {
            if (!collisionObjects[s.Object].Contains(s.CollidedObject))
            {
                collisionObjects[s.Object].Add(s.CollidedObject);
            }
        }
    }

    void ExitCollision(RobotCollisionExitEvent s)
    {
        if (s.Object == _leftFingerTrig || s.Object == _rightFingerTrig)
        {
            collisionObjects[s.Object].Remove(s.CollidedObject);
        }
    }

    void FixedUpdate()
    {
        bool grip = _propertyProvider.EndEffectorOn;   // true = закрывать

        float targetLeft = grip ? closePosition : openPosition;
        float targetRight = grip ? closePosition : openPosition;

        MoveFinger(leftRb, leftStartPos, targetLeft);
        MoveFinger(rightRb, rightStartPos, targetRight);

        // Логика захвата детали
        HandleGripLogic(grip);


    }

    private void MoveFinger(Rigidbody rb, Vector3 startLocalPos, float targetX)
    {
        Vector3 targetWorld = rb.transform.parent.TransformPoint(startLocalPos + new Vector3(targetX, 0, 0));
        Vector3 error = targetWorld - rb.worldCenterOfMass;

        Vector3 targetVel = error * stiffness;
        Vector3 velError = targetVel - rb.linearVelocity;

        Vector3 force = velError * damping;

        if (force.magnitude > maxForce)
            force = force.normalized * maxForce;

        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void HandleGripLogic(bool grip)
    {
        if (grip && !detailInGrip)
        {
            // Проверяем, зажали ли деталь с двух сторон
            foreach (var obj in collisionObjects[_leftFingerTrig])
            {
                if (obj.layer == LayerMask.NameToLayer("Detail") &&
                    collisionObjects[_rightFingerTrig].Contains(obj))
                {
                    detailInGrip = true;
                    Detail = obj;
                    Detail.GetComponent<Rigidbody>().isKinematic = true;
                    Detail.transform.SetParent(transform);
                    break;
                }
            }
        }
        else if (!grip && detailInGrip)
        {
            if (Detail != null)
            {
                
                if (Detail.transform.parent == transform)
                {
                    Detail.GetComponent<Rigidbody>().isKinematic = false;
                    Detail.transform.SetParent(null);
                }
            }
            detailInGrip = false;
            Detail = null;
        }
    }
}