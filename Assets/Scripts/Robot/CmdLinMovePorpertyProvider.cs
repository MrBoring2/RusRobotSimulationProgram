//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UIElements;
//using static UnityEngine.GraphicsBuffer;

//public class CmdLinMovePorpertyProvider : MonoBehaviour, IPropertyProvider
//{
//    public string Name { get => gameObject.name; set => gameObject.name = Name; }
//    public Vector3 Position { get => transform.position; set => transform.position = Position; }
//    public Vector3 Rotation { get => transform.eulerAngles; set => transform.eulerAngles = Rotation; }
//    public Quaternion RotationQ { get => transform.rotation; set => transform.rotation = RotationQ; }
//    public Vector3 Scale { get => transform.localScale; set => transform.localScale = Scale; }

//    //к точке
//    public float Speed = 0;
//    public TypePoint pointType = TypePoint.PTP;

//    //магнит
//    public MagnitS magnitStatus = MagnitS.NotControl;

//    //в точке
//    public float delay = 0;

        
   
//}
//public enum MagnitS
//{
//    On,
//    Off,
//    NotControl
//}
//public enum TypePoint
//{
//    PTP,
//    LIN
//}


