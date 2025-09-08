// ───────────────────────────────────────────────────────────────────────────────
// AKLD_SOTemplate.cs
// Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
// 
// PURPOSE
// A lightweight ScriptableObject to centralize Wwise references (Events, RTPCs,
// Switches, States) and access them by a logical string key. It acts as an
// abstraction layer: code calls by key, designers map keys to actual Wwise assets.
//
// WHY
// • Decouple code from concrete Wwise assets.
// • Let audio designers swap assets in the Inspector without code changes.
// • Keep a single source of truth for common audio references.
//
// REQUIREMENTS
// • Unity with Wwise integration (AK.Wwise types available).
//
// HOW TO USE (Quick Start)
// 1) Create: Right-click in Project → Create → SO/Audio/Template.
// 2) Fill lists with pairs: (Key Name, Wwise Asset).
// 3) Reference this asset from any MonoBehaviour.
// 4) Call by key, e.g.:
//      audioTemplate.PostEventByName("Footstep", gameObject);
//      audioTemplate.SetSwitchByName("Surface", gameObject);
//      audioTemplate.SetRTPCByName("Speed", gameObject, speed);
//      audioTemplate.SetStateByName("Gameplay_Combat");
//
// NOTES
// • Key matching is case-insensitive.
// • Duplicate keys will log a warning; last one wins.
// • You can also fetch the raw Wwise objects: GetEventComponent("Key"), etc.
// ───────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "SO/Audio/Template", fileName = "AKLD_SOTemplate")]
public class AKLD_SOTemplate : ScriptableObject
{
    [Serializable]
    public class ComponentEvent
    {
        [Tooltip("Logical key to look up this Wwise Event (case-insensitive).")]
        public string componentName;

        [Tooltip("Wwise Event reference.")]
        public AK.Wwise.Event eventComponent;
    }

    [Serializable]
    public class ComponentRTPC
    {
        [Tooltip("Logical key to look up this Wwise RTPC (case-insensitive).")]
        public string componentName;

        [Tooltip("Wwise RTPC reference.")]
        public AK.Wwise.RTPC rtpcComponent;
    }

    [Serializable]
    public class ComponentSwitch
    {
        [Tooltip("Logical key to look up this Wwise Switch (case-insensitive).")]
        public string componentName;

        [Tooltip("Wwise Switch reference.")]
        public AK.Wwise.Switch switchComponent;
    }

    [Serializable]
    public class ComponentState
    {
        [Tooltip("Logical key to look up this Wwise State (case-insensitive).")]
        public string componentName;

        [Tooltip("Wwise State reference.")]
        public AK.Wwise.State stateComponent;
    }

    [Header("Events")]
    public List<ComponentEvent> eventComponents = new List<ComponentEvent>();

    [Header("RTPCs")]
    public List<ComponentRTPC> rtpcComponents = new List<ComponentRTPC>();

    [Header("Switches")]
    public List<ComponentSwitch> switchComponents = new List<ComponentSwitch>();

    [Header("States")]
    public List<ComponentState> stateComponents = new List<ComponentState>();

    // Fast lookups (rebuilt on load/validate)
    private Dictionary<string, AK.Wwise.Event> _events;
    private Dictionary<string, AK.Wwise.RTPC> _rtpcs;
    private Dictionary<string, AK.Wwise.Switch> _switches;
    private Dictionary<string, AK.Wwise.State> _states;

    void OnEnable() => RebuildDictionaries();
#if UNITY_EDITOR
    void OnValidate() => RebuildDictionaries();
#endif

    private void RebuildDictionaries()
    {
        _events = BuildDict(eventComponents, e => e.componentName, e => e.eventComponent);
        _rtpcs = BuildDict(rtpcComponents, e => e.componentName, e => e.rtpcComponent);
        _switches = BuildDict(switchComponents, e => e.componentName, e => e.switchComponent);
        _states = BuildDict(stateComponents, e => e.componentName, e => e.stateComponent);
    }

