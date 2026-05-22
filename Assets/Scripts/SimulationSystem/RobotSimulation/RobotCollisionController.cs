using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RobotCollisionController : MonoBehaviour
{
    public GameObject A1;
    private MeshRenderer A1MeshRenderer;
    public GameObject A2;
    private MeshRenderer A2MeshRenderer;
    public GameObject A3;
    private MeshRenderer A3MeshRenderer;
    public GameObject A3_4;
    private MeshRenderer A3_4MeshRenderer;
    public GameObject A4;
    private MeshRenderer A4MeshRenderer;
    public GameObject A5;
    private MeshRenderer A5MeshRenderer;
    public GameObject A6;
    private MeshRenderer A6MeshRenderer;
    public GameObject EndEffector;
    private MeshRenderer EndEffectorRenderer;
    public GameObject LeftFinger;
    private MeshRenderer LeftFingerRenderer;
    public GameObject RightFinger;
    private MeshRenderer RightFingerRenderer;
    private List<GameObject> JointGroup = new();
    private List<GameObject> EndEffectorGroup = new();
    private Dictionary<GameObject, (MeshRenderer MeshRenderer, Color Color)> DictionaryMeshRenderers = new();
    Color defaultColorA;
    Color defultColorEndEff;
    Color defaultColorFinger;
    public List<string> TestList = new();

    private Dictionary<GameObject, List<GameObject>> collisionObjects = new();
    private Dictionary<string, List<string>> stringCollisionObjects = new();
    
    private EventBus _eventBus;
    private NotificationSystemManager _notification;
    private SimulationManager _SimManager;
    void Start()
    {
        _SimManager = ServiceManager.Current.Get<SimulationManager>();
        A1MeshRenderer = A1.GetComponent<MeshRenderer>();
        A2MeshRenderer = A2.GetComponent<MeshRenderer>();
        A3MeshRenderer = A3.GetComponent<MeshRenderer>();
        A3_4MeshRenderer = A3_4.GetComponent<MeshRenderer>();
        A4MeshRenderer = A4.GetComponent<MeshRenderer>();
        A5MeshRenderer = A5.GetComponent<MeshRenderer>();
        A6MeshRenderer = A6.GetComponent<MeshRenderer>();
        EndEffectorRenderer = EndEffector.GetComponent<MeshRenderer>();
        LeftFingerRenderer = LeftFinger.GetComponent<MeshRenderer>();
        RightFingerRenderer = RightFinger.GetComponent<MeshRenderer>();
        defaultColorA = A1MeshRenderer.material.color;
        defultColorEndEff = EndEffectorRenderer.material.color;
        defaultColorFinger = RightFingerRenderer.material.color;
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<RobotCollisionEvent>(OnRobotCollision);
        _eventBus.Subscribe<RobotCollisionExitEvent>(OnRobotCollisionExit);
        var GO = new[] { A1, A2, A3, A4, A5, A6, EndEffector, RightFinger, LeftFinger };
        var MR = new[] { A1MeshRenderer, A2MeshRenderer, A3MeshRenderer, A4MeshRenderer, A5MeshRenderer, A6MeshRenderer, EndEffectorRenderer, LeftFingerRenderer, RightFingerRenderer };
        for (int i = 0; i < 9; i++)
        {
            collisionObjects.Add(GO[i], new List<GameObject>());
            DictionaryMeshRenderers.Add(GO[i], (MR[i], MR[i].material.color));
            if (i < 6)
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
                    IsCollisionBetween(s, A6, EndEffector) || IsCollisionBetween(s, EndEffector, RightFinger) ||
                    IsCollisionBetween(s, EndEffector, LeftFinger) || IsCollisionBetween(s, LeftFinger, RightFinger)) return;
        GameObject obj1 = s.Object == A3_4 ? A3 : s.Object; //для которго отселживается коллизия
        GameObject obj2 = s.CollidedObject == A3_4 ? A3 : s.CollidedObject; // в коллизии
        if (obj1 == A1 || obj1 == A2 || obj1 == A3 || obj1 == A4 || obj1 == A5 || obj1 == A6 || obj1 == EndEffector || obj1 == RightFinger || obj1 == LeftFinger)
        {
            if (!collisionObjects[obj1].Contains(obj2))
            {
                collisionObjects[obj1].Add(obj2);
                PauseSim();
                UpdateColorGameObjectCollide(obj1, true);
                CreateMessage(obj1, obj2);
                UpdateStringCollisionObjects();
            }
            
        }
    }
    bool IsCollisionBetween(RobotCollisionEvent s, GameObject obj1, GameObject obj2)
    {
        return (s.Object == obj1 && s.CollidedObject == obj2) ||
               (s.Object == obj2 && s.CollidedObject == obj1);
    }
    private void OnRobotCollisionExit(RobotCollisionExitEvent e)
    {
        GameObject obj = e.Object;
        if (obj == A1 || obj == A2 || obj == A3 || obj == A3_4 || obj == A4 || obj == A5 || obj == A6 || obj == EndEffector || obj == RightFinger || obj == LeftFinger)
        {
            if (obj == A3_4) obj = A3;

            if (collisionObjects[obj].Contains(e.CollidedObject))
            {
                collisionObjects[obj].Remove(e.CollidedObject);
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
            stringCollisionObjects[jointName] = collidedObjectNames;
        }
        UpdateTestList();
        _eventBus.Invoke(new CollisionDictionaryUpdated { isCollision = isCollision, stringCollisionObjects = stringCollisionObjects });
    }

    private void UpdateTestList()
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
    private void UpdateColorGameObjectCollide(GameObject gameObject, bool InCollide)
    {
        DictionaryMeshRenderers[gameObject].MeshRenderer.material.color = InCollide ? Color.red : DictionaryMeshRenderers[gameObject].Color;
        if(gameObject == A3)
        {
            A3_4MeshRenderer.material.color = InCollide ? Color.red : DictionaryMeshRenderers[A3].Color;
        }
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
