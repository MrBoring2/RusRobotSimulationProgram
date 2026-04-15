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
//////////////////////////////// ДЛЯ ТЕСТА
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

    public void Init()
    {
    }


    private void ConvertListsToDictionaries()
    {
        signalsDict.Clear();
        intDataDict.Clear();

        foreach (var entry in boolSignals)
            if (!string.IsNullOrEmpty(entry.key))
                signalsDict[entry.key] = entry.value;

        foreach (var entry in intDataList)
            if (!string.IsNullOrEmpty(entry.key))
                intDataDict[entry.key] = entry.value;
    }

  

    public Dictionary<string, bool> GetSignals()
    {
        ConvertListsToDictionaries();
        return signalsDict;
    }
    public Dictionary<string, int> GetIntData()
    {
        ConvertListsToDictionaries();
        return intDataDict;
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








