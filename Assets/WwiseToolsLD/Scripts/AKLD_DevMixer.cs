/*
 * AKLD_DevMixerGroup.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
 * 
 * Description:
 * Editor-only dev mixer for Unity + Wwise.
 * Controls global RTPCs via a 0–100 knob mapped to [min..max],
 * with per-RTPC mute/solo and a master mute.
 * Applies changes on Update/OnValidate and avoids redundant sets.
 * Ideal for fast testing when menus/UI aren’t ready.
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
[AddComponentMenu("AKLD/AKLD Dev Mixer")]
[DisallowMultipleComponent]
public class AKLD_DevMixerGroup : MonoBehaviour
{
    [System.Serializable]
    public class AKLD_DevMixer
    {
        [Tooltip("Global Wwise RTPC to control")]
        public AK.Wwise.RTPC rtpc;

        [Tooltip("Minimum value for this RTPC")]
        public float min = 0f;

        [Tooltip("Maximum value for this RTPC")]
        public float max = 100f;

        [Tooltip("Knob value (0–100) remapped to [min..max]")]
        [Range(0, 100)] public float potentiometerValue = 80f;

        [Tooltip("Mute this RTPC (forces it to min)")]
        public bool mute = false;

        [Tooltip("Solo this RTPC (when any solo is active, others are muted)")]
        public bool solo = false;

        // Internal cache to avoid redundant SetGlobalValue calls
        private float lastAppliedValue = float.NaN;
        private bool wasMuted = false;

        // Remap 0–1 to [min..max]
        float Remap01ToRange(float t01) => Mathf.Lerp(min, max, Mathf.Clamp01(t01));

        public void Apply(bool masterMute, bool anySoloActive)
        {
            if (rtpc == null) return;

            // Priority: masterMute > solo > local mute
            bool effectiveMute = masterMute || (anySoloActive ? !solo : mute);

            float knob01 = potentiometerValue / 100f;
            float targetValue = effectiveMute ? min : Remap01ToRange(knob01);

            if (wasMuted != effectiveMute || !Mathf.Approximately(lastAppliedValue, targetValue))
            {
                rtpc.SetGlobalValue(targetValue);
                lastAppliedValue = targetValue;
                wasMuted = effectiveMute;
            }
        }
    }

    [Header("Master Controls")]
    [Tooltip("Mutes the entire group (sets all RTPCs to their min)")]
    public bool masterMute = false;

    [Space]
    [Tooltip("Technical mixers for global RTPCs")]
    public List<AKLD_DevMixer> devMixers = new List<AKLD_DevMixer>();

    public void AddDevMixer(AK.Wwise.RTPC rtpc)
    {
        if (rtpc == null) return;
        var m = new AKLD_DevMixer { rtpc = rtpc };
        devMixers.Add(m);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    void Update() => ApplyAll();

    // Apply immediately when moving sliders/toggles in the Inspector
    void OnValidate() => ApplyAll();

    void ApplyAll()
    {
        bool anySolo = !masterMute && devMixers.Exists(m => m != null && m.solo);

        foreach (var m in devMixers)
        {
            if (m == null) continue;
            m.Apply(masterMute, anySolo);
        }
    }
}
#endif
