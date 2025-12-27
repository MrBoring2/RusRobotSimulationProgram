//using Assets.Scripts.Models;
//using System;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;


//public class EVENTS : MonoBehaviour
//{
//    public event Action<SIM> StatusSim;
//    public SIM newStatus = SIM.STOP;
//    private SIM oldStatus = SIM.STOP;
//    public event Action<MODE> Mode;
//    public MODE newMode = MODE.NONE;
//    private MODE oldMode = MODE.NONE;
//    public event Action<CmdLinMovePorpertyProvider> TeleportTo;


//    [Header("UI Elements")]
//    public Button Start_;
//    public Button Stop;
//    public Button Resume;
//    public Button Jog;
//    public Button Step;

//    void Start()
//    {
//        Start_.onClick.AddListener(ST);
//        Stop.onClick.AddListener(Stop_);
//        Resume.onClick.AddListener(REs);
//        Jog.onClick.AddListener(J);
//        Step.onClick.AddListener(Step_);
//        // Назначаем методы на кнопки
//        /*btnLogPosition.onClick.AddListener(LogPosition);
//        btnResetPosition.onClick.AddListener(ResetPosition);
//        btnTogglePanel.onClick.AddListener(TogglePanel);*/

//        // Скрываем панель по умолчанию
//    }

//    void Step_()
//    {
//        Mode.Invoke(MODE.STEP);
//    }
//    void ST()
//    {
//        Mode.Invoke(MODE.NONE);
//        StatusSim.Invoke(SIM.START);
//    }
//    void Stop_()
//    {
//        StatusSim.Invoke(SIM.STOP);
//    }
//    private void REs()
//    {
//        StatusSim.Invoke(SIM.RESUME);
//    }
//    void J() { 
//        Mode.Invoke(MODE.JOG_MODE);
//    }


//    // Update is called once per frame
//    void FixedUpdate()
//    {
//        if(newMode != oldMode)
//        {
//            Mode?.Invoke(newMode);
//            oldMode = newMode = MODE.NONE;
//        }
//        if (newStatus != oldStatus)
//        {
//            StatusSim?.Invoke(newStatus);
//            oldStatus = newStatus = SIM.NONE;
//        }
//    }
//}
