using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using UnityEngine;
public class TrackingRobotCollision : MonoBehaviour
{
    private EventBus _eventBus;

    private List<GameObject> collisionObjects = new();
    private void Awake()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
    }
    void Start()
    {
         
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("SceneObject") || other.gameObject.CompareTag("Составные части робота"))
        {
            _eventBus.Invoke(new RobotCollisionEvent(gameObject, other.gameObject));
            collisionObjects.Add(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        _eventBus.Invoke(new RobotCollisionExitEvent(gameObject, other.gameObject));
        collisionObjects.Remove(other.gameObject);
    }
    private void FixedUpdate()
    {
        if (collisionObjects.Count == 0) return;
        foreach (var obj in collisionObjects)
        {
            if(obj == null ||  obj.activeInHierarchy == false)
            {
                DeleteCollision(obj);
            }
        }
    }
    private void DeleteCollision(GameObject obj)
    {
        _eventBus.Invoke(new RobotCollisionExitEvent(gameObject, obj.gameObject));
        collisionObjects.Remove(obj);
    }
}

public class RobotCollisionEvent
{
    public GameObject Object { get; }
    public GameObject CollidedObject { get; }
    public RobotCollisionEvent(GameObject joint, GameObject collidedObject)
    {
        Object = joint;
        CollidedObject = collidedObject;
    }
}
public class RobotCollisionExitEvent
{
    public GameObject Object { get; }
    public GameObject CollidedObject { get; }
    public RobotCollisionExitEvent(GameObject joint, GameObject collidedObject)
    {
        Object = joint;
        CollidedObject = collidedObject;
    }
}
//public class AntiPenetration : MonoBehaviour
//{
//    public float maxPenetration = 0.02f; // метров

//    void OnCollisionStay(Collision collision)
//    {
//        foreach (ContactPoint contact in collision.contacts)
//        {
//            if (contact.separation < -maxPenetration) // сильное проникновение
//            {
//                Time.timeScale = 0.1f;           // замедляем вместо полного лага
//                Debug.Log("Сильное проникновение! Замедляем симуляцию.");
//                break;
//            }
//        }
//    }
//}