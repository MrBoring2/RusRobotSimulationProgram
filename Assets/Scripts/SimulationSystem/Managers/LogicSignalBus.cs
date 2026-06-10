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
    void Init(Init s)
    {
        signalsDict.Clear();
        intDataDict.Clear();
    }
    
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
    public bool GetSignal(string signalName)
    {
        return signalsDict.ContainsKey(signalName) && signalsDict[signalName];
    }
    public int GetIntData(string DataName)
    {
        return intDataDict.ContainsKey(DataName) ? intDataDict[DataName] : 0;
    }
    public Dictionary<string, bool> GetSignals()
    {

        return signalsDict;
    }
    public Dictionary<string, int> GetIntData()
    {

        return intDataDict;
    }
    /// <summary>
    /// Получение всех сигналов и данных в виде кортежа словарей
    /// </summary>
    /// <returns></returns>
    public (Dictionary<string, bool> Signals, Dictionary<string, int> Data) GetAllData()
    {
        return (signalsDict, intDataDict);
    }

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








