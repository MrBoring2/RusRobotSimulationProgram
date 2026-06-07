using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using UnityEngine;
using UnityEngine.Rendering;

public class Move : MonoBehaviour
{

    private MoveState moveState = MoveState.Open;
    private MoveState oldState = MoveState.Open;
    public float openPosition = 0f;      // открыто
    public float closePosition = 0.1f;   // закрыто (в метрах!)
    private float neutralPos = 0;
    public float stiffness = 140f;       // Жёсткость
    public float damping = 22f;          // Демпфирование (против дрожи)
    public float maxForce = 3000f;       // Максимальная сила толчка

    private Rigidbody rb;

    EventBus _eventBus => ServiceManager.Current.Get<EventBus>();

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(moveState == MoveState.Open)
        {
            MoveFinger(rb, Vector3.zero, openPosition);
        }
        else if(moveState == MoveState.Close)
        {
            MoveFinger(rb, Vector3.zero, closePosition);
        }
        else if (moveState == MoveState.Neutral && oldState != MoveState.Neutral)
        {
            oldState = MoveState.Neutral;
            neutralPos = rb.transform.localPosition.x;
        }
        else if (moveState == MoveState.Neutral)
        {
            MoveFinger(rb, Vector3.zero, neutralPos);
        }
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

    public void Close()
    {
        oldState = moveState;
        moveState = MoveState.Close;
    }
    public void Open()
    {
        oldState = moveState;
        moveState = MoveState.Open;
    }
    public void Neutral()
    {
        oldState = moveState;
        moveState = MoveState.Neutral;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Detail") )
        {
            _eventBus.Invoke(new CNCCollisionOnEvent(gameObject, collision.gameObject));
        }

    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Detail"))
        {
            _eventBus.Invoke(new CNCCollisionExitEvent(gameObject, collision.gameObject));
        }
    }

}
enum MoveState
{
    Open,
    Close,
    Neutral
}
