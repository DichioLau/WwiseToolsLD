/*
 * AKLD_DBMultipleGO.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
 * 
 * Description:
 * Multi-target distance to RTPC controller for Unity + Wwise.
 * - Tracks distance from a main object (A) to multiple targets.
 * - Each target has its own input range (Unity distance in world units).
 * - Selects one "owner" target using Nearest Sticky logic (hysteresis + dwell time + direction bias).
 * - Smooths transitions with crossfade and optional max change rate.
 * - Maps distance to RTPC value via per-target normalization + global remap curve.
 * - Sends the final smoothed value to a single Wwise RTPC.
 * 
 * Debug:
 * - Scene Gizmos draw lines and spheres with colors:
 *   - Red = target out of range
 *   - Green = target in range
 *   - Yellow = selected (owner) target
 * - Labels show raw distance, normalized t, mapped value, and input range.
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


[AddComponentMenu("AKLD/AKLD DB Multiple GO")]
public class AKLD_DBMultipleGO : MonoBehaviour
{
    // ----------------------
    // Serializable classes
    // ----------------------

    [System.Serializable]
    public class TargetEntry
    {
        public Transform target;

        [Header("Input (Unity distance for THIS target)")]
        [Tooltip("Minimum and maximum world distance for normalization of this target.")]
        public float inputMin = 0f;
        public float inputMax = 10f;
    }

    // ----------------------
    // Inspector fields
    // ----------------------

    [Header("Main Object (A) and Targets")]
    [Tooltip("The moving object whose distance to targets will be measured.")]
    public Transform objectA;

    [Tooltip("List of targets, each with its own input distance range.")]
    public List<TargetEntry> targets = new List<TargetEntry>();

    [Header("Axis to Measure")]
    public AxisDistance axisDistance = AxisDistance.All;
    public enum AxisDistance { X, Y, Z, All }

    [Header("Distance Behavior")]
    [Tooltip("NearIsMin: close = low value. NearIsMax: close = high value (inverse).")]
    public DistanceMode distanceMode = DistanceMode.NearIsMin;
    public enum DistanceMode { NearIsMin, NearIsMax }

    [Header("Remap (Distance -> RTPC)")]
    [Tooltip("Enable mapping from distance to RTPC range via curve.")]
    public bool enableRemap = true;
    public AnimationCurve remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Output RTPC range after remap.")]
    public float outputMin = 0f;
    public float outputMax = 100f;

    [Header("Selection (Nearest Sticky)")]
    [Range(0f, 0.5f)]
    [Tooltip("Percentage advantage required for a new target to take control.")]
    public float hysteresisPercent = 0.10f; // ~10%
    [Tooltip("Minimum time (s) a target must hold ownership before another can replace it.")]
    public float minDwellTime = 0.25f;
    [Tooltip("Crossfade time (s) between old and new owner values.")]
    public float crossfadeTime = 0.25f;

    [Header("Direction Bias")]
    [Tooltip("Bias factor favoring targets in the direction of motion (0 = disabled).")]
    public float directionBias = 0.5f;

    [Header("Output Smoothing")]
    [Tooltip("Maximum allowed change per second (0 = disabled).")]
    public float maxChangePerSecond = 0f;

    [Header("RTPC")]
    [SerializeField] private AK.Wwise.RTPC rtpc = null;

    [Header("Update Mode")]
    public UpdateMode updateMode = UpdateMode.EveryFrame;
    public enum UpdateMode { EveryFrame, FixedUpdate, IntervalSeconds }
    [Min(0.01f)]
    [Tooltip("Interval for UpdateMode.IntervalSeconds (seconds).")]
    public float updateIntervalSeconds = 0.05f;

    [Header("Gizmos (Scene View)")]
    public bool drawGizmos = true;
    public bool drawAxisProjection = true;
    public float gizmoSphereRadius = 0.06f;
    public Color colorOutOfRange = Color.red;
    public Color colorInRange = Color.green;
    public Color colorSelected = Color.yellow;

    [Header("Debug (Read-only)")]
    public bool EnableRTPCDebug = true; 
    [SerializeField] private int selectedIndex = -1;
    [SerializeField] private string selectedName = "";
    [SerializeField] private float currentDistance = 0f;       // Distance to current owner
    [SerializeField] private float currentRawValue = 0f;       // Value before smoothing
    [SerializeField] private float currentSmoothedValue = 0f;  // Value sent to Wwise

    // ----------------------
    // Private state
    // ----------------------

    private Vector3 lastAPos;
    private bool hasLastAPos = false;
    private float updateTimer = 0f;

    private float crossfadeElapsed = 0f;
    private bool inCrossfade = false;
    private float crossfadeStartValue = 0f;
    private float lastSwitchTime = -999f;

    // ----------------------
    // Unity lifecycle
    // ----------------------

    void Awake()
    {
        if (objectA != null)
        {
            lastAPos = objectA.position;
            hasLastAPos = true;
        }
        currentSmoothedValue = 0f;
        lastSwitchTime = Time.time;
    }

    void Update()
    {
        if (updateMode == UpdateMode.EveryFrame)
            Tick(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (updateMode == UpdateMode.FixedUpdate)
            Tick(Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        if (updateMode == UpdateMode.IntervalSeconds)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateIntervalSeconds)
            {
                Tick(updateTimer);
                updateTimer = 0f;
            }
        }
    }

    // ----------------------
    // Core logic
    // ----------------------

    private void Tick(float dt)
    {
        if (objectA == null || rtpc == null || targets == null || targets.Count == 0)
            return;

        Vector3 aPos = objectA.position;

        // Estimate A's velocity (for direction bias)
        Vector3 aVel = Vector3.zero;
        if (hasLastAPos) aVel = (aPos - lastAPos) / Mathf.Max(dt, 1e-6f);
        lastAPos = aPos; hasLastAPos = true;

        // Selection step: find best target by score (distance ± bias)
        int bestIdx = -1;
        float bestScore = float.PositiveInfinity;

        int count = targets.Count;
        float[] distances = new float[count];
        float[] values = new float[count];
        float[] scores = new float[count];

        for (int i = 0; i < count; i++)
        {
            var entry = targets[i];
            Transform t = entry.target;

            if (t == null)
            {
                distances[i] = float.PositiveInfinity;
                values[i] = 0f;
                scores[i] = float.PositiveInfinity;
                continue;
            }

            // Measure distance on chosen axis
            float d = MeasureDistance(aPos, t.position);
            distances[i] = d;

            // Normalize using THIS target's input range
            float tNorm = Mathf.InverseLerp(entry.inputMin, entry.inputMax, d);
            if (distanceMode == DistanceMode.NearIsMax) tNorm = 1f - tNorm;

            // Remap to RTPC range
            float v;
            if (enableRemap)
            {
                float shaped = (remapCurve != null) ? remapCurve.Evaluate(tNorm) : tNorm;
                v = Mathf.Lerp(outputMin, outputMax, Mathf.Clamp01(shaped));
            }
            else
            {
                v = d; // fallback to raw distance
            }
            values[i] = v;

            // Direction bias: favor targets towards which A is moving
            float bias = 0f;
            if (directionBias > 0f && aVel.sqrMagnitude > 1e-6f)
            {
                Vector3 toTarget = (t.position - aPos);
                if (axisDistance != AxisDistance.All)
                {
                    switch (axisDistance)
                    {
                        case AxisDistance.X: toTarget.y = toTarget.z = 0f; break;
                        case AxisDistance.Y: toTarget.x = toTarget.z = 0f; break;
                        case AxisDistance.Z: toTarget.x = toTarget.y = 0f; break;
                    }
                }
                float dot = Vector3.Dot(aVel.normalized, toTarget.normalized);
                if (dot > 0f) bias = directionBias * dot;
            }

            float score = d - bias;
            scores[i] = score;

            if (score < bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        if (bestIdx < 0) return;

        // Owner selection (Nearest Sticky with hysteresis + dwell)
        if (selectedIndex < 0)
        {
            // First assignment
            selectedIndex = bestIdx;
            selectedName = SafeName(targets[selectedIndex].target);
            lastSwitchTime = Time.time;
        }
        else if (bestIdx != selectedIndex)
        {
            float currScore = scores[selectedIndex];
            float newScore = scores[bestIdx];
            bool dwellOK = (Time.time - lastSwitchTime) >= minDwellTime;
            bool hysteresisOK = newScore < currScore * (1f - hysteresisPercent);

            if (dwellOK && hysteresisOK)
            {
                selectedIndex = bestIdx;
                selectedName = SafeName(targets[selectedIndex].target);
                lastSwitchTime = Time.time;

                // Start crossfade between old and new value
                inCrossfade = crossfadeTime > 0f;
                crossfadeElapsed = 0f;
                crossfadeStartValue = currentSmoothedValue;
            }
        }

        // Owner's raw value
        float ownerValue = values[selectedIndex];
        currentDistance = distances[selectedIndex];

        // Crossfade handling
        float desired;
        if (inCrossfade)
        {
            crossfadeElapsed += dt;
            float w = Mathf.Clamp01(crossfadeElapsed / Mathf.Max(1e-4f, crossfadeTime));
            desired = Mathf.Lerp(crossfadeStartValue, ownerValue, w);
            if (w >= 1f) inCrossfade = false;
        }
        else
        {
            desired = ownerValue;
        }
        currentRawValue = desired;

        // Final smoothing
        if (maxChangePerSecond > 0f)
        {
            float maxDelta = maxChangePerSecond * dt;
            currentSmoothedValue = Mathf.MoveTowards(currentSmoothedValue, desired, maxDelta);
        }
        else
        {
            currentSmoothedValue = desired;
        }

        // Send to Wwise

        if (EnableRTPCDebug) Debug.Log("RTPC -  " + currentSmoothedValue);

        rtpc.SetGlobalValue(currentSmoothedValue);
    }

    // ----------------------
    // Helpers
    // ----------------------

    private float MeasureDistance(Vector3 a, Vector3 b)
    {
        switch (axisDistance)
        {
            case AxisDistance.X: return Mathf.Abs(a.x - b.x);
            case AxisDistance.Y: return Mathf.Abs(a.y - b.y);
            case AxisDistance.Z: return Mathf.Abs(a.z - b.z);
            default: return Vector3.Distance(a, b);
        }
    }

    private string SafeName(Transform t) => t ? t.name : "(null)";

    // ----------------------
    // Gizmos (Scene Debug)
    // ----------------------

    void OnDrawGizmos()
    {
        if (!drawGizmos || objectA == null || targets == null) return;

        Vector3 aPos = objectA.position;

        for (int i = 0; i < targets.Count; i++)
        {
            var entry = targets[i];
            if (entry == null || entry.target == null) continue;

            Vector3 bPos = entry.target.position;
            float d = MeasureDistance(aPos, bPos);

            // Base color (range)
            Color c = (d > entry.inputMax) ? colorOutOfRange : colorInRange;
            // Override to yellow if this target is the current owner
            if (i == selectedIndex) c = colorSelected;

            // Draw line and sphere
            Gizmos.color = c;
            Gizmos.DrawLine(aPos, bPos);
            Gizmos.DrawSphere(bPos, gizmoSphereRadius);

            // Optional axis projection
            if (drawAxisProjection && axisDistance != AxisDistance.All)
            {
                Vector3 A = aPos, B = bPos;
                switch (axisDistance)
                {
                    case AxisDistance.X: A.y = B.y = (aPos.y + bPos.y) * 0.5f; A.z = B.z = (aPos.z + bPos.z) * 0.5f; break;
                    case AxisDistance.Y: A.x = B.x = (aPos.x + bPos.x) * 0.5f; A.z = B.z = (aPos.z + bPos.z) * 0.5f; break;
                    case AxisDistance.Z: A.x = B.x = (aPos.x + bPos.x) * 0.5f; A.y = B.y = (aPos.y + bPos.y) * 0.5f; break;
                }
                Color proj = c; proj.a = 0.5f;
                Gizmos.color = proj;
                Gizmos.DrawLine(A, B);
            }

#if UNITY_EDITOR
            // Inspector label
            float tNorm = Mathf.InverseLerp(entry.inputMin, entry.inputMax, d);
            if (distanceMode == DistanceMode.NearIsMax) tNorm = 1f - tNorm;
            float shaped = enableRemap && remapCurve != null ? remapCurve.Evaluate(tNorm) : tNorm;
            float v = enableRemap ? Mathf.Lerp(outputMin, outputMax, Mathf.Clamp01(shaped)) : d;

            string tag = (i == selectedIndex) ? " (OWNER)" : "";
            Vector3 labelPos = bPos + Vector3.up * 0.08f;
            Handles.Label(labelPos,
                $"{entry.target.name}{tag}\n d={d:F2} | t={tNorm:F2} | v={v:F2}\n[in:{entry.inputMin:F1}-{entry.inputMax:F1}]");
#endif
        }

        // Draw A
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(aPos, gizmoSphereRadius * 1.1f);
    }
}
