using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.Robot;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.SimulationSystem.RobotSimulation
{

    public class RobotController : MonoBehaviour
    {
        RobotPropertyProvider _propertyProvider;
        SimulationManager _simManager => ServiceManager.Current.Get<SimulationManager>();
        SceneObjectsManager _sceneObjectManager => ServiceManager.Current.Get<SceneObjectsManager>();
        EventBus _eventBus => ServiceManager.Current.Get<EventBus>();

        float Speed;//м/с

        Angles[] angles;

        //оптимизация
        Vector3 oldJOGposition = Vector3.zero;
        Quaternion oldJOGrotation = Quaternion.identity;
        float[] oldAngles = new float[6] { 0, 0, 0, 0, 0, 0 };


        public string ID => _propertyProvider.Id;
        public bool RunTask { get; set; }
        bool CommandComplete = true;
        public List<SubProgramm> Programm { get; set; }

        public AnimationCurve SpeedCurve;
        private InverseK_new InvKin;



        void Start()
        {
            InvKin = gameObject.GetComponent<InverseK_new>();
            _eventBus.Subscribe<StopProgramm>(StopSim);
            _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Subscribe<RobotsControllerResetState>(ControllerResetState);
            _propertyProvider = GetComponent<RobotPropertyProvider>();
            _propertyProvider.JOGpoint.Position = new Vector3(1, 1, 1);
            _propertyProvider.JOGpoint.Rotation = new Vector3(180, 0, 0);
            ///////////
            Vector3[] testData = new Vector3[10000];
            HashSet<Vector3> uniquePoints = new HashSet<Vector3>();

            System.Random random = new System.Random();

            for (int i = 0; i < 10000; i++)
            {
                Vector3 newPoint;
                do
                {
                    float x = 1f + (float)random.NextDouble();
                    float y = 1f + (float)random.NextDouble();
                    float z = 1f + (float)random.NextDouble();
                    newPoint = new Vector3(x, y, z);
                }
                while (uniquePoints.Contains(newPoint));

                uniquePoints.Add(newPoint);
                testData[i] = newPoint;
            }
            //////
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                InvKin.IKCalc(_propertyProvider.RP, testData[i], Quaternion.identity);
            }
            stopwatch.Stop();
            UnityEngine.Debug.Log($"10000 точек IKCalc выполнился за: {(double)stopwatch.ElapsedMilliseconds / Stopwatch.Frequency} с или {stopwatch.ElapsedTicks} тиков ");
            SetJogMove();
        }
        private void FixedUpdate()
        {
            if (_simManager.GetModeSim() == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                SetJogMove();
            }
            else if (_simManager.GetModeSim() == MODE.ANGLES_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                SetAngleMove();
            }
        }
        //=================================== КОМАНДЫ ===================================//
        public async Awaitable RobotSetWait(WaitPropertyProvider cmd)
        {
            await SetWait(cmd.Get());
        }
        private async Awaitable SetWait(float time)
        {
            await Awaitable.WaitForSecondsAsync(time);
        }
        //--Команда изменения состояния эффектора
        public async Awaitable RobotSetStateEndEffector(StateEndEffectorPropertyProvider cmd)
        {
            await Awaitable.FixedUpdateAsync();
            _propertyProvider.EndEffectorOn = cmd.Get();
        }
        //--Команда линейное движение
        //public async Awaitable RobotSetLinMove(LinearPointPropertyProvider point)
        //{
        //    UnityEngine.Debug.LogError("Линейное движение: Старт ");

        //    Point Start = new(_propertyProvider.JOGpoint.Position, _propertyProvider.JOGpoint.LocalRotationQ);
        //    Point End = new(GetPositionInfo(point).Position, GetPositionInfo(point).Rotation);
        //    Point wayPoint = new(Start.Position, Start.Rotation);
        //    Vector3 wayDirection = (End.Position - Start.Position).normalized;
        //    float distance = Vector3.Distance(Start.Position, End.Position);
        //    float traveled = 0f;

        //    ///время
        //    float timeInWay = distance / Speed;

        //    float timeCurrent = 0;
        //    float timeCurrenScale = 0;
        //    //Сделать проверку точки на достижимость, если точка недоступна, то не выполнять движение и выдавать ошибку
        //    if (Start.Position == End.Position)
        //    {
        //        float angle = (Quaternion.Angle(Start.Rotation, End.Rotation));
        //        Vector3 ang1 = Start.Rotation.eulerAngles;
        //        Vector3 ang2 = End.Rotation.eulerAngles;
        //        timeInWay = (Quaternion.Angle(Start.Rotation, End.Rotation)) / point.AngleSpeed;
        //    }
        //    while (End.Position != wayPoint.Position || (Quaternion.Angle(wayPoint.Rotation, End.Rotation) > 0.001))
        //    {
        //        if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
        //        timeCurrenScale = timeCurrent / timeInWay;
        //        if (timeCurrenScale > 1) timeCurrenScale = 1;
        //        float positionInLine = SpeedCurve.Evaluate(timeCurrenScale) * distance;
        //        float step = positionInLine - traveled;
        //        wayPoint.Position += wayDirection * step;

        //        wayPoint.Rotation = Quaternion.SlerpUnclamped(Start.Rotation, End.Rotation, SpeedCurve.Evaluate(timeCurrenScale));
        //        angles = InvKin.IKCalc(_propertyProvider.RP, wayPoint.Position, wayPoint.Rotation);

        //        traveled = positionInLine;

        //        //Выбор конфигурации точки
        //        InvKin.CheckLimit(angles[0]);
        //        if (InvKin.checkIsNaN(angles[0]))
        //        {
        //            ModifyRobot(_propertyProvider, angles[0].GetFloats());
        //            SetJogPosition(wayPoint);

        //            timeCurrent += Time.deltaTime;
        //            await Awaitable.FixedUpdateAsync();
        //            //yield return new WaitForSeconds(Time.fixedDeltaTime);
        //        }

        //    }
        //    //CommandComplete = true;
        //    return;
        //}
        public async Awaitable RobotSetLinMove(LinearPointPropertyProvider point)
        {
            UnityEngine.Debug.LogError("Линейное движение: Старт ");

            Point Start = new(_propertyProvider.JOGpoint.Position, _propertyProvider.JOGpoint.LocalRotationQ);
            Point End = new(GetPositionInfo(point).Position, GetPositionInfo(point).Rotation);

            Point wayPoint = new(Start.Position, Start.Rotation);
            Vector3 wayDirection = (End.Position - Start.Position).normalized;
            float distance = Vector3.Distance(Start.Position, End.Position);
            Quaternion rotateDirect;
            bool isTriangularProfile = false;
            bool onlyRotate = false;
            //временные
            float LinAcceler = point.LinAcceler;
            float LinBrake = point.LinBrake;
            float AngleAcceler = point.AngleAcceler;
            float AngleBreak = point.AngleBrake;
            float AngleSpeed = point.AngleSpeed;
            float LinearSpeed = point.LinearSpeed;
            float vMax = 0;
            //временные
            float currentTimeMove = 0;
            float currentWay = 0;
            float Angle = 0;
            //
            float tAcсeler = 0;
            float tBrake = 0;
            float sAcсeler = 0;
            float sBrake = 0;
            float sLinear = 0;
            float tLinear = 0;

            //равноускоренное вращение
            if (Vector3.Distance(End.Position, Start.Position) < 0.001)
            {
                onlyRotate = true;
                Angle = Quaternion.Angle(Start.Rotation, End.Rotation);
                tAcсeler = AngleSpeed / AngleAcceler;
                tBrake = AngleSpeed / AngleBreak;
                sAcсeler = (AngleAcceler * tAcсeler * tAcсeler) / 2;
                sBrake = (AngleBreak * tBrake * tBrake) / 2;
                //трангулярная скорость
                if ((sAcсeler + sBrake) > Angle)
                {
                    isTriangularProfile = true;
                    vMax = Mathf.Sqrt(Angle / ((1 / (2 * AngleAcceler)) + (1 / (2 * AngleBreak))));
                    tAcсeler = vMax / AngleAcceler;
                    tBrake = vMax / AngleBreak;
                    sAcсeler = (AngleAcceler * tAcсeler * tAcсeler) / 2;
                    sBrake = (AngleBreak * tBrake * tBrake) / 2;
                }
                //трапецивидная скорость
                else
                {
                    sLinear = Angle - (sAcсeler + sBrake);
                    tLinear = sLinear / AngleSpeed;
                }
            }
            else
            {
                tAcсeler = LinearSpeed / LinAcceler;
                tBrake = LinearSpeed / LinBrake;
                sAcсeler = (LinAcceler * tAcсeler * tAcсeler) / 2;
                sBrake = (LinBrake * tBrake * tBrake) / 2;
                //триангулярная скорость
                if ((sAcсeler + sBrake) > distance)
                {
                    isTriangularProfile = true;
                    vMax = Mathf.Sqrt(distance / ((1 / (2 * LinAcceler)) + (1 / (2 * LinBrake))));
                    tAcсeler = vMax / LinAcceler;
                    tBrake = vMax / LinBrake;
                    sAcсeler = (LinAcceler * tAcсeler * tAcсeler) / 2;
                    sBrake = (LinBrake * tBrake * tBrake) / 2;
                }
                //трапецивидная скорость
                else
                {
                    sLinear = distance - (sAcсeler + sBrake);
                    tLinear = sLinear / LinearSpeed;
                }
            }


            //Сделать проверку точки на достижимость, если точка недоступна, то не выполнять движение и выдавать ошибку

            while (/*End.Position != wayPoint.Position*/Vector3.Distance(End.Position, wayPoint.Position) > 0.001 || (Quaternion.Angle(wayPoint.Rotation, End.Rotation) > 0.001))
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                Stopwatch stopwatch = Stopwatch.StartNew();
                //трапеция
                if (!isTriangularProfile && !onlyRotate)
                {
                    if (currentTimeMove < tAcсeler)
                    {
                        currentWay = (LinAcceler * currentTimeMove * currentTimeMove) / 2;

                    }
                    else if ((currentTimeMove >= tAcсeler) && (currentTimeMove <= tAcсeler + tLinear))
                    {
                        currentWay = sAcсeler + LinearSpeed * (currentTimeMove - tAcсeler);
                    }
                    else if (currentTimeMove > tAcсeler + tLinear)
                    {
                        float t_brake = currentTimeMove - (tAcсeler + tLinear);
                        currentWay = sAcсeler + sLinear + (LinearSpeed * t_brake - (LinBrake * t_brake * t_brake) / 2);

                    }
                    wayPoint.Position = Start.Position + (wayDirection * currentWay);
                    wayPoint.Rotation = Quaternion.Lerp(Start.Rotation, End.Rotation, currentWay / distance);
                }
                //треугольник
                else if (isTriangularProfile && !onlyRotate)
                {
                    if (currentTimeMove <= tAcсeler)
                    {
                        currentWay = (LinAcceler * currentTimeMove * currentTimeMove) / 2;

                    }
                    else if (currentTimeMove > tAcсeler)
                    {
                        float t_brake = currentTimeMove - tAcсeler;
                        currentWay = sAcсeler + (vMax * t_brake - (LinBrake * t_brake * t_brake) / 2);

                    }
                    wayPoint.Position = Start.Position + (wayDirection * currentWay);
                    wayPoint.Rotation = Quaternion.Lerp(Start.Rotation, End.Rotation, currentWay / distance);
                }
                //точка
                else
                {
                    if (!isTriangularProfile)
                    {
                        if (currentTimeMove < tAcсeler)
                        {
                            currentWay = (AngleAcceler * currentTimeMove * currentTimeMove) / 2;

                        }
                        else if ((currentTimeMove >= tAcсeler) && (currentTimeMove <= tAcсeler + tLinear))
                        {
                            currentWay = sAcсeler + AngleSpeed * (currentTimeMove - tAcсeler);
                        }
                        else if (currentTimeMove > tAcсeler + tLinear)
                        {
                            float t_brake = currentTimeMove - (tAcсeler + tLinear);
                            currentWay = sAcсeler + sLinear + (AngleSpeed * t_brake - (AngleBreak * t_brake * t_brake) / 2);

                        }
                        wayPoint.Rotation = Quaternion.Lerp(Start.Rotation, End.Rotation, currentWay / Angle);
                    }
                    else
                    {
                        if (currentTimeMove <= tAcсeler)
                        {
                            currentWay = (AngleAcceler * currentTimeMove * currentTimeMove) / 2;

                        }
                        else if (currentTimeMove > tAcсeler)
                        {
                            float t_brake = currentTimeMove - tAcсeler;
                            currentWay = sAcсeler + (vMax * t_brake - (AngleBreak * t_brake * t_brake) / 2);

                        }
                        wayPoint.Rotation = Quaternion.Lerp(Start.Rotation, End.Rotation, currentWay / Angle);
                    }
                }
                
                angles = InvKin.IKCalc(_propertyProvider.RP, wayPoint.Position, wayPoint.Rotation);
                

                //Выбор конфигурации точки
                InvKin.CheckLimit(angles[0]);
                if (InvKin.checkIsNaN(angles[0]))
                {
                    stopwatch.Stop();
                    long freq = Stopwatch.Frequency;
                    UnityEngine.Debug.Log($"Метод IKCalc выполнился за: {(double)stopwatch.ElapsedMilliseconds/freq} с");
                    UnityEngine.Debug.Log($"Или в тиках: {stopwatch.ElapsedTicks}");
                    ModifyRobot(_propertyProvider, angles[0].GetFloats());
                    SetJogPosition(wayPoint);

                    currentTimeMove += Time.deltaTime;
                    await Awaitable.FixedUpdateAsync();
                    //yield return new WaitForSeconds(Time.fixedDeltaTime);
                }
                else
                {
                    await Awaitable.FixedUpdateAsync();
                    UnityEngine.Debug.LogError("Ошибка линейного движения");
                }

            }
            //CommandComplete = true;
            return;
        }
        /// <summary>
        /// расчет параметров равноускоренного движения, трапеция и треугольник
        /// </summary>
        /// <param name="LinearSpeed"></param>
        /// <param name="LinAcceler"></param>
        /// <param name="LinBrake"></param>
        /// <param name="distance"></param>
        public (float tAcсeler, float sAcсeler, float tBrake, float sBrake, float tLinear, float sLinear, float vMax, int DirectRoteate, bool isTriangularProfile)
            Сalc(float LinearSpeed, float LinAcceler, float LinBrake, float distance, int direct)
        {
            float tAcсeler = LinearSpeed / LinAcceler;
            float tBrake = LinearSpeed / LinBrake;
            float sAcсeler = (LinAcceler * tAcсeler * tAcсeler) / 2;
            float sBrake = (LinBrake * tBrake * tBrake) / 2;
            float sLinear = 0;
            float tLinear = 0;
            float vMax = LinearSpeed;
            int DirectRoteate = direct;
            bool isTriangularProfile = false;
            //трангулярная скорость
            if ((sAcсeler + sBrake) > Mathf.Abs(distance))
            {
                isTriangularProfile = true;
                vMax = Mathf.Sqrt(Mathf.Abs(distance) / ((1 / (2 * LinAcceler)) + (1 / (2 * LinBrake))));
                if (Mathf.Abs(distance) <= 1e-6f)
                {
                    vMax = 0f;
                }
                tAcсeler = vMax / LinAcceler;
                tBrake = vMax / LinBrake;
                sAcсeler = (LinAcceler * tAcсeler * tAcсeler) / 2;
                sBrake = (LinBrake * tBrake * tBrake) / 2;
            }
            //трапецивидная скорость
            else
            {
                sLinear = Mathf.Abs(distance) - (sAcсeler + sBrake);
                tLinear = sLinear / LinearSpeed;
            }
            return (tAcсeler, sAcсeler, tBrake, sBrake, tLinear, sLinear, vMax, DirectRoteate, isTriangularProfile);
        }

        public async Awaitable RobotSetPTPMove(LinearPointPropertyProvider point)
        {
            //параметры движения
            Angles AngleAcceler = _propertyProvider.AngleAcceler;
            Angles AngleBrake = _propertyProvider.AngleBrake;
            float percent = point.SpeedPercent;
            Angles AnglesSpeed = new(_propertyProvider.AnglesSpeedLimit.PercentAngles(percent));
            //параметры движения
            UnityEngine.Debug.LogError("PTP движение: Старт ");
            float CurrentTimeMove = 0;
            Point End = new(GetPositionInfo(point).Position, GetPositionInfo(point).Rotation);

            Angles EndAngles = InvKin.IKCalc(_propertyProvider.RP, End.Position, End.Rotation)[0];
            Angles CurrentAngles = new(_propertyProvider.J1Angle, _propertyProvider.J2Angle, _propertyProvider.J3Angle, _propertyProvider.J4Angle, _propertyProvider.J5Angle, _propertyProvider.J6Angle);
            Angles StartAngles = new(_propertyProvider.J1Angle, _propertyProvider.J2Angle, _propertyProvider.J3Angle, _propertyProvider.J4Angle, _propertyProvider.J5Angle, _propertyProvider.J6Angle);
            Angles Distance = new();
            List<(float tAcсeler, float sAcсeler, float tBrake, float sBrake, float tLinear, float sLinear, float vMax, int DirectRoteate, bool isTriangularProfile)> ListParameters = new();
            (float LongTime, int LongThetha) = (float.NegativeInfinity, -1);
            for (int i = 0; i < 6; i++)
            {
                Distance.SetThetha(i, EndAngles.GetThetha(i) - StartAngles.GetThetha(i));
                int Direct = 0;
                if (Distance.GetThetha(i) > 0)
                {
                    Direct = 1;
                }
                else
                {
                    Direct = -1;
                }
                //расчет параметров для каждой оси
                ListParameters.Add(Сalc(AnglesSpeed.GetThetha(i), AngleAcceler.GetThetha(i), AngleBrake.GetThetha(i), Distance.GetThetha(i), Direct));

                var x = ListParameters[i];
                if (LongTime < x.tLinear + x.tBrake + x.tAcсeler)
                {
                    LongTime = x.tLinear + x.tBrake + x.tAcсeler;
                    LongThetha = i;
                }
            }

            while (CurrentAngles.Diff(EndAngles) > 0.001f)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                await Awaitable.FixedUpdateAsync();
                CurrentTimeMove += Time.deltaTime;
                if (!ListParameters[LongThetha].isTriangularProfile)
                {
                    if (CurrentTimeMove < ListParameters[LongThetha].tAcсeler)
                    {
                        CurrentAngles.SetThetha(LongThetha, StartAngles.GetThetha(LongThetha) + ListParameters[LongThetha].DirectRoteate * (AngleAcceler.GetThetha(LongThetha) * CurrentTimeMove * CurrentTimeMove) / 2);
                    }
                    else if ((CurrentTimeMove >= ListParameters[LongThetha].tAcсeler) && (CurrentTimeMove <= ListParameters[LongThetha].tAcсeler + ListParameters[LongThetha].tLinear))
                    {
                        CurrentAngles.SetThetha(LongThetha, StartAngles.GetThetha(LongThetha) + ListParameters[LongThetha].DirectRoteate * (ListParameters[LongThetha].sAcсeler + ((CurrentTimeMove - ListParameters[LongThetha].tAcсeler) * AnglesSpeed.GetThetha(LongThetha))));
                    }
                    else
                    {
                        float t_brake = CurrentTimeMove - (ListParameters[LongThetha].tAcсeler + ListParameters[LongThetha].tLinear);
                        CurrentAngles.SetThetha(LongThetha, StartAngles.GetThetha(LongThetha) + ListParameters[LongThetha].DirectRoteate * (ListParameters[LongThetha].sAcсeler + ListParameters[LongThetha].sLinear + (ListParameters[LongThetha].vMax * t_brake - (AngleBrake.GetThetha(LongThetha) * t_brake * t_brake) / 2)));
                    }
                }
                else
                {
                    if (CurrentTimeMove < ListParameters[LongThetha].tAcсeler)
                    {
                        CurrentAngles.SetThetha(LongThetha, StartAngles.GetThetha(LongThetha) + ListParameters[LongThetha].DirectRoteate * (AngleAcceler.GetThetha(LongThetha) * CurrentTimeMove * CurrentTimeMove) / 2);
                    }
                    else
                    {
                        float t_brake = CurrentTimeMove - (ListParameters[LongThetha].tAcсeler);
                        CurrentAngles.SetThetha(LongThetha, StartAngles.GetThetha(LongThetha) + ListParameters[LongThetha].DirectRoteate * (ListParameters[LongThetha].sAcсeler + (ListParameters[LongThetha].vMax * t_brake - (AngleBrake.GetThetha(LongThetha) * t_brake * t_brake) / 2)));
                    }
                }

                float percentMove = MathF.Abs((CurrentAngles.GetThetha(LongThetha) - StartAngles.GetThetha(LongThetha)) / Mathf.Abs(Distance.GetThetha(LongThetha)));
                for (int i = 0; i < 6; i++)
                {
                    if (i != LongThetha)
                    {
                        CurrentAngles.SetThetha(i, StartAngles.GetThetha(i) + ListParameters[i].DirectRoteate * (Mathf.Abs(Distance.GetThetha(i)) * percentMove));
                    }
                }
                _propertyProvider.J1Angle = CurrentAngles.thetha1;
                _propertyProvider.J2Angle = CurrentAngles.thetha2;
                _propertyProvider.J3Angle = CurrentAngles.thetha3;
                _propertyProvider.J4Angle = CurrentAngles.thetha4;
                _propertyProvider.J5Angle = CurrentAngles.thetha5;
                _propertyProvider.J6Angle = CurrentAngles.thetha6;
                SetJogPosition(GetPositionInfo(_propertyProvider._forwarKinObj.Pos));
            }

        }
        //=================================== КОМАНДЫ ===================================//
        //--Задать позицию ДЖОГа
        public void SetJogMove()
        {
            if (oldJOGposition != _propertyProvider.JOGpoint.Position || oldJOGrotation != _propertyProvider.JOGpoint.LocalRotationQ)
            {
                angles = InvKin.IKCalc(_propertyProvider.RP, _propertyProvider.JOGpoint.Position, _propertyProvider.JOGpoint.LocalRotationQ);
                //добавить автовыбор конфигурации или ручной ввод
                InvKin.CheckLimit(angles[0]);
                if (InvKin.checkIsNaN(angles[0]))
                {
                    ModifyRobot(_propertyProvider, angles[0].GetFloats());
                    angles[0].GetFloats().CopyTo(_propertyProvider.ChangeAngles, 0);
                    //_propertyProvider.XYZ = _propertyProvider.oldXYZ = EffectorPosition.Position;
                    //_propertyProvider.XYZRot = _propertyProvider.oldXYZRot = EffectorPosition.Rotation;
                }
                oldJOGposition = _propertyProvider.JOGpoint.Position;
                oldJOGrotation = _propertyProvider.JOGpoint.LocalRotationQ;

            }

        }
        public void SetAngleMove()
        {
            if (!oldAngles.SequenceEqual(_propertyProvider.ChangeAngles))
            {
                _propertyProvider.ChangeAngles = InvKin.CheckLimit(_propertyProvider.ChangeAngles);
                ModifyRobot(_propertyProvider, _propertyProvider.ChangeAngles);
                Point position = _propertyProvider.GetActualPosEffector();
                _propertyProvider.JOGpoint.GlobalPosition = position.Position;
                _propertyProvider.JOGpoint.GlobalRotationQ = position.Rotation;
                _propertyProvider.ChangeAngles.CopyTo(oldAngles, 0);
            }
        }
        //--Выполнить подпрограмму (задачу)--
        public void RunSubProgramm(string IDTaskToRun)
        {

            try
            {
                //Programm = BuildProgramm(ID);
                //SubProgramm Task = Programm.FirstOrDefault(x => x.ID == IDTaskToRun);
                RunTask = true;
                _ = Run(Programm.FirstOrDefault(x => x.ID == IDTaskToRun));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Подпрограмма с ID '{ID}' для робота {IDTaskToRun} ошибка: {ex}");
            }


        }
        //--Корутина выполнение подпрограммы (задачи)--
        async Awaitable Run(SubProgramm Task)
        {
            foreach (var comand in Task.ProgrammElement)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                while (!(_simManager.GetStatusSim() == SIM_STAT.PLAY))
                {
                    if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                    await Awaitable.FixedUpdateAsync();
                }
                //await Awaitable.FixedUpdateAsync();
                //CommandComplete = false;
                await comand.Execute(this);

            }
            UnityEngine.Debug.LogWarning("Задача завершена");
            ////final
            RunTask = false;

        }
        public void StopSim(StopProgramm s)
        {
            //tokenTask.Cancel();
            //tokenTask.Dispose();
            RunTask = false;
            CommandComplete = true;
        }

        /// <summary>
        /// получение дерева программы
        /// </summary>
        //public List<RobotProgrammElement> Programm
        //{
        //    get
        //    {
        //        var a = BuildTreeInternal(ID);
        //        return a;
        //    }
        //}

        public List<SubProgramm> GetProgramm()
        {
            return BuildProgramm(ID);
        }
        private List<SubProgramm> BuildProgramm(string parentId)
        {
            List<SubProgramm> programm = new();
            List<RobotProgramObject> Tasks = _sceneObjectManager.Commands.GetSubPrograms(parentId);

            foreach (var item in Tasks)
            {
                SubProgramm Task = new(new List<RobotProgrammElement>(), ENUM_COMMANDS.SUBPROGRAMM, item.Id);
                var Commands = _sceneObjectManager.Commands.GetCommandsFromSubProgram(parentId, item.Id);
                foreach (var command in Commands)
                {
                    ConvertToRobotProgrammElement(command, Task.ProgrammElement);
                }
                programm.Add(Task);
            }
            return programm;
        }

        private void ConvertToRobotProgrammElement(CommandObject obj, List<RobotProgrammElement> programm)
        {
            if (obj.Type == ObjectType.LinearMoveCommand)
            {
                var command = new CommandMove(obj.Reference.GetComponent<LinearPointPropertyProvider>(), ENUM_COMMANDS.MOVE_LIN, obj.Id);
                programm.Add(command);
            }
            else if (obj.Type == ObjectType.StateEndEffectorCommand)
            {
                var command = new ComandSetStateEndEffector(obj.Reference.GetComponent<StateEndEffectorPropertyProvider>(), ENUM_COMMANDS.CHANGE_STATE_ENDEFFECTOR, obj.Id);
                programm.Add(command);
            }
            else if (obj.Type == ObjectType.WaitCommand)
            {
                var command = new CommandWait(obj.Reference.GetComponent<WaitPropertyProvider>(), ENUM_COMMANDS.WAIT, obj.Id);
                programm.Add(command);
            }
        }
        void ControllerResetState(RobotsControllerResetState s)
        {
            CommandComplete = true;
            Programm = null;
            Programm = BuildProgramm(ID);
        }
        //--Мгновенное перемещение к переданной точке
        private void TeleportToPoint(PickCommandSignal s)
        {
            if (_simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                SceneObject obj = s.Point;
                if (obj.Type == ObjectType.LinearMoveCommand && (ServiceManager.Current.Get<SceneObjectsManager>().Commands.GetSubProgram(obj.ParentId).ParentId == ID))
                {
                    if (s.Point.Type == ObjectType.LinearMoveCommand)
                    {

                        LinearPointPropertyProvider LPPP = (LinearPointPropertyProvider)s.Point.PropertyProvider;
                        angles = InvKin.IKCalc(_propertyProvider.RP, GetPositionInfo(LPPP));
                        //Выбор конфигурации точки
                        InvKin.CheckLimit(angles[0]);
                        if (InvKin.checkIsNaN(angles[0]))
                        {
                            ModifyRobot(_propertyProvider, angles[0].GetFloats());
                            _propertyProvider.JOGpoint.Position = GetPositionInfo(LPPP).Position;
                            _propertyProvider.JOGpoint.GlobalRotationQ = GetPositionInfo(LPPP).Rotation;

                        }
                    }
                }

            }
        }
        public void TeleportToPoint(LinearPointPropertyProvider p)
        {

        }
        //--Получить позицию точки--
        public Point GetPositionInfo(LinearPointPropertyProvider p)
        {
            Speed = p.LinearSpeed;
            return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.transform.localRotation, Speed = Speed };
        }
        public Point GetPositionInfo(Point p)
        {
            Speed = p.Speed;
            return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.Rotation, Speed = Speed };
        }

        /// <summary>
        /// задать позицию джога
        /// </summary>
        /// <param name="point"></param>
        public void SetJogPosition(Point point)
        {
            _propertyProvider.JOGpoint.Position = point.Position;
            _propertyProvider.JOGpoint.LocalRotationQ = point.Rotation;
        }
        /// <summary>
        /// применить углы на модель робота
        /// </summary>
        /// <param name="_propertyProvider"></param>
        /// <param name="ang"></param>
        public void ModifyRobot(RobotPropertyProvider _propertyProvider, float[] ang)
        {
            _propertyProvider.J1Angle = ang[0];
            _propertyProvider.J2Angle = ang[1];
            _propertyProvider.J3Angle = ang[2];
            _propertyProvider.J4Angle = ang[3];
            _propertyProvider.J5Angle = ang[4];
            _propertyProvider.J6Angle = ang[5];
        }
        void OnDestroy()
        {
            _eventBus.Unsubcribe<StopProgramm>(StopSim);
            _eventBus.Unsubcribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Unsubcribe<RobotsControllerResetState>(ControllerResetState);


        }
    }
}
