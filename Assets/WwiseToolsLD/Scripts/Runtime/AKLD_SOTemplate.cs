/*
* AKLD_SOTemplate.cs
* Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
*
* PURPOSE
* ScriptableObject con listas dinámicas (Events/RTPCs/Switches/States).
* El generador crea una partial con MÉTODOS autocompletables.
* RTPCs: SIEMPRE GLOBALES (usa AkSoundEngine.SetRTPCValue).
*/

using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "AudioLD", fileName = "AKLD_SOTemplate")]
public partial class AKLD_SOTemplate : ScriptableObject
{
    [Serializable] public class ComponentEvent { public string componentName; public AK.Wwise.Event eventComponent; }
    [Serializable] public class ComponentRTPC { public string componentName; public AK.Wwise.RTPC rtpcComponent; }
    [Serializable] public class ComponentSwitch { public string componentName; public AK.Wwise.Switch switchComponent; }
    [Serializable] public class ComponentState { public string componentName; public AK.Wwise.State stateComponent; }

    [Header("Event Components")]
    public List<ComponentEvent> eventComponents = new();

    [Header("RTPC Components (GLOBAL)")]
    public List<ComponentRTPC> rtpcComponents = new();

    [Header("Switch Components")]
    public List<ComponentSwitch> switchComponents = new();

    [Header("State Components")]
    public List<ComponentState> stateComponents = new();

    // Lookups (usados por la partial autogenerada)
    public AK.Wwise.Event GetEventComponent(string key) => eventComponents.Find(x => x.componentName == key)?.eventComponent;
    public AK.Wwise.RTPC GetRTPCComponent(string key) => rtpcComponents.Find(x => x.componentName == key)?.rtpcComponent;
    public AK.Wwise.Switch GetSwitchComponent(string key) => switchComponents.Find(x => x.componentName == key)?.switchComponent;
    public AK.Wwise.State GetStateComponent(string key) => stateComponents.Find(x => x.componentName == key)?.stateComponent;

    // (Opcional) helpers genéricos
    public void Post(AK.Wwise.Event evt, GameObject go) { evt?.Post(go); }
    public void Set(AK.Wwise.Switch sw, GameObject go) { sw?.SetValue(go); }
    public void Set(AK.Wwise.State st) { st?.SetValue(); }
    public void SetGlobal(AK.Wwise.RTPC rtpc, float value)
    {
        if (rtpc == null) { Debug.LogWarning("[AKLD_SOTemplate] RTPC null (SetGlobal)."); return; }
        AkSoundEngine.SetRTPCValue(rtpc.Id, value);
    }
}
