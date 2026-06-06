using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace Assets.Scripts.SimulationSystem.RobotSimulation
{

    public class RobotController : MonoBehaviour
    {
        RobotPropertyProvider _propertyProvider;
        SimulationManager _simManager => ServiceManager.Current.Get<SimulationManager>();
        SceneObjectsManager _sceneObjectManager => ServiceManager.Current.Get<SceneObjectsManager>();
        EventBus _eventBus => ServiceManager.Current.Get<EventBus>();
        NotificationSystemManager Notification;

        Angles[] angles;
        
        //оптимизация
        Vector3 oldJOGposition = Vector3.zero;
        Quaternion oldJOGrotation = Quaternion.identity;
        float[] oldAngles = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool needUpdateConf = false;
        int OldConfigPoint = 0;
        bool inProgressAsync = false;
        //
        bool endAnglesMove = false;
        public string ID => _propertyProvider.Id;
        public bool RunTask { get; set; }
        public List<SubProgramm> Programm { get; set; }

        public AnimationCurve SpeedCurve;
        private InverseK_new InvKin;

        Awaitable setJogAsync;
        Awaitable setAngleAsync;
        PointPropertyProvider OldPoint;
        void Start()
        {
            Notification = ServiceManager.Current.Get<NotificationSystemManager>();
            InvKin = gameObject.GetComponent<InverseK_new>();
            //_eventBus.Subscribe<StopProgramm>(StopSim);
            _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Subscribe<Init>(ControllerResetState);
            _propertyProvider = GetComponent<RobotPropertyProvider>();
            _propertyProvider.JOGpoint.LocalPosition = new Vector3(1, 1, 1);
            _propertyProvider.JOGpoint.Rotation = new Vector3(180, 0, 0);
             _ = SetJogMove();
        }
        private void Update()
        {
            
            if (_simManager.GetModeSim().SimulationMode == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                 _ = SetJogMove();
                _ = SetAngleMove();
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
        public async Awaitable RobotSetLinMove(PointPropertyProvider point)
        {
            if(OldPoint == null)
            {
                _eventBus.Invoke(new SystemPauseSim($"Движение робота должно начинаться с движения Точка-Точка"));
            }
            else
            {
                if (point.PointType == POINTTYPE.LinearPoint && OldPoint.ConfigPoint != point.ConfigPoint)
                {
                    _eventBus.Invoke(new SystemPauseSim($"Неверная конфигурация точки({point.name})"));
                }
            }
            
            UnityEngine.Debug.LogError("Линейное движение: Старт ");

            Point Start = new(_propertyProvider.JOGpoint.LocalPosition, _propertyProvider.JOGpoint.LRotationQ);
            Point End = new(GetPositionInfo(point).Position, GetPositionInfo(point).Rotation);

            Point wayPoint = new(Start.Position, Start.Rotation);
            Vector3 wayDirection = (End.Position - Start.Position).normalized;
            float distance = Vector3.Distance(Start.Position, End.Position);
            Quaternion rotateDirect;
            bool isTriangularProfile = false;
            bool onlyRotate = false;
            //получение парамтеров двжижения
            float LinAcceler = point.LinAcceler;
            float LinBrake = point.LinBrake;
            float AngleAcceler = point.AngleAcceler;
            float AngleBreak = point.AngleBrake;
            float AngleSpeed = point.AngleSpeed;
            float LinearSpeed = point.Speed;
            float vMax = 0;
            //получение парамтеров двжижения
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

                // === Для расчёта скорости и ускорения ===
            float[] previousAngles = new float[6];
            float[] previousVelocities = new float[6] {0,0,0,0,0,0 };

            float[] CurrentAngles  = new float[6];
            float[] AngularVelocities  = new float[6];   // град/сек
            float[] AngularAccelerations  = new float[6]; // град/сек²
            CurrentAngles = InvKin.IK(_propertyProvider.RP, Start.Position, Start.Rotation)[point.ConfigPoint].GetFloats();
            Array.Copy(CurrentAngles, previousAngles, 6);
            float dt = 0;
            //равноускоренное вращение
            if (Vector3.Distance(End.Position, Start.Position) < 0.01)
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

            while (Vector3.Distance(End.Position, wayPoint.Position) > 0.01 || (Quaternion.Angle(wayPoint.Rotation, End.Rotation) > 0.01))
            {
                
                while (_simManager.GetStatusSim() == SIM_STAT.PAUSE)
                {
                    if(_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                    await Awaitable.FixedUpdateAsync();
                }
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

                if (InvKin.CheckLimit(angles[point.ConfigPoint], _propertyProvider.AnglesLimit))
                {
                    _eventBus.Invoke(new SystemPauseSim("Линейное движение невозможно, ось достигла предела"));
                }
                if (InvKin.checkIsNaN(angles[point.ConfigPoint]))
                {
                    ModifyRobot(_propertyProvider, angles[point.ConfigPoint].GetFloats());
                    SetJogPosition(wayPoint);

                    //расчет фактической сокрости, ускорения осей
                     dt += Time.deltaTime;

                    if (dt > 0.1 && _simManager.GetSimulationParam().CheckSpeed)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            CurrentAngles[i] = angles[point.ConfigPoint].GetThetha(i);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            float deltaAngle = Mathf.DeltaAngle(previousAngles[i], CurrentAngles[i]);
                            AngularVelocities[i] = deltaAngle / dt;

                            // Угловое ускорение
                            AngularAccelerations[i] = (AngularVelocities[i] - previousVelocities[i]) / dt;

                            // Обновляем предыдущие значения
                            previousAngles[i] = CurrentAngles[i];
                            previousVelocities[i] = AngularVelocities[i];
                        }
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        sb.AppendLine("=== Robot Joints Velocity & Acceleration ===");
                        for (int i = 0; i < 6; i++)
                        {
                            sb.AppendLine($"J{i + 1}:  " +
                                         $"Angle = {CurrentAngles[i]:F1}° | " +
                                         $"Vel = {AngularVelocities[i]:F2} °/s | " +
                                         $"Acc = {AngularAccelerations[i]:F2} °/s²");
                        }
                        UnityEngine.Debug.Log(sb.ToString());
                        for (int i = 0; i < 6; i++)
                        {
                            if ((currentTimeMove <= tAcсeler) && Mathf.Abs(AngularAccelerations[i]) > _propertyProvider.AngleAcceler.GetThetha(i))
                            {
                                _eventBus.Invoke(new SystemPauseSim($"Ось А{i + 1} (Текущая сокрость:{Mathf.Abs(AngularAccelerations[i])}); Ограничение:({_propertyProvider.AngleAcceler.GetThetha(i)})\n Измените парамерты движения к точке {point.Name}"));
                                break;
                            }
                            if ((currentTimeMove > tAcсeler) && Mathf.Abs(AngularAccelerations[i]) > _propertyProvider.AngleBrake.GetThetha(i))
                            {
                                _eventBus.Invoke(new SystemPauseSim($"Ось А{i + 1} (Текущее ускорение торможения:{Mathf.Abs(AngularAccelerations[i])}); Ограничение({_propertyProvider.AngleBrake.GetThetha(i)})\n Измените парамерты движения к точке {point.Name}"));
                                break;
                            }
                            if (AngularVelocities[i] > _propertyProvider.AnglesSpeedLimit.GetThetha(i))
                            {
                                _eventBus.Invoke(new SystemPauseSim($"Ось A{i + 1} (Текущая скорость:{AngularVelocities[i]}); Ограничение({_propertyProvider.AnglesSpeedLimit.GetThetha(i)})\n Измените парамерты движения к точке {point.Name}"));
                                break;
                            }

                        }
                        
                        dt = 0;
                    }
                    //расчет фактической сокрости, ускорения осей

                    currentTimeMove += Time.deltaTime;
                    await Awaitable.FixedUpdateAsync();
                }
                else
                {
                    await Awaitable.FixedUpdateAsync();
                    UnityEngine.Debug.LogError("Ошибка линейного движения");
                }

            }
            OldPoint = point;
            return;
        }
        /// <summary>
        /// расчет параметров равноускоренного движения, трапеция и треугольник
        /// </summary>
        /// <param name="Speed"></param>
        /// <param name="Acceler"></param>
        /// <param name="Brake"></param>
        /// <param name="angle"></param>
        public (float tAcсeler, float sAcсeler, float tBrake, float sBrake, float tLinear, float sLinear, float vMax, int DirectRoteate, bool isTriangularProfile)
            Сalc(float Speed, float Acceler, float Brake, float angle, int direct)
        {
            float tAcсeler = Speed / Acceler;
            float tBrake = Speed / Brake;
            float sAcсeler = (Acceler * tAcсeler * tAcсeler) / 2;
            float sBrake = (Brake * tBrake * tBrake) / 2;
            float sLinear = 0;
            float tLinear = 0;
            float vMax = Speed;
            int DirectRoteate = direct;
            bool isTriangularProfile = false;
            //трангулярная скорость
            if ((sAcсeler + sBrake) > Mathf.Abs(angle))
            {
                isTriangularProfile = true;
                vMax = Mathf.Sqrt(Mathf.Abs(angle) / ((1 / (2 * Acceler)) + (1 / (2 * Brake))));
                if (Mathf.Abs(angle) <= 1e-6f)
                {
                    vMax = 0f;
                }
                tAcсeler = vMax / Acceler;
                tBrake = vMax / Brake;
                sAcсeler = (Acceler * tAcсeler * tAcсeler) / 2;
                sBrake = (Brake * tBrake * tBrake) / 2;
            }
            //трапецивидная скорость
            else
            {
                sLinear = Mathf.Abs(angle) - (sAcсeler + sBrake);
                tLinear = sLinear / Speed;
            }
            return (tAcсeler, sAcсeler, tBrake, sBrake, tLinear, sLinear, vMax, DirectRoteate, isTriangularProfile);
        }

        public async Awaitable RobotSetPTPMove(PointPropertyProvider point)
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

            Angles EndAngles = InvKin.IKCalc(_propertyProvider.RP, End.Position, End.Rotation)[point.ConfigPoint];
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

            while (CurrentAngles.Diff(EndAngles) > 0.01f && CurrentTimeMove < LongTime)
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                while (_simManager.GetStatusSim() == SIM_STAT.PAUSE)
                {
                    if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                    await Awaitable.FixedUpdateAsync();
                }
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
                _propertyProvider.JOGpoint.ConfigPoint = point.ConfigPoint;
            }
            OldPoint = point;

        }
        //=================================== КОМАНДЫ ===================================//
        //--Задать позицию ДЖОГа
        
        public async Awaitable SetJogMove()
        {
            if (setJogAsync != null && !setJogAsync.IsCompleted) return;
            setJogAsync = _SetJogMove();
            await setJogAsync;
            setJogAsync = null;
        }
        public async Awaitable _SetJogMove()
        {
            if ((oldJOGposition != _propertyProvider.JOGpoint.LocalPosition || oldJOGrotation != _propertyProvider.JOGpoint.LRotationQ || OldConfigPoint != _propertyProvider.JOGpoint.ConfigPoint) &&  !_propertyProvider.JOGpoint.AngleMode)
            {

                angles = InvKin.IKCalc(_propertyProvider.RP, _propertyProvider.JOGpoint.LocalPosition, _propertyProvider.JOGpoint.LRotationQ);
                bool InLimit = false;
                if (_propertyProvider.JOGpoint.VerificationAngles) 
                {
                    InLimit =  InvKin.CheckLimit(angles[_propertyProvider.JOGpoint.ConfigPoint], _propertyProvider.AnglesLimit);
                };
                if (!InLimit && InvKin.checkIsNaN(angles[_propertyProvider.JOGpoint.ConfigPoint]))
                {

                    ModifyRobot(_propertyProvider, angles[_propertyProvider.JOGpoint.ConfigPoint].GetFloats());
                    angles[_propertyProvider.JOGpoint.ConfigPoint].GetFloats().CopyTo(_propertyProvider.ChangeAngles, 0);
                    angles[_propertyProvider.JOGpoint.ConfigPoint].GetFloats().CopyTo(oldAngles,0);
                    //////
                    await Awaitable.NextFrameAsync();

                    //var jogPos = _propertyProvider.JOGpoint.GlobalPosition;
                    //var objPos = _propertyProvider.GetActualPosEffector().Position;
                    //UnityEngine.Debug.LogWarning($"JOG:({jogPos.x:F4}, {jogPos.y:F4}, {jogPos.z:F4});;;OBJ:({objPos.x:F4}, {objPos.y:F4}, {objPos.z:F4})");
                }
                else
                {
                    //Notification.ShowWarning("Точка вне зоны досягаемости");
                    _propertyProvider.JOGpoint.LocalPosition = oldJOGposition;
                    _propertyProvider.JOGpoint.LRotationQ = oldJOGrotation;
                    return;
                }
                oldJOGposition = _propertyProvider.JOGpoint.LocalPosition;
                oldJOGrotation = _propertyProvider.JOGpoint.LRotationQ;
                OldConfigPoint = _propertyProvider.JOGpoint.ConfigPoint;

                _eventBus.Invoke(new ChangeAnglesJOGSignal(_propertyProvider));
            }

        }
        
        public async Awaitable SetAngleMove()
        {
            if (setAngleAsync != null && !setAngleAsync.IsCompleted) return;
            setAngleAsync =  _SetAngleMove();
            await setAngleAsync;
            setAngleAsync = null;
        }
        public async Awaitable _SetAngleMove()
        {
            if (Diff(oldAngles, _propertyProvider.ChangeAngles) > 0.001 && _propertyProvider.JOGpoint.AngleMode)
            {
                _propertyProvider.ChangeAngles = InvKin.CheckLimit(_propertyProvider.ChangeAngles, _propertyProvider.AnglesLimit); //углы
                ModifyRobot(_propertyProvider, _propertyProvider.ChangeAngles);
                await Awaitable.NextFrameAsync();
                Point position = new(); 
                position.Position = transform.InverseTransformPoint(_propertyProvider.GetActualPosEffector().Position);
                position.Rotation = Quaternion.Inverse(transform.rotation) * _propertyProvider.GetActualPosEffector().Rotation;
                _propertyProvider.JOGpoint.ConfigPoint = OldConfigPoint = InvKin.CheckConfig(new Angles(_propertyProvider.J1Angle, _propertyProvider.J2Angle, _propertyProvider.J3Angle, _propertyProvider.J4Angle, _propertyProvider.J5Angle, _propertyProvider.J6Angle), _propertyProvider.RP, position);
                _propertyProvider.JOGpoint.LocalPosition = oldJOGposition = position.Position;
                _propertyProvider.JOGpoint.LRotationQ = oldJOGrotation = position.Rotation;

                _propertyProvider.ChangeAngles.CopyTo(oldAngles, 0);
                _eventBus.Invoke(new ChangeConfigJOGSignal(_propertyProvider));
            }
        }

        float NormalizeAngle360(float angle)
        {
            angle = angle % 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }
        public float Diff(float[] a1, float[] a2)
        {
            float max = float.NegativeInfinity;
            for (int i = 0; i < 6; i++)
            {
                float diff = Mathf.Abs(NormalizeAngle360(a1[i]) - NormalizeAngle360(a2[i]));
                if (diff > max) max = diff;

            }
            return max;
        }

        //--Выполнить подпрограмму (задачу)--
        public void RunSubProgramm(string IDTaskToRun)
        {
            try
            {
                RunTask = true;
                _ = Run(Programm.FirstOrDefault(x => x.ID == IDTaskToRun));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Подпрограмма с ID '{ID}' для робота {IDTaskToRun} ошибка: {ex}");
            }


        }
        //-- Выполнение подпрограммы (задачи)--
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
                await comand.Execute(this);

            }
            UnityEngine.Debug.LogWarning("Задача завершена");
            RunTask = false;

        }
        /*public void StopSim(StopProgramm s)
        {
            RunTask = false;
            CommandComplete = true;
        }*/
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
                var command = new CommandMove(obj.Reference.GetComponent<PointPropertyProvider>(), ENUM_COMMANDS.MOVE_LIN, obj.Id);
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
        void ControllerResetState(Init s)
        {
            RunTask = false;
            Programm = null;
            OldPoint = null;
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

                        PointPropertyProvider LPPP = (PointPropertyProvider)s.Point.PropertyProvider;
                        angles = InvKin.IKCalc(_propertyProvider.RP, GetPositionInfo(LPPP));
                        //Выбор конфигурации точки
                        //InvKin.CheckLimit(angles[LPPP.ConfigPoint], _propertyProvider.AnglesLimit);
                        if (InvKin.checkIsNaN(angles[LPPP.ConfigPoint]))
                        {
                            ModifyRobot(_propertyProvider, angles[LPPP.ConfigPoint].GetFloats());
                            _propertyProvider.JOGpoint.LocalPosition = GetPositionInfo(LPPP).Position;
                            _propertyProvider.JOGpoint.LRotationQ = GetPositionInfo(LPPP).Rotation;
                            _propertyProvider.JOGpoint.ConfigPoint = LPPP.ConfigPoint;

                        }
                        else
                        {
                            Notification.ShowError($"Точка({LPPP.Name}) недосягаема!");
                        }
                    }
                }

            }
        }
        //--Получить позицию точки--
        public Point GetPositionInfo(PointPropertyProvider p)
        {
            //Speed = p.LinearSpeed;
            return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.transform.localRotation};
        }
        public Point GetPositionInfo(Point p)
        {
            //Speed = p.Speed;
            return new Point { Position = _propertyProvider.transform.InverseTransformPoint(p.Position), Rotation = p.Rotation};
        }

        /// <summary>
        /// задать позицию джога
        /// </summary>
        /// <param name="point"></param>
        public void SetJogPosition(Point point)
        {
            _propertyProvider.JOGpoint.LocalPosition = point.Position;
            _propertyProvider.JOGpoint.LRotationQ = point.Rotation;
        }
        /// <summary>
        /// применить углы на модель робота
        /// </summary>
        /// <param name="_propertyProvider"></param>
        /// <param name="ang"></param>
        public float[] ModifyRobot(RobotPropertyProvider _propertyProvider, float[] ang)
        {
            float[] deltaThetha = new float[6];

            deltaThetha[0] = Mathf.DeltaAngle(_propertyProvider.J1Angle, ang[0]);
            deltaThetha[1] = Mathf.DeltaAngle(_propertyProvider.J2Angle, ang[1]);
            deltaThetha[2] = Mathf.DeltaAngle(_propertyProvider.J3Angle, ang[2]);
            deltaThetha[3] = Mathf.DeltaAngle(_propertyProvider.J4Angle, ang[3]);
            deltaThetha[4] = Mathf.DeltaAngle(_propertyProvider.J5Angle, ang[4]);
            deltaThetha[5] = Mathf.DeltaAngle(_propertyProvider.J6Angle, ang[5]);

            _propertyProvider.J1Angle = ang[0];
            _propertyProvider.J2Angle = ang[1];
            _propertyProvider.J3Angle = ang[2];
            _propertyProvider.J4Angle = ang[3];
            _propertyProvider.J5Angle = ang[4];
            _propertyProvider.J6Angle = ang[5];
            return deltaThetha;
        }
       
        void OnDestroy()
        {
            //_eventBus.Unsubcribe<StopProgramm>(StopSim);
            _eventBus.Unsubcribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Unsubcribe<Init>(ControllerResetState);


        }
    }
}
