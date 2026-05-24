using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RobotCollisionController : MonoBehaviour
{
    public GameObject A1;
    public MeshRenderer A1MeshRenderer;
    public GameObject A2;
    public MeshRenderer A2MeshRenderer;
    public GameObject A3;
    public MeshRenderer A3MeshRenderer;
    public GameObject A3_4;
    public MeshRenderer A3_4MeshRenderer;
    public GameObject A4;
    public MeshRenderer A4MeshRenderer;
    public GameObject A5;
    public MeshRenderer A5MeshRenderer;
    public GameObject A6;
    public MeshRenderer A6MeshRenderer;
    public GameObject EndEffector;
    public MeshRenderer EndEffectorRenderer;
    public GameObject LeftFingerTrig;
    public MeshRenderer LeftFingerRenderer;
    public GameObject RightFingerTrig;
    public MeshRenderer RightFingerRenderer;
    private List<GameObject> JointGroup = new();
    private List<GameObject> EndEffectorGroup = new();
    private Dictionary<GameObject, (MeshRenderer MeshRenderer, Color Color)> DictionaryMeshRenderers = new();
    Color defaultColorA;
    Color defultColorEndEff;
    Color defaultColorFinger;
    public List<string> TestList = new();

    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();
    public Dictionary<string, List<string>> stringCollisionObjects = new();
    
    private EventBus _eventBus;
    private NotificationSystemManager _notification;
    private SimulationManager _SimManager;
    void Start()
    {
        _SimManager = ServiceManager.Current.Get<SimulationManager>();
        defaultColorA = A1MeshRenderer.material.color;
        defultColorEndEff = EndEffectorRenderer.material.color;
        defaultColorFinger = RightFingerRenderer.material.color;
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<RobotCollisionEvent>(OnRobotCollision);
        _eventBus.Subscribe<RobotCollisionExitEvent>(OnRobotCollisionExit);
        var GO = new[] { A1, A2, A3, A3_4, A4, A5, A6, EndEffector, RightFingerTrig, LeftFingerTrig };
        var MR = new[] { A1MeshRenderer, A2MeshRenderer, A3MeshRenderer, A3_4MeshRenderer, A4MeshRenderer, A5MeshRenderer, A6MeshRenderer, EndEffectorRenderer,RightFingerRenderer, LeftFingerRenderer };
        for (int i = 0; i < 10; i++)
        {
            collisionObjects.Add(GO[i], new List<GameObject>());
            DictionaryMeshRenderers.Add(GO[i], (MR[i], MR[i].material.color));
            if (i < 7)
            {
                JointGroup.Add(GO[i]);
            }
            else
            {
                EndEffectorGroup.Add(GO[i]);
            }
        }
        _notification = ServiceManager.Current.Get<NotificationSystemManager>();
    }

    private void OnRobotCollision(RobotCollisionEvent s)
    { 
        if (IsCollisionBetween(s, A1, A2) || IsCollisionBetween(s, A2, A3) || IsCollisionBetween(s, A3, A3_4) || IsCollisionBetween(s, A3_4, A4) ||
                    IsCollisionBetween(s, A4, A5) || IsCollisionBetween(s, A5, A6) ||
                    IsCollisionBetween(s, A6, EndEffector) || IsCollisionBetween(s, EndEffector, RightFingerTrig) ||
                    IsCollisionBetween(s, EndEffector, LeftFingerTrig) || IsCollisionBetween(s, LeftFingerTrig, RightFingerTrig)) return;
        GameObject obj1 = s.Object; //для которго отселживается коллизия
        GameObject obj2 =  s.CollidedObject; // в коллизии
        if (obj1 == A1 || obj1 == A2 || obj1 == A3 || obj1 == A3_4 || obj1 == A4 || obj1 == A5 || obj1 == A6 || obj1 == EndEffector || obj1 == RightFingerTrig || obj1 == LeftFingerTrig)
        {
            if (!collisionObjects[obj1].Contains(obj2))
            {
                collisionObjects[obj1].Add(obj2);
                if(!(EndEffectorGroup.Contains(obj1) && obj2.layer == LayerMask.NameToLayer("Detail")))
                {
                    PauseSim();
                    UpdateColorGameObjectCollide(obj1, true);
                    CreateMessage(obj1, obj2);
                }
                
                UpdateStringCollisionObjects();
            }
            
        }
    }
    bool IsCollisionBetween(RobotCollisionEvent s, GameObject obj1, GameObject obj2)
    {
        return (s.Object == obj1 && s.CollidedObject == obj2) ||
               (s.Object == obj2 && s.CollidedObject == obj1);
    }
    private void OnRobotCollisionExit(RobotCollisionExitEvent s)
    {
        GameObject obj =  s.Object; //для которго отселживается коллизия
        GameObject obj2 =  s.CollidedObject; // в коллизии
        if (obj == A1 || obj == A2 || obj == A3 || obj == A3_4 || obj == A4 || obj == A5 || obj == A6 || obj == EndEffector || obj == RightFingerTrig || obj == LeftFingerTrig)
        {

            if (collisionObjects[obj].Contains(obj2))
            {
                collisionObjects[obj].Remove(obj2);
                if (collisionObjects[obj].Count == 0)
                {
                    UpdateColorGameObjectCollide(obj, false);
                    //_notification.ShowInfo("Коллизия устранена", $"({obj.name}) больше ни с чем не соприкасается");
                } 
                UpdateStringCollisionObjects();
            }
        }
    }
    private void UpdateStringCollisionObjects()
    {
        bool isCollision = false;
        stringCollisionObjects.Clear();
        foreach (var kvp in collisionObjects)
        {
            string jointName = kvp.Key.name;
            List<string> collidedObjectNames = new List<string>();
            foreach (var objInCol in kvp.Value)
            {
                collidedObjectNames.Add(objInCol.name);
                isCollision = true;
            }
            if (kvp.Key == A3_4) jointName = A3.name;
            stringCollisionObjects[jointName] = collidedObjectNames;
        }
        UpdateTestList();
        _eventBus.Invoke(new CollisionDictionaryUpdated { isCollision = isCollision, stringCollisionObjects = stringCollisionObjects });
    }

    private void UpdateTestList()
    {
        TestList.Clear();
        foreach (var kvp in stringCollisionObjects)
        {
            foreach (var objInCol in kvp.Value)
            {
                TestList.Add(kvp.Key + " collides with " + objInCol);
            }
        }
    }
    private void UpdateColorGameObjectCollide(GameObject gameObject, bool InCollide)
    {
        DictionaryMeshRenderers[gameObject].MeshRenderer.material.color = InCollide ? Color.red : DictionaryMeshRenderers[gameObject].Color;
    }
    private void PauseSim()
    {
        if(_SimManager.GetStatusSim() == SIM_STAT.PLAY && _SimManager.GetSimulationParam().PauseSimInCol)
            _eventBus.Invoke(new SystemPauseSim { info = "Обнаружена коллизия" });

    }
    /// <summary>
    /// создание уведомления
    /// </summary>
    /// <param name="object1">Отслеживаемый объект</param>
    /// <param name="object2">Находящийся в коллизии</param>
    private void CreateMessage(GameObject object1, GameObject object2) 
    {
        if (JointGroup.Contains(object1) && !_SimManager.GetSimulationParam().AlarmJointColStatus) return;
        if (EndEffectorGroup.Contains(object1) && !_SimManager.GetSimulationParam().AlarmEndEffectorColStatus) return;
        _notification.ShowWarning("Обнаружена коллизия", $"({object1.name}) соприкасается с объектом({object2.name})");

    }
}

public class  CollisionDictionaryUpdated
{
    public bool isCollision;
        
    public Dictionary<string, List<string>> stringCollisionObjects;
}
public class SystemPauseSim
{
    public string info;
}
