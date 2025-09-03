/*
 * AKLD_HeartbeatModulator.cs
 *
 * Author: Lautaro Dichio
 * Description:
 * Modulates one or more Wwise RTPCs with a heartbeat-shaped curve that is scaled
 * by player proximity to one or more 3D modulation zones. Each zone can define
 * its own Collider and Focus (anchor) used for a top-down (XZ) radial bake that
 * maps distance to a normalized proximity factor in [0..1].
 *
 * The modulation can be synchronized to Wwise music bars/beats via MusicSyncBar
 * callbacks, supporting either frequency (cycles per beat) or BPM subdivisions.
 *
 * Designed for technical sound designers working with Unity + Wwise.
 * Includes scene gizmos and a custom inspector visualizer for fast iteration.
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ===============================
// ReadOnly helper for Inspector
// ===============================


public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(pos, prop, label, true);
        GUI.enabled = true;
    }
}
#endif
// ===============================

[ExecuteAlways]
[AddComponentMenu("AKLD/AKLD Heartbeat Modulator")]
public class AKLD_HeartbeatModulator : MonoBehaviour
{
    // ===============================
    // Public enums (UI selections)
    // ===============================
    public enum SyncMode { Frequency, BPM }
    public enum HeartbeatShape { FastPulse, CalmBeat, StressSpike, Custom }

    // Constants (kept for possible future smoothing reuse)
    const float kOutputSmoothingHz = 10f;
    const float kEps = 1e-4f;

    // ===============================
    // Per-RTPC configuration
    // ===============================
    [System.Serializable]
    public class RTPCSettings
    {
        [Tooltip("Target Wwise RTPC. Final value = initialValue + (curve * proximity * amplitude).")]
        public AK.Wwise.RTPC rtpc;

        [Tooltip("Depth multiplier per RTPC (0 = no modulation, 1 = full depth, >1 = stronger).")]
        public float amplitude = 1f;

        [Tooltip("Baseline of this RTPC. The heartbeat curve (−0.5..+0.5) is added on top, scaled by proximity and amplitude.")]
        public float initialValue = 0f;

        [HideInInspector] public float currentOutputValue = 0f; // last value written (for inspector visualizer)
        [HideInInspector] public float _smoothedInternal;       // unused (reserved for optional smoothing)
    }

    // ===============================
    // A modulation zone = Collider + its own Focus
    // ===============================
    [System.Serializable]
    public class ModulationZone
    {
        [Tooltip("Collider that defines this modulation zone.")]
        public Collider collider;

        [Tooltip("Focus (anchor) for the radial bake of THIS zone. If null, uses the global musicEventPosition.")]
        public Transform focus;

        // Per-zone bake caches
        [HideInInspector] public float[] bakedRadii; // XZ boundary length per sector
        [HideInInspector] public Vector3 lastBakeFocusPos;
        [HideInInspector] public Vector3 lastBakeAreaPos;
        [HideInInspector] public Quaternion lastBakeAreaRot;
        [HideInInspector] public Vector3 lastBakeAreaScale;
    }

    // ===============================
    // Target & areas (zones only)
    // ===============================
    [Header("Target")]
    [Tooltip("GameObject tested against the zones to compute proximity weighting.")]
    public GameObject objectToCheck;

    [Header("Modulation Zones (Collider + Focus)")]
    [Tooltip("Each zone has its own Collider and Focus. Proximity takes the MAX influence across zones.")]
    public List<ModulationZone> zones = new();

    [Header("Fallback Oriented Box (used if no Zones)")]
    [Tooltip("Local center of the fallback oriented box relative to this Transform.")]
    public Vector3 relativeCenter = Vector3.zero;

    [Tooltip("Size (X,Y,Z) of the fallback oriented box in world units.")]
    public Vector3 size = new Vector3(6f, 2f, 6f);

    [Tooltip("Extra local rotation applied to the fallback oriented box.")]
    public Quaternion rotation = Quaternion.identity;

    [Tooltip("Fill color for the fallback box gizmo.")]
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.2f);

    // ===============================
    // Wwise event anchor (music sync focus)
    // ===============================
    [Header("Wwise: Music Event & Global Focus")]
    [Tooltip("Wwise Event used to synchronize heartbeat cycles via MusicSyncBar callback.")]
    public AK.Wwise.Event musicEvent;

    [Tooltip("Global focus used for timing and as fallback (for zones without a Focus or when using the OBB).")]
    public Transform musicEventPosition;

    // ===============================
    // RTPC list
    // ===============================
    [Header("RTPCs")]
    [Tooltip("RTPCs to modulate. Each receives: initialValue + (curve * proximity * amplitude).")]
    public List<RTPCSettings> rtpcs = new();

    // ===============================
    // Heartbeat shape (curve)
    // ===============================
    [Header("Heartbeat Shape (−0.5 .. +0.5)")]
    [Tooltip("Select a built-in heartbeat shape or use Custom. Shapes must output values in [−0.5 .. +0.5].")]
    public HeartbeatShape shape = HeartbeatShape.FastPulse;

    [Header("Custom Curve (if shape = Custom)")]
    [Tooltip("Custom curve expected to be in −0.5..+0.5 over x ∈ [0..1]. No re-centering is applied.")]
    public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    // ===============================
    // Musical sync parameters
    // ===============================
    [Header("Sync")]
    [Tooltip("Sync mode. Frequency = cycles per beat. BPM = musical subdivisions per beat.")]
    public SyncMode syncMode = SyncMode.BPM;

    [Tooltip("In Frequency mode: 1 = one heartbeat cycle per beat.")]
    public float frequency = 1f;

    [Tooltip("Beats per minute used for cycle timing.")]
    public float bpm = 120f;

    [Tooltip("In BPM mode: 1 = quarter notes, 2 = eighths, 0.5 = half notes, etc.")]
    public float pulsesPerBeat = 1f;

    // ===============================
    // Proximity bake (“pizza”) – top-down XZ sectors
    // ===============================
    [Header("Pizza (Top-Down XZ Bake)")]
    [Tooltip("Number of radial sectors used to pre-bake boundary distances for each zone.")]
    [Range(4, 64)] public int sectorCount = 16;

    [Tooltip("If enabled, automatically re-bakes when any focus or zone collider moves.")]
    public bool autoRebakeIfMoved = false;

    [Tooltip("Padding subtracted from each baked boundary distance as a safety margin.")]
    public float bakeEdgePadding = 0.00f;

    // ===============================
    // Smoothing
    // ===============================
    [Header("Smoothing")]
    [Tooltip("Temporal smoothing factor for proximity. Higher = more responsive (less smoothing). Lower reduces jitter.")]
    [Range(0f, 30f)] public float progressSmoothing = 10f;

    // ===============================
    // Debug / Gizmos
    // ===============================
    [Header("Debug")]
    [Tooltip("If enabled, logs MusicSyncBar ticks during Play Mode.")]
    public bool debugBarLogs = true;

    [Tooltip("If enabled, draws gizmos for areas, foci and baked radii.")]
    public bool drawGizmos = true;

    // ===============================
    // Runtime state (read-only / hidden)
    // ===============================
    [ReadOnly, Tooltip("True after the first MusicSyncBar (or immediately if no event).")]
    public bool modulationActive;

    [HideInInspector] // available to code; hidden from Inspector
    public float phaseX;            // 0..1 position within the current heartbeat cycle

    [ReadOnly, Tooltip("Instant proximity factor in [0..1] from the radial bake (0=edge, 1=focus).")]
    public float progress01;        // 0..1 (raw, per-frame MAX across zones)

    [ReadOnly, Tooltip("Smoothed proximity factor in [0..1]. Used in the output formula.")]
    public float progressSmoothed;  // 0..1 (smoothed proximity)

    // ===============================
    // Music timing state
    // ===============================
    private float timeElapsed;
    private bool waitingForBar;
    private volatile bool barTicked;
    private int lastBarStartMs;
    private int barCount;
    private uint playingId = AkSoundEngine.AK_INVALID_PLAYING_ID;

    // ===============================
    // Fallback OBB cache (used if no Zones)
    // ===============================
    [SerializeField, HideInInspector] private float[] _fallbackBakedRadii;
    [SerializeField, HideInInspector] private Vector3 lastBakeFallbackPos;
    [SerializeField, HideInInspector] private Quaternion lastBakeFallbackRot;

    // ===============================
    // Unity lifecycle
    // ===============================
    void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        // Initialize RTPCs to their baselines
        foreach (var r in rtpcs)
        {
            r._smoothedInternal = r.initialValue;
            r.currentOutputValue = r.initialValue;
            if (r.rtpc != null) r.rtpc.SetGlobalValue(r.initialValue);
        }

        progress01 = progressSmoothed = 0f;

        // Bake proximity at start
        EnsureBake(true);

        // Post music event (if assigned) to receive bar callbacks for syncing
        if (musicEvent != null && musicEventPosition != null)
        {
            playingId = musicEvent.Post(
                musicEventPosition.gameObject,
                (uint)AkCallbackType.AK_MusicSyncBar,
                MusicCallback,
                this
            );

            if (playingId != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                // start after the first bar tick for clean alignment
                waitingForBar = true;
                modulationActive = false;
            }
            else
            {
                Debug.LogWarning("[AKLD_HeartbeatModulator] Failed to post music event.");
                modulationActive = true;
            }
        }
        else
        {
            Debug.LogWarning("[AKLD_HeartbeatModulator] Missing musicEvent or musicEventPosition.");
            modulationActive = true;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (objectToCheck == null) return;

        if (autoRebakeIfMoved) EnsureBake(false);

        // -------------------------------------------------
        // Proximity (“pizza”) — compute progress01 in [0..1]
        // MAX across zones; if no zones, fallback OBB.
        // -------------------------------------------------
        float maxProgress = 0f;

        if (HasAnyZones())
        {
            Vector3 playerPos = objectToCheck.transform.position;

            for (int zi = 0; zi < zones.Count; zi++)
            {
                var z = zones[zi];
                if (z == null || z.collider == null || z.bakedRadii == null) continue;

                Transform f = (z.focus != null) ? z.focus : musicEventPosition;
                if (f == null) continue;

                float pz = ComputeProgressInZone(z, f.position, playerPos);
                if (pz > maxProgress) maxProgress = pz;
                if (maxProgress >= 1f) break; // early-out
            }
        }
        else
        {
            // Fallback OBB with global focus
            if (musicEventPosition != null)
            {
                Vector3 focus = musicEventPosition.position;
                Vector3 player = objectToCheck.transform.position;
                Vector2 fp = new Vector2(player.x - focus.x, player.z - focus.z);
                float dist2D = fp.magnitude;

                if (_fallbackBakedRadii == null || _fallbackBakedRadii.Length != Mathf.Max(1, sectorCount))
                {
                    maxProgress = 0f;
                }
                else if (dist2D < kEps)
                {
                    maxProgress = 1f;
                }
                else
                {
                    float ang = Mathf.Atan2(fp.y, fp.x); if (ang < 0f) ang += Mathf.PI * 2f;
                    float sectorAngle = (Mathf.PI * 2f) / sectorCount;
                    float fIndex = ang / sectorAngle;
                    int i0 = Mathf.FloorToInt(fIndex) % sectorCount;
                    int i1 = (i0 + 1) % sectorCount;
                    float t = fIndex - Mathf.Floor(fIndex);

                    float r0 = _fallbackBakedRadii[i0];
                    float r1 = _fallbackBakedRadii[i1];
                    float r = Mathf.Lerp(r0, r1, t) - bakeEdgePadding;
                    r = Mathf.Max(r, 0.001f);

                    maxProgress = Mathf.Clamp01(1f - (dist2D / r));
                }
            }
        }

        progress01 = maxProgress;

        // ---------------------------------------------
        // Temporal smoothing of proximity (progress01)
        // ---------------------------------------------
        float progA = 1f - Mathf.Exp(-progressSmoothing * Time.deltaTime);
        progressSmoothed = Mathf.Lerp(progressSmoothed, progress01, progA);

        // ---------------------------------------------
        // Musical phase & cycle timing
        // ---------------------------------------------
        int posMs = GetMusicPosMs();
        if (barTicked)
        {
            lastBarStartMs = posMs;
            barTicked = false;
            barCount++;
            if (debugBarLogs)
                Debug.Log($"[AKLD_HeartbeatModulator] BAR #{barCount} @ {posMs} ms");
        }
        if (!modulationActive) return;

        float secPerBeat = 60f / Mathf.Max(0.0001f, bpm);
        float secondsSinceBar = Mathf.Max(0f, (posMs - lastBarStartMs) / 1000f);

        float cycleSeconds = (syncMode == SyncMode.Frequency)
            ? (secPerBeat / Mathf.Max(0.0001f, frequency))       // 1 = one heartbeat drawing per beat
            : (secPerBeat / Mathf.Max(0.0001f, pulsesPerBeat));  // musical subdivision

        // Phase within current cycle [0..1]
        phaseX = Mathf.Repeat(secondsSinceBar / cycleSeconds, 1f);

        // ---------------------------------------------
        // Heartbeat curve value in [−0.5 .. +0.5]
        // ---------------------------------------------
        float curve = EvaluateCenteredShape(phaseX);

        // ---------------------------------------------
        // Output to RTPCs
        // value = initialValue + (curve * proximity * amplitude)
        // proximity = progressSmoothed (or use progress01 for raw)
        // ---------------------------------------------
        float proximity = progressSmoothed;
        foreach (var r in rtpcs)
        {
            if (r.rtpc == null) continue;

            float value = r.initialValue + (curve * proximity * r.amplitude);
            r.rtpc.SetGlobalValue(value);
            r.currentOutputValue = value;

            // NOTE: no clamp here. Add Mathf.Clamp01 if your RTPC is strictly [0..1].
        }
    }

    // ===============================
    // Per-zone progress (using that zone's baked radii)
    // ===============================
    float ComputeProgressInZone(ModulationZone z, Vector3 focus, Vector3 player)
    {
        if (z.bakedRadii == null || z.bakedRadii.Length != Mathf.Max(1, sectorCount))
            return 0f;

        Vector2 fp = new Vector2(player.x - focus.x, player.z - focus.z);
        float dist2D = fp.magnitude;

        if (dist2D < kEps) return 1f;

        float ang = Mathf.Atan2(fp.y, fp.x); if (ang < 0f) ang += Mathf.PI * 2f;
        float sectorAngle = (Mathf.PI * 2f) / sectorCount;
        float fIndex = ang / sectorAngle;
        int i0 = Mathf.FloorToInt(fIndex) % sectorCount;
        int i1 = (i0 + 1) % sectorCount;
        float t = fIndex - Mathf.Floor(fIndex);

        float r0 = z.bakedRadii[i0];
        float r1 = z.bakedRadii[i1];
        float r = Mathf.Lerp(r0, r1, t) - bakeEdgePadding;
        r = Mathf.Max(r, 0.001f);

        return Mathf.Clamp01(1f - (dist2D / r));
    }

    // ===============================
    // Bake control
    // ===============================
    void EnsureBake(bool forceIfEmpty)
    {
        // Zones present → bake per-zone
        if (HasAnyZones())
        {
            bool need = forceIfEmpty;

            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z == null || z.collider == null) continue;

                Transform f = (z.focus != null) ? z.focus : musicEventPosition;
                if (f == null) continue;

                if (z.bakedRadii == null || z.bakedRadii.Length != Mathf.Max(1, sectorCount))
                    need = true;

                if (!need)
                {
                    if ((f.position - z.lastBakeFocusPos).sqrMagnitude > 0.000001f) need = true;
                    Transform t = z.collider.transform;
                    if ((t.position - z.lastBakeAreaPos).sqrMagnitude > 0.000001f) need = true;
                    if (Quaternion.Angle(t.rotation, z.lastBakeAreaRot) > 0.01f) need = true;
                    if ((t.lossyScale - z.lastBakeAreaScale).sqrMagnitude > 0.000001f) need = true;
                }
            }

            if (!need) return;
            BakePizza_PerZone();
            return;
        }

        // No zones → fallback OBB
        bool needFallback =
            forceIfEmpty ||
            _fallbackBakedRadii == null ||
            _fallbackBakedRadii.Length != Mathf.Max(1, sectorCount) ||
            (transform.position - lastBakeFallbackPos).sqrMagnitude > 0.000001f ||
            Quaternion.Angle(transform.rotation, lastBakeFallbackRot) > 0.01f;

        if (!needFallback) return;
        BakePizza_FallbackOBB();
    }

    bool HasAnyZones()
    {
        if (zones == null) return false;
        for (int i = 0; i < zones.Count; i++)
            if (zones[i] != null && zones[i].collider != null) return true;
        return false;
    }

    // --- Per-zone bake ---
    void BakePizza_PerZone()
    {
        int N = Mathf.Max(1, sectorCount);
        float sectorAngle = (Mathf.PI * 2f) / N;

        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (z == null || z.collider == null) continue;

            if (z.bakedRadii == null || z.bakedRadii.Length != N)
                z.bakedRadii = new float[N];

            Transform f = (z.focus != null) ? z.focus : musicEventPosition;
            if (f == null) continue;

            Vector3 focus = f.position;

            for (int s = 0; s < N; s++)
            {
                float ang = s * sectorAngle;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)); // XZ
                z.bakedRadii[s] = FindBoundaryDistanceByBisectionZone(z.collider, focus, dir);
            }

            // snapshot last bake transforms
            z.lastBakeFocusPos = focus;
            Transform t = z.collider.transform;
            z.lastBakeAreaPos = t.position;
            z.lastBakeAreaRot = t.rotation;
            z.lastBakeAreaScale = t.lossyScale;
        }

#if UNITY_EDITOR
        Debug.Log($"[AKLD_HeartbeatModulator] Pizza baked per-zone (N={N}).");
#endif
    }

    // --- Fallback OBB bake (no zones) ---
    void BakePizza_FallbackOBB()
    {
        int N = Mathf.Max(1, sectorCount);
        if (_fallbackBakedRadii == null || _fallbackBakedRadii.Length != N)
            _fallbackBakedRadii = new float[N];

        Vector3 focus = musicEventPosition != null ? musicEventPosition.position : transform.position;
        float sectorAngle = (Mathf.PI * 2f) / N;

        for (int s = 0; s < N; s++)
        {
            float ang = s * sectorAngle;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)); // XZ
            _fallbackBakedRadii[s] = RayOBBDistanceXZ(focus, dir);
        }

        lastBakeFallbackPos = transform.position;
        lastBakeFallbackRot = transform.rotation;

#if UNITY_EDITOR
        Debug.Log($"[AKLD_HeartbeatModulator] Pizza baked (fallback OBB): N={N}.");
#endif
    }

    // ===============================
    // Distance queries
    // ===============================
    // Single-zone boundary via bisection
    float FindBoundaryDistanceByBisectionZone(Collider col, Vector3 origin, Vector3 dirXZ)
    {
        const int MAX_EXPAND = 32;
        const int MAX_BIN = 24;

        float step = Mathf.Max(1f, size.magnitude * 0.25f);
        float tLow = 0f;
        float tHigh = step;

        int i = 0;
        while (i++ < MAX_EXPAND)
        {
            Vector3 p = origin + dirXZ * tHigh;
            if (!IsInsideZone(col, p)) break;
            tHigh *= 2f;
        }

        if (IsInsideZone(col, origin + dirXZ * tHigh))
            return step;

        for (int b = 0; b < MAX_BIN; b++)
        {
            float mid = 0.5f * (tLow + tHigh);
            Vector3 p = origin + dirXZ * mid;
            if (IsInsideZone(col, p)) tLow = mid; else tHigh = mid;
        }

        return Mathf.Max(tHigh, 0.001f);
    }

    // Fallback OBB ray
    float RayOBBDistanceXZ(Vector3 origin, Vector3 dirXZ)
    {
        Matrix4x4 M = Matrix4x4.TRS(
            transform.position + transform.rotation * relativeCenter,
            transform.rotation * rotation,
            Vector3.one
        );
        Matrix4x4 invM = M.inverse;

        Vector3 oL = invM.MultiplyPoint3x4(origin);
        Vector3 dL = invM.MultiplyVector(new Vector3(dirXZ.x, 0f, dirXZ.z)).normalized;
        Vector3 half = size * 0.5f;

        float tmin = -Mathf.Infinity, tmax = Mathf.Infinity;

        // X slab
        if (Mathf.Abs(dL.x) < 1e-5f) { if (Mathf.Abs(oL.x) > half.x) return 0.001f; }
        else
        {
            float t1 = (-half.x - oL.x) / dL.x;
            float t2 = (half.x - oL.x) / dL.x;
            if (t1 > t2) { var tmp = t1; t1 = t2; t2 = tmp; }
            tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2);
        }
        // Z slab
        if (Mathf.Abs(dL.z) < 1e-5f) { if (Mathf.Abs(oL.z) > half.z) return 0.001f; }
        else
        {
            float t1 = (-half.z - oL.z) / dL.z;
            float t2 = (half.z - oL.z) / dL.z;
            if (t1 > t2) { var tmp = t1; t1 = t2; t2 = tmp; }
            tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2);
        }

        float tExit = tmax;
        if (tExit < 0f) tExit = 0.001f;
        return tExit;
    }

    // Inside checks
    bool IsInsideZone(Collider c, Vector3 pos)
    {
        if (c == null) return false;
        Vector3 cp = c.ClosestPoint(pos);
        return (cp - pos).sqrMagnitude <= (kEps * kEps);
    }

    // ===============================
    // Heartbeat curves (must return −0.5..+0.5)
    // ===============================
    float EvaluateCenteredShape(float x01)
    {
        switch (shape)
        {
            case HeartbeatShape.FastPulse: return FastPulseCentered(x01);
            case HeartbeatShape.CalmBeat: return CalmBeatCentered(x01);
            case HeartbeatShape.StressSpike: return StressSpikeCentered(x01);
            case HeartbeatShape.Custom:
            default:
                return Mathf.Clamp(customCurve.Evaluate(x01), -0.5f, 0.5f);
        }
    }

    // Asymmetric pulse (0..1), then centered to −0.5..+0.5
    private float FastPulseCentered(float x)
    {
        float u;
        if (x < 0.1f) u = Mathf.Lerp(0f, 1f, x / 0.1f);
        else if (x < 0.2f) u = Mathf.Lerp(1f, 0.3f, (x - 0.1f) / 0.1f);
        else u = Mathf.Lerp(0.3f, 0f, (x - 0.2f) / 0.8f);
        return Mathf.Clamp(u - 0.5f, -0.5f, 0.5f);
    }

    // Pure sine over a full cycle, scaled to −0.5..+0.5
    private float CalmBeatCentered(float x)
    {
        float s = Mathf.Sin(x * Mathf.PI * 2f);
        return Mathf.Clamp(0.5f * s, -0.5f, 0.5f);
    }

    // Narrow positive spike, centered to −0.5..+0.5
    private float StressSpikeCentered(float x)
    {
        float u = Mathf.Clamp01(Mathf.Exp(-15f * Mathf.Pow(x - 0.1f, 2f)) * 1.5f);
        return Mathf.Clamp(u - 0.5f, -0.5f, 0.5f);
    }

    // ===============================
    // Music timing helpers (Wwise)
    // ===============================
    int GetMusicPosMs()
    {
        if (playingId == AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            // Fallback to local time if event is not playing
            timeElapsed += Time.deltaTime;
            return Mathf.RoundToInt(timeElapsed * 1000f);
        }

        int ms;
        var res = AkSoundEngine.GetSourcePlayPosition(playingId, out ms, true);
        if (res != AKRESULT.AK_Success)
        {
            timeElapsed += Time.deltaTime;
            return Mathf.RoundToInt(timeElapsed * 1000f);
        }
        return ms;
    }

    private static void MusicCallback(object cookie, AkCallbackType type, AkCallbackInfo info)
    {
        if (cookie is not AKLD_HeartbeatModulator inst) return;
        if (type != AkCallbackType.AK_MusicSyncBar) return;

        inst.barTicked = true;

        if (inst.waitingForBar)
        {
            inst.waitingForBar = false;
            inst.modulationActive = true;
            inst.timeElapsed = 0f;
        }
    }

#if UNITY_EDITOR
    // ===============================
    // Gizmos & custom Inspector
    // ===============================
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Draw global focus (for timing & fallback)
        if (musicEventPosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(musicEventPosition.position, 0.06f);
        }

        // Draw fallback OBB if no zones
        if (!HasAnyZones())
        {
            Matrix4x4 areaM = Matrix4x4.TRS(
                transform.position + transform.rotation * relativeCenter,
                transform.rotation * rotation,
                Vector3.one
            );
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = areaM;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = prev;
        }

        // Draw per-zone baked radii (if any)
        if (HasAnyZones())
        {
            for (int zi = 0; zi < zones.Count; zi++)
            {
                var z = zones[zi];
                if (z == null || z.collider == null || z.bakedRadii == null) continue;
                Transform f = (z.focus != null) ? z.focus : musicEventPosition;
                if (f == null) continue;

                Vector3 c = f.position;
                float sectorAngle = (Mathf.PI * 2f) / z.bakedRadii.Length;

                for (int i = 0; i < z.bakedRadii.Length; i++)
                {
                    float ang = i * sectorAngle;
                    Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 end = c + dir * z.bakedRadii[i];

#if UNITY_EDITOR
                    Handles.color = new Color(0.7f, 0.9f, 0.2f, 0.9f);
                    Handles.DrawAAPolyLine(2.0f, c, end);
#endif
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(end, 0.03f);
                }
            }
        }

        // Debug label near player
        if (Application.isPlaying && objectToCheck)
        {
#if UNITY_EDITOR
            Vector3 player = objectToCheck.transform.position;
            Handles.color = Color.white;
            Handles.Label(player, $"proximity={progressSmoothed:F2}");
#endif
        }
    }

    // ====== Inspector (Bake & Visualizer) ======
    [CustomEditor(typeof(AKLD_HeartbeatModulator))]
    public class AKLD_HeartbeatModulatorEditor : Editor
    {
        static readonly Color ColBg = new Color(0.10f, 0.07f, 0.12f, 1f);
        static readonly Color ColFg = new Color(0.69f, 0.44f, 0.97f, 1f);
        static readonly Color ColFg2 = new Color(0.95f, 0.50f, 1.00f, 1f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var s = (AKLD_HeartbeatModulator)target;

            EditorGUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake Pizza (Top-Down XZ)"))
            {
                s.EnsureBake(true);
                EditorUtility.SetDirty(s);
            }
            if (GUILayout.Button("Clear Bake"))
            {
                // Clear zone & fallback caches
                if (s.zones != null)
                    foreach (var z in s.zones) if (z != null) z.bakedRadii = null;
                s._fallbackBakedRadii = null;
                EditorUtility.SetDirty(s);
            }
            GUILayout.EndHorizontal();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Visualizer", EditorStyles.boldLabel);

            // Global proximity bar
            DrawNiceBar("Proximity (Smoothed)", s.progressSmoothed, 0f, 1f, $"{s.progressSmoothed:F2}");

            // Per-RTPC bars: expected range with the current formula
            // initialValue ± (0.5 * proximity * amplitude)
            if (s.rtpcs != null && s.rtpcs.Count > 0)
            {
                EditorGUILayout.Space(4);
                foreach (var r in s.rtpcs)
                {
                    string name = r.rtpc != null ? r.rtpc.Name : "RTPC";

                    float span = 0.5f * s.progressSmoothed * r.amplitude;
                    float min = r.initialValue - span;
                    float max = r.initialValue + span;

                    float t = (max > min) ? Mathf.InverseLerp(min, max, r.currentOutputValue) : 0.5f;

                    DrawNiceBar(name, t, 0f, 1f, $"{r.currentOutputValue:F3}");
                }
            }
        }

        void DrawNiceBar(string label, float t, float min, float max, string overlay)
        {
            EditorGUILayout.LabelField(label);
            Rect r = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.DrawRect(r, ColBg);
            Rect r1 = new Rect(r.x, r.y, Mathf.Clamp01((t - min) / Mathf.Max(1e-5f, (max - min))) * r.width, r.height);
            EditorGUI.DrawRect(r1, ColFg);
            Rect r2 = new Rect(r.x, r.y, r1.width * 0.4f, r.height);
            EditorGUI.DrawRect(r2, ColFg2);
            Handles.color = new Color(1, 1, 1, 0.15f);
            Handles.DrawAAPolyLine(2f, new Vector3(r.x, r.yMax), new Vector3(r.xMax, r.yMax));
            var style = new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            EditorGUI.LabelField(r, overlay, style);
        }
    }
#endif
}


