using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class SimpleGripper : MonoBehaviour
{
    public RobotPropertyProvider _propertyProvider;

    public GameObject _leftFinger;
    private MeshRenderer leftFingerRender;
    public GameObject _rightFinger;
    private MeshRenderer rightFingerRender;
    private Rigidbody leftFingerRigid;
    private Rigidbody rightFingerRigid;
    private GameObject Detail;
    bool detailInGrip = false;

    private EventBus _eventBus;
    private NotificationSystemManager _notification;

    public List<string> TestList = new();

    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();
    private Dictionary<string, List<string>> stringCollisionObjects = new();

    public float openPosition = 0f;      // раскрыто
    public float closePosition = 50f;  // сжато
    public float speed = 1;          // сила

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        leftFingerRigid = _leftFinger.GetComponent<Rigidbody>();
        rightFingerRigid = _rightFinger.GetComponent<Rigidbody>();
        leftFingerRender = leftFingerRigid.GetComponent<MeshRenderer>();
        rightFingerRender = rightFingerRigid.GetComponent<MeshRenderer>();
        _eventBus.Subscribe<RobotCollisionEvent>(EnterCollision);
        _eventBus.Subscribe<RobotCollisionExitEvent>(ExitCollision);
        collisionObjects.Add(_leftFinger, new List<GameObject>());
        collisionObjects.Add(_rightFinger, new List<GameObject>());
        _notification = ServiceManager.Current.Get<NotificationSystemManager>();
    }
    void EnterCollision(RobotCollisionEvent s)
    {
        if(s.Object == _leftFinger || s.Object == _rightFinger)
        {
            if (!collisionObjects[s.Object].Contains(s.CollidedObject))
            {
                collisionObjects[s.Object].Add(s.CollidedObject);
                //_notification.ShowWarning("Обнаружена коллизия", $"({s.Object.name}) соприкасается с объектом({s.CollidedObject.name})");
                UpdateTestList();
            }
        }
        
    }
    void ExitCollision(RobotCollisionExitEvent s)
    {
        if(s.Object == _leftFinger || s.Object == _rightFinger)
        {
            if (collisionObjects[s.Object].Count > 0)
            {
                if (collisionObjects[s.Object].Contains(s.CollidedObject))
                {   
                    collisionObjects[s.Object].Remove(s.CollidedObject);
                   // _notification.ShowInfo("Коллизия устранена", $"({s.Object.name}) больше ни с чем не соприкасается");
                    UpdateTestList();

                }
            }
        }
        
    }
    void FixedUpdate()
    {
        bool grip = _propertyProvider.EndEffectorOn;

        Vector3 leftDir = leftFingerRigid.transform.parent.TransformDirection(Vector3.right);
        Vector3 rightDir = rightFingerRigid.transform.parent.TransformDirection(Vector3.right);

        float leftPos = leftFingerRigid.transform.localPosition.x;
        float rightPos = rightFingerRigid.transform.localPosition.x;
        
        if (grip)
        {
            if (!detailInGrip)
            {
                foreach (var obj in collisionObjects[_leftFinger])
                {
                    if (obj.layer == LayerMask.NameToLayer("Detail"))
                    {
                        if (collisionObjects[_rightFinger].Contains(obj))
                        {
                            detailInGrip = true;
                            Detail = obj;
                        }
                    }
                }
                // СЖИМАЕМ - только если не доехали до границы
                if (leftPos < closePosition && !detailInGrip)
                    leftFingerRigid.transform.localPosition += new Vector3(speed, 0, 0);
                if (detailInGrip)
                {
                    Detail.GetComponent<Rigidbody>().isKinematic = true;
                    Detail.transform.parent = gameObject.transform;
                }
                else
                    StopFinger(leftFingerRigid);

                if (rightPos < closePosition && !detailInGrip)
                    rightFingerRigid.transform.localPosition += new Vector3(speed, 0, 0);
                else
                    StopFinger(rightFingerRigid);
            }
            
        }
        else
        {
            if (detailInGrip)
            {
                if(Detail != null && Detail.activeInHierarchy == true)
                {
                    Detail.GetComponent<Rigidbody>().isKinematic = false;
                    Detail.transform.parent = null;
                    detailInGrip = false;
                }
                else
                {
                    detailInGrip = false;
                }
                
            }
            // РАЗЖИМАЕМ - только если не доехали до границы
            if (leftPos > openPosition)
                leftFingerRigid.transform.localPosition += new Vector3(-speed, 0, 0);
            else
                StopFinger(leftFingerRigid);

            if (rightPos > openPosition)
                rightFingerRigid.transform.localPosition += new Vector3(-speed, 0, 0);
            else
                StopFinger(rightFingerRigid);
        }
    }
    public void UpdateTestList()
    {
        TestList.Clear();
        foreach (var kvp in collisionObjects)
        {
            foreach (var objInCol in kvp.Value)
            {
                TestList.Add(kvp.Key.name + " collides with " + objInCol.name);
            }
        }
    }
    void StopFinger(Rigidbody finger)
    {

    }
}