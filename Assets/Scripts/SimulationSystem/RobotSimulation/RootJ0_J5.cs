//using UnityEngine;

//public class RootJ0_J5 : MonoBehaviour
//{
//    public ArticulationBody[] joints; // J1..J6
//    public RobotPropertyProvider _propertyProvider;
//    float[] Stiffnes = {10000000,10000000,10000000,10000000,10000000,10000000};
//    float[] ForceLimit = { 1000000, 1000000, 1000000, 10000000, 10000000, 10000000};
//    float[] Damping = { 3.402823e+38f, 3.402823e+38f, 3.402823e+38f, 3.402823e+38f, 3.402823e+38f, 3.402823e+38f };
//    float[] AnglesCorrect = { 0, +90, -90, 0, 0, 0 };
//    public bool On = true;
//    void FixedUpdate()
//    {
//        if (!On) return;
//        for(int i = 0; i<6; i++)
//        {
//            var drive = joints[i].xDrive; // или yDrive/zDrive в зависимости от оси
//            drive.driveType = ArticulationDriveType.Target; // Устанавливаем тип управления
//            drive.forceLimit = ForceLimit[i];
//            drive.target = _propertyProvider.GetAnglesAnim()[i] + AnglesCorrect[i]; // в градусах!
//            joints[i].xDrive = drive;
//            joints[i].angularDamping = Damping[i];
//            joints[i].linearDamping = Damping[i];
//        }

//    }


