using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.Providers.PropertyProviders;
using System;
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

        Angles[] angles;

        //оптимизация
        Vector3 oldJOGposition = Vector3.zero;
        Quaternion oldJOGrotation = Quaternion.identity;
        float[] oldAngles = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool needUpdateConf = false;
        int OldConfigPoint = 0;

        public string ID => _propertyProvider.Id;
        public bool RunTask { get; set; }
        public List<SubProgramm> Programm { get; set; }

        public AnimationCurve SpeedCurve;
        private InverseK_new InvKin;



        void Start()
        {
            InvKin = gameObject.GetComponent<InverseK_new>();
            //_eventBus.Subscribe<StopProgramm>(StopSim);
            _eventBus.Subscribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Subscribe<Init>(ControllerResetState);
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
            /*Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                InvKin.IKCalc(_propertyProvider.RP, testData[i], Quaternion.identity);
            }
            stopwatch.Stop();
            UnityEngine.Debug.Log($"10000 точек IKCalc выполнился за: {(double)stopwatch.ElapsedMilliseconds / Stopwatch.Frequency} с или {stopwatch.ElapsedTicks} тиков ");
            */
            _SetJogMove();
        }
        private void FixedUpdate()
        {
            if(_simManager.GetModeSim() == (MODE.JOG_MODE, MODE.ANGLES_MODE) && needUpdateConf)
            {
                int config = InvKin.CheckConfig(new Angles(_propertyProvider.ChangeAngles) ,_propertyProvider.RP, _propertyProvider.JOGpoint.Position, _propertyProvider.JOGpoint.LocalRotationQ);

                _propertyProvider.JOGpoint.ConfigPoint = config;
                needUpdateConf = false;
            }
            if (_simManager.GetModeSim().SimulationMode == MODE.JOG_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                _ = SetJogMove();
            }
            else if (_simManager.GetModeSim().SimulationMode == MODE.ANGLES_MODE && _simManager.GetStatusSim() == SIM_STAT.STOP)
            {
                _ = SetAngleMove();
                needUpdateConf = true;
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

            while (Vector3.Distance(End.Position, wayPoint.Position) > 0.001 || (Quaternion.Angle(wayPoint.Rotation, End.Rotation) > 0.001))
            {
                if (_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                while (_simManager.GetStatusSim() == SIM_STAT.PAUSE)
                {
                    if(_simManager.GetStatusSim() == SIM_STAT.STOP) return;
                    await Awaitable.FixedUpdateAsync();
                }
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
                InvKin.CheckLimit(angles[point.ConfigPoint]);
                if (InvKin.checkIsNaN(angles[point.ConfigPoint]))
                {
                    /*stopwatch.Stop();
                    long freq = Stopwatch.Frequency;
                    UnityEngine.Debug.Log($"Метод IKCalc выполнился за: {(double)stopwatch.ElapsedMilliseconds/freq} с");
                    UnityEngine.Debug.Log($"Или в тиках: {stopwatch.ElapsedTicks}");*/
                    ModifyRobot(_propertyProvider, angles[point.ConfigPoint].GetFloats());
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

            while (CurrentAngles.Diff(EndAngles) > 0.001f)
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
            }

        }
        //=================================== КОМАНДЫ ===================================//
        //--Задать позицию ДЖОГа
        public async Awaitable SetJogMove()
        {
            await _SetJogMove();
        }
        public async Awaitable _SetJogMove()
        {
            if (oldJOGposition != _propertyProvider.JOGpoint.Position || oldJOGrotation != _propertyProvider.JOGpoint.LocalRotationQ || OldConfigPoint != _propertyProvider.JOGpoint.ConfigPoint)
            {
                
                angles = InvKin.IKCalc(_propertyProvider.RP, _propertyProvider.JOGpoint.Position, _propertyProvider.JOGpoint.LocalRotationQ);
                //добавить автовыбор конфигурации или ручной ввод\
                bool InLimit = false;
                if (_propertyProvider.JOGpoint.VerificationAngles) 
                {
                    InLimit =  InvKin.CheckLimit(angles[_propertyProvider.JOGpoint.ConfigPoint]);
                };
                if (!InLimit && InvKin.checkIsNaN(angles[_propertyProvider.JOGpoint.ConfigPoint]))
                {
                    ModifyRobot(_propertyProvider, angles[_propertyProvider.JOGpoint.ConfigPoint].GetFloats());
                    angles[_propertyProvider.JOGpoint.ConfigPoint].GetFloats().CopyTo(_propertyProvider.ChangeAngles, 0);

                    //////
                    //await Awaitable.FixedUpdateAsync();
                    //var jogPos = _propertyProvider.JOGpoint.GlobalPosition;
                    //var objPos = _propertyProvider.GetActualPosEffector().Position;

                    //UnityEngine.Debug.LogWarning($"JOG:({jogPos.x:F4}, {jogPos.y:F4}, {jogPos.z:F4});;;OBJ:({objPos.x:F4}, {objPos.y:F4}, {objPos.z:F4})");
                    //_propertyProvider.XYZ = _propertyProvider.oldXYZ = EffectorPosition.Position;
                    //_propertyProvider.XYZRot = _propertyProvider.oldXYZRot = EffectorPosition.Rotation;
                }
                else
                {
                    _propertyProvider.JOGpoint.Position = oldJOGposition;
                    _propertyProvider.JOGpoint.LocalRotationQ = oldJOGrotation;
                    //UnityEngine.Debug.LogError("Точка недостижима");
                }
                oldJOGposition = _propertyProvider.JOGpoint.Position;
                oldJOGrotation = _propertyProvider.JOGpoint.LocalRotationQ;

            }

        }
        public async Awaitable SetAngleMove()
        {
            await _SetAngleMove();
        }
        public async Awaitable _SetAngleMove()
        {
            if (!oldAngles.SequenceEqual(_propertyProvider.ChangeAngles))
            {
                _propertyProvider.ChangeAngles = InvKin.CheckLimit(_propertyProvider.ChangeAngles); //углы
                ModifyRobot(_propertyProvider, _propertyProvider.ChangeAngles);
                await Awaitable.FixedUpdateAsync();
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
        void ControllerResetState(Init s)
        {
            RunTask = false;
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
                        InvKin.CheckLimit(angles[LPPP.ConfigPoint]);
                        if (InvKin.checkIsNaN(angles[LPPP.ConfigPoint]))
                        {
                            ModifyRobot(_propertyProvider, angles[LPPP.ConfigPoint].GetFloats());
                            _propertyProvider.JOGpoint.Position = GetPositionInfo(LPPP).Position;
                            _propertyProvider.JOGpoint.GlobalRotationQ = GetPositionInfo(LPPP).Rotation;

                        }
                    }
                }

            }
        }
        //--Получить позицию точки--
        public Point GetPositionInfo(LinearPointPropertyProvider p)
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
            //_eventBus.Unsubcribe<StopProgramm>(StopSim);
            _eventBus.Unsubcribe<PickCommandSignal>(TeleportToPoint);
            _eventBus.Unsubcribe<Init>(ControllerResetState);


        }
    }
}
