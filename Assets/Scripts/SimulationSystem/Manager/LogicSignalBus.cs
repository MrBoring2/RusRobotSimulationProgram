using System.Collections.Generic;
using UnityEngine;

public class LogisSignalBus
{
    private Dictionary<string, bool> signals = new Dictionary<string, bool>();

    public void SetSignal(string signalName, bool value)
    {
        if (!signals.ContainsKey(signalName))
            signals.Add(signalName, false);
        signals[signalName] = value;
        Debug.Log($"[SIGNAL] {signalName} = {value}");
    }

    public bool GetSignal(string signalName)
    {
        return signals.ContainsKey(signalName) && signals[signalName];
    }

}