using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;
public class CollisionEffector : MonoBehaviour
{
    private EventBus _eventBus;
    private List<GameObject> collisionObjects = new();
    void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        
    }
    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        if (collision.gameObject.tag == "SceneObject")
        {
            _eventBus.Invoke(new EndEffecorCollisionEvent(gameObject, collision.gameObject));
            collisionObjects.Add(collision.gameObject);
        }
    }
    private void OnCollisionExit(UnityEngine.Collision collision)
    {
        _eventBus.Invoke(new EndEffecorCollisionExitEvent(gameObject, collision.gameObject));
        collisionObjects.Remove(collision.gameObject);
    }
    private void FixedUpdate()
    {
        if (collisionObjects.Count == 0) return;
        foreach (var obj in collisionObjects)
        {
            if (obj == null || obj.activeInHierarchy == false)
            {
                DeleteCollision(obj);
            }
        }
    }
    private void DeleteCollision(GameObject obj)
    {
        _eventBus.Invoke(new EndEffecorCollisionExitEvent(gameObject, obj.gameObject));
        collisionObjects.Remove(obj);

    }
}

public class EndEffecorCollisionEvent
{
    public GameObject Finger { get; }
    public GameObject CollidedObject { get; }
    public EndEffecorCollisionEvent(GameObject joint, GameObject collidedObject)
    {
        Finger = joint;
        CollidedObject = collidedObject;
    }
}
public class EndEffecorCollisionExitEvent
{
    public GameObject Finger { get; }
    public GameObject CollidedObject { get; }
    public EndEffecorCollisionExitEvent(GameObject joint, GameObject collidedObject)
    {
        Finger = joint;
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