using Assets.Scripts.CustomServiceManager;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.CustomEventBus;

public class LogicSignalBus : MonoBehaviour, IService
{
    [Header("=== Булевые сигналы ===")]
    [SerializeField]
    private List<SignalEntry> boolSignals = new List<SignalEntry>();

    [Header("=== Целочисленные данные ===")]
    [SerializeField]
    private List<IntDataEntry> intDataList = new List<IntDataEntry>();

    private Dictionary<string, bool> signalsDict = new Dictionary<string, bool>();
    private Dictionary<string, int> intDataDict = new Dictionary<string, int>();

    void Start()
    {
         ServiceManager.Current.Get<EventBus>().Subscribe<Init>(Init);
    }
    public void Init() { }
    /// <summary>
    /// инициализация
    /// </summary>
    /// <param name="s"></param>
    void Init(Init s)
    {
        signalsDict.Clear();
        intDataDict.Clear();
    }
    //кновертация словарей для отладки в инспекторе
    private void ConvertDictionatyToList()
    {
        boolSignals.Clear();
        foreach (var kvp in signalsDict)
        {
            boolSignals.Add(new SignalEntry { key = kvp.Key, value = kvp.Value });
        }
        intDataList.Clear();
        foreach (var kvp in intDataDict)
        {
            intDataList.Add(new IntDataEntry { key = kvp.Key, value = kvp.Value });
        }
    }
    //установка булевого сигнала
    public void SetSignal(string signalName, bool value)
    {
        if (signalsDict.ContainsKey(signalName))
        {
            signalsDict[signalName] = value;
        }
        else
        {
            signalsDict.Add(signalName, value);
        }
        ConvertDictionatyToList();
    }
    //установка целочисленного сигнала
    public void SetIntData(string DataName, int ValueToSet)
    {
        if(intDataDict.ContainsKey(DataName))
        {
            intDataDict[DataName] = ValueToSet;
        }
        else
        {
            intDataDict.Add(DataName, ValueToSet);
        }
        ConvertDictionatyToList();
    }
    //получение булевого сигнала
    public bool GetSignal(string signalName)
    {
        return signalsDict.ContainsKey(signalName) && signalsDict[signalName];
    }
    //получение целочисленного сигнала
    public int GetIntData(string DataName)
    {
        return intDataDict.ContainsKey(DataName) ? intDataDict[DataName] : 0;
    }
    //получение всех булевых сигналов
    public Dictionary<string, bool> GetSignals()
    {

        return signalsDict;
    }
    //получение всех целочисленных сигналов
    public Dictionary<string, int> GetIntData()
    {

        return intDataDict;
    }
    //классы для отображения в инспекторе
    [System.Serializable]
    public class SignalEntry
    {
        public string key;
        public bool value;
    }

    [System.Serializable]
    public class IntDataEntry
    {
        public string key;
        public int value;
    }
}