    private static Dictionary<string, TVal> BuildDict<TSrc, TVal>(
        List<TSrc> source,
        Func<TSrc, string> keySelector,
        Func<TSrc, TVal> valSelector)
        where TVal : class
    {
        var dict = new Dictionary<string, TVal>(StringComparer.OrdinalIgnoreCase);
        if (source == null) return dict;

        foreach (var item in source)
        {
            if (item == null) continue;

            var key = keySelector(item);
            var val = valSelector(item);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (dict.ContainsKey(key))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AKLD_SOTemplate] Duplicate key '{key}'. Last one will be used.");
#endif
            }
            dict[key] = val;
        }
        return dict;
    }

    // ── Raw accessors (return null if not found) ────────────────────────────────
    /// <summary>Returns the Wwise Event mapped to the given key, or null if not found.</summary>
    public AK.Wwise.Event GetEventComponent(string key) =>
        (_events != null && _events.TryGetValue(key, out var e)) ? e : null;

    /// <summary>Returns the Wwise RTPC mapped to the given key, or null if not found.</summary>
    public AK.Wwise.RTPC GetRTPCComponent(string key) =>
        (_rtpcs != null && _rtpcs.TryGetValue(key, out var r)) ? r : null;

    /// <summary>Returns the Wwise Switch mapped to the given key, or null if not found.</summary>
    public AK.Wwise.Switch GetSwitchComponent(string key) =>
        (_switches != null && _switches.TryGetValue(key, out var s)) ? s : null;

    /// <summary>Returns the Wwise State mapped to the given key, or null if not found.</summary>
    public AK.Wwise.State GetStateComponent(string key) =>
        (_states != null && _states.TryGetValue(key, out var st)) ? st : null;

    // ── Convenience calls (safe no-ops with warnings if key missing) ───────────
    /// <summary>Posts an Event by key on a GameObject.</summary>
    public void PostEventByName(string key, GameObject gameObject)
    {
        var ev = GetEventComponent(key);
        if (ev == null)
        {
            Debug.LogWarning($"[AKLD_SOTemplate] Event key '{key}' not found.");
            return;
        }
        ev.Post(gameObject);
    }

    /// <summary>Sets a Switch by key on a GameObject.</summary>
    public void SetSwitchByName(string key, GameObject gameObject)
    {
        var sw = GetSwitchComponent(key);
        if (sw == null)
        {
            Debug.LogWarning($"[AKLD_SOTemplate] Switch key '{key}' not found.");
            return;
        }
        sw.SetValue(gameObject);
    }

    /// <summary>Sets an RTPC by key on a GameObject.</summary>
    public void SetRTPCByName(string key, GameObject gameObject, float value)
    {
        var rtpc = GetRTPCComponent(key);
        if (rtpc == null)
        {
            Debug.LogWarning($"[AKLD_SOTemplate] RTPC key '{key}' not found.");
            return;
        }
        rtpc.SetValue(gameObject, value);
    }

    /// <summary>Sets a State by key (global).</summary>
    public void SetStateByName(string key)
    {
        var st = GetStateComponent(key);
        if (st == null)
        {
            Debug.LogWarning($"[AKLD_SOTemplate] State key '{key}' not found.");
            return;
        }
        st.SetValue();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AKLD_SOTemplate))]
public class AKLD_SOTemplateEditor : Editor
{
    private Texture2D _banner;
    private SerializedProperty _scriptProp;

    private void OnEnable()
    {
        _scriptProp = serializedObject.FindProperty("m_Script");
        // Put your banner in a Resources folder; keep your original name if you want.
        _banner = Resources.Load<Texture2D>("Titulo script 7");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_banner != null)
        {
            GUILayout.Space(6f);
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(_banner.height));
            EditorGUI.DrawTextureTransparent(rect, _banner, ScaleMode.ScaleToFit);
            GUILayout.Space(4f);
        }
        else
        {
            EditorGUILayout.HelpBox("Banner not found. Place a texture named 'Titulo script 7' under a Resources folder.", MessageType.Info);
        }

        // Draw everything except the script field
        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
