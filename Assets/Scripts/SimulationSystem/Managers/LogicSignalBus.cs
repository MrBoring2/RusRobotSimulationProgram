//using Assets.Scripts.CustomServiceManager;
//using System.Collections.Generic;
//using UnityEngine;

//public class LogicSignalBus : MonoBehaviour, IService
//{
//    private Dictionary<string, bool> signals = new Dictionary<string, bool>();
//    private Dictionary<string, int> intData = new Dictionary<string, int>();
//    public void Init()
//    {
//        signals.Add("key", true);
//        intData.Add("counter", 0);
//    }
//    public void SetSignal(string signalName, bool value)
//    {
//        if (!signals.ContainsKey(signalName))
//        {
//            signals.Add(signalName, false);
//        }
//        signals[signalName] = value;
//        Debug.Log($"[SIGNAL] {signalName} = {value}");
//    }

//    public bool GetSignal(string signalName)
//    {
//        return signals.ContainsKey(signalName) && signals[signalName];
//    }
//    public void SetIntData(string dataName, int value)
//    {
//        if (!intData.ContainsKey(dataName))
//        {
//            intData.Add(dataName, 0);
//        }
//        intData[dataName] = value;
//        Debug.Log($"[INT DATA] {dataName} = {value}");
//    }
//    public int GetIntData(string dataName)
//    {
//        return intData.ContainsKey(dataName) ? intData[dataName] : 0;
//    }

//    public Dictionary<string, bool> GetSignals()
//    {
//               return signals;
//    }
//    public Dictionary<string, int> GetIntData()
//    {
//        return intData;
//    }

//}
////////////////////////////////  списки для теста чтобы отображалось в инспекторе
using Assets.Scripts.CustomServiceManager;
using System.Collections.Generic;
using UnityEngine;

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

    private Dictionary<string, bool> CopySignalsDict = new Dictionary<string, bool>();
    private Dictionary<string, int> CopyIntDataDict = new Dictionary<string, int>();

    private Dictionary<string, bool> BufSignalsDict = new Dictionary<string, bool>();
    private Dictionary<string, int> BufIntDataDict = new Dictionary<string, int>();

    public void Init(){}
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
            boolSignals.Add(new SignalEntry { key = signalName, value = value });
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
            intDataList.Add(new IntDataEntry { key = DataName, value = ValueToSet });
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

    //////////////////////////// Методы для работы с копиями словарей
    public void CreateSignalCadr()
    {
        CopySignalsDict.Clear();
        foreach (var kvp in signalsDict)
        {
            CopySignalsDict.Add(kvp.Key, kvp.Value);
        }
        CopyIntDataDict.Clear();
        foreach (var kvp in intDataDict)
        {
            CopyIntDataDict.Add(kvp.Key, kvp.Value);
        }
    }
    public void CadrToActiveSignal()
    {
        foreach (var kvp in BufSignalsDict)
        {
            SetSignal(kvp.Key, kvp.Value);
        }
        foreach (var kvp in BufIntDataDict)
        {
            SetIntData(kvp.Key, kvp.Value);
        }
    }
    public bool GetCopySignal(string signalName)
    {
        return CopySignalsDict.ContainsKey(signalName) && CopySignalsDict[signalName];
    }
    public int GetCopyIntData(string DataName)
    {
        return CopyIntDataDict.ContainsKey(DataName) ? CopyIntDataDict[DataName] : 0;
    }
    
    public Dictionary<string, bool> GetCopySignals()
    {
        
        return CopySignalsDict;
    }
    public Dictionary<string, int> GetCopyIntData()
    {
        
        return CopyIntDataDict;
    }
    public void SetCopySignal(string signalName, bool value)
    {
        if(BufSignalsDict.ContainsKey(signalName))
        {
            BufSignalsDict[signalName] = value;
        }
        else
        {
            BufSignalsDict.Add(signalName, value);
        }
    }
    public void SetCopyIntData(string dataName, int value)
    {
        if(BufIntDataDict.ContainsKey(dataName))
        {
            BufIntDataDict[dataName] = value;
        }
        else
        {
            BufIntDataDict.Add(dataName, value);
        }
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








