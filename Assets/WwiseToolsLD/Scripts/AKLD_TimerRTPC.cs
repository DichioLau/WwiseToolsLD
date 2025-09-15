/*
 * AKLD_TimerRTPC.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
 *
 * What this does (in plain audio terms):
 * - You control ONE Wwise RTPC.
 * - You place several 3D “boxes” in the scene (they can be moved, rotated and scaled).
 * - When the listener/object enters a box, the RTPC starts changing over time.
 * - When it leaves that box, the RTPC moves toward a chosen target value.
 * - Only one box “has control” at a time: the first box you enter keeps control until you exit it.
 * - The RTPC is a single shared value, so there are NO jumps when switching boxes.
 *
 * Enter behavior (inside a box):
 * - Choose whether the value goes up (Sum) or down (Subtract).
 * - Choose the step size and the time between steps.
 * - Optional: stop the movement at a specific Enter Target value.
 *
 * Exit behavior (when leaving the box):
 * - Choose whether the value heads up or down.
 * - Set a target value to reach, a step size, and the time between steps.
 *
 * How to use it:
 * 1) Assign the Wwise RTPC you want to control.
 * 2) Set the global min/max and initial value.
 * 3) Add as many boxes as you need and tweak each one’s Enter/Exit settings.
 * 4) In the Scene view, grab each box’s transform gizmo to position/rotate/scale it.
 *
 * Visuals in Scene:
 * - Each box shows a clear colored outline.
 * - You can edit position/rotation/scale directly with the standard transform gizmo.
 * - Only the color is editable (outline is always opaque with fixed thickness).
 *
 * Technical notes:
 * - The RTPC can be applied Globally or to a specific GameObject (TargetMode).
 * - All values are clamped to your min/max.
 * - Coroutines stop cleanly when the component is disabled.
*/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;

public class AKLD_TimerRTPC : MonoBehaviour
{
    // ==============================
    // Shared configuration
    // ==============================

    [Header("Object To Track")]
    [Tooltip("Transform to test against all boxes (drives enter/exit).")]
    public Transform objectToCheck;

    public enum TargetMode { Global, ThisGameObject, ObjectToCheck, OtherGameObject }
    public enum OperationType { Sum, Subtract }

    [Header("RTPC (Shared)")]
    [Tooltip("Wwise RTPC to control (single shared RTPC for all boxes).")]
    public AK.Wwise.RTPC rTPC;

    [Tooltip("Where the RTPC is applied. Global by default.")]
    public TargetMode targetMode = TargetMode.Global;

    [Tooltip("Target GameObject when TargetMode = OtherGameObject.")]
    public GameObject otherObject;

    [Header("Range & Init (Shared)")]
    [Tooltip("Minimum allowed RTPC value.")]
    public float minValue = 0f;

    [Tooltip("Maximum allowed RTPC value.")]
    public float maxValue = 100f;

    [Tooltip("Initial value applied on Start.")]
    public float initialValue = 0f;

    [Tooltip("Single shared runtime value. All boxes read/write this to avoid jumps.")]
    public float value = 0f;

    [Header("Debug")]
    [Tooltip("Enable general debug logs.")]
    public bool debugOn = false;

    // ==============================
    // Per-box configuration
    // ==============================

    [System.Serializable]
    public class BoxItem
    {
        [Header("Box")]
        [Tooltip("Enable/disable this box item.")]
        public bool enabled = true;

        [Tooltip("Relative center (offset from component transform).")]
        public Vector3 relativeCenter = Vector3.zero;

        [Tooltip("Box size (width, height, depth).")]
        public Vector3 size = new Vector3(1f, 1f, 1f);

        [Tooltip("Box rotation in degrees (local to the component).")]
        public Vector3 rotationEuler = Vector3.zero;

        [Tooltip("If true, touching the borders counts as inside.")]
        public bool includeBorders = false;

        [Tooltip("Gizmo color for this box in the Scene view.")]
        public Color gizmoColor = Color.yellow;

        [Header("Enter Settings")]
        [Tooltip("Enable/disable the Enter behavior for this box.")]
        public bool enterEnabled = true;

        [Tooltip("Operation on Enter: Sum increases, Subtract decreases the value.")]
        public OperationType enterOperation = OperationType.Sum;

        [Tooltip("How much the value changes per step during Enter.")]
        [Min(0f)] public float enterIncrementAmount = 0f;

        [Tooltip("Seconds between Enter steps.")]
        [Min(0.0001f)] public float enterIncrementInterval = 1f;

        [Tooltip("Optional target for Enter. If enabled: Sum moves up to this value; Subtract moves down to this value.")]
        public bool enterUseTarget = false;

        [Tooltip("Enter target value (used only if 'enterUseTarget' is true).")]
        public float enterTargetValue = 0f;

        [Tooltip("Message logged when entering this box (for debugging).")]
        public string message = "Inside the area";

        [Header("Exit Settings")]
        [Tooltip("Enable/disable the Exit behavior for this box.")]
        public bool exitEnabled = true;

        [Tooltip("Operation on Exit: Sum moves upward to target; Subtract moves downward to target.")]
        public OperationType exitOperation = OperationType.Subtract;

        [Tooltip("Target value to reach on Exit. Sum increases to this value; Subtract decreases to this value.")]
        public float exitTargetValue = 0f;

        [Tooltip("How much the value changes per step during Exit (MoveTowards step).")]
        [Min(0f)] public float exitIncrementAmount = 0f;

        [Tooltip("Seconds between Exit steps.")]
        [Min(0.0001f)] public float exitIncrementInterval = 1f;

        // Runtime
        [System.NonSerialized] public bool wasInside = false;
        [System.NonSerialized] public Coroutine runningCoroutine = null;
    }

    [Header("Boxes")]
    [Tooltip("Add as many boxes as you need. Each one has its own Enter/Exit behavior.")]
    public List<BoxItem> boxes = new List<BoxItem>();

    // ==============================
    // Internal state
    // ==============================

    // First-come-wins owner; -1 when nobody owns the RTPC.
    private int ownerIndex = -1;

    // Minimum per-axis size to keep boxes visible and valid
    private const float kMinAxis = 0.05f;

    // ================
    // Helpers
    // ================

    static float Clamp(float v, float min, float max) => Mathf.Clamp(v, min, max);

    void ApplyRTPC(float v)
    {
        if (rTPC == null) return;

        if (targetMode == TargetMode.Global)
        {
            rTPC.SetGlobalValue(v);
            if (debugOn) Debug.Log($"[AKLD_TimerRTPC] (Global) {rTPC} = {v}");
            return;
        }

        GameObject target = gameObject;
        switch (targetMode)
        {
            case TargetMode.ObjectToCheck: target = objectToCheck ? objectToCheck.gameObject : gameObject; break;
            case TargetMode.OtherGameObject: target = otherObject ? otherObject : gameObject; break;
            default: target = gameObject; break;
        }

        // Hint: make sure the target has AkGameObj for per-object RTPCs.
        rTPC.SetValue(target, v);
        if (debugOn) Debug.Log($"[AKLD_TimerRTPC] {target.name} :: {rTPC} = {v}");
    }

    // OBB (oriented box) check: transform world point into the box-local frame
    bool IsInsideOBB(BoxItem b, Vector3 worldPos)
    {
        Vector3 c = transform.position + b.relativeCenter;
        Quaternion rot = Quaternion.Euler(b.rotationEuler);
        Vector3 local = Quaternion.Inverse(rot) * (worldPos - c); // to box local
        Vector3 half = new Vector3(
            Mathf.Max(kMinAxis, b.size.x) * 0.5f,
            Mathf.Max(kMinAxis, b.size.y) * 0.5f,
            Mathf.Max(kMinAxis, b.size.z) * 0.5f
        );

        if (b.includeBorders)
        {
            return (local.x >= -half.x && local.x <= half.x) &&
                   (local.y >= -half.y && local.y <= half.y) &&
                   (local.z >= -half.z && local.z <= half.z);
        }
        else
        {
            return (local.x > -half.x && local.x < half.x) &&
                   (local.y > -half.y && local.y < half.y) &&
                   (local.z > -half.z && local.z < half.z);
        }
    }

    void StopBoxCoroutine(BoxItem b)
    {
        if (b != null && b.runningCoroutine != null)
        {
            StopCoroutine(b.runningCoroutine);
            b.runningCoroutine = null;
        }
    }

    // ==================
    // Unity lifecycle
    // ==================

    private void Start()
    {
        value = Clamp(initialValue, minValue, maxValue);
        ApplyRTPC(value);
    }

    private void OnDisable()
    {
        for (int i = 0; i < boxes.Count; i++)
            StopBoxCoroutine(boxes[i]);
        ownerIndex = -1;
    }

    private void Update()
    {
        if (objectToCheck == null || boxes == null || boxes.Count == 0) return;

        Vector3 pos = objectToCheck.position;

        for (int i = 0; i < boxes.Count; i++)
        {
            var b = boxes[i];
            if (!b.enabled) continue;

            bool inside = IsInsideOBB(b, pos);

            // ENTER
            if (inside && !b.wasInside)
            {
                // first-come-wins
                if (ownerIndex == -1)
                {
                    ownerIndex = i;

                    // Clean start for this box
                    StopBoxCoroutine(b);

                    if (b.enterEnabled && b.enterIncrementAmount > 0f)
                    {
                        if (debugOn) Debug.Log(b.message);
                        b.runningCoroutine = StartCoroutine(RunEnter(b));
                    }

                    // Stop any other box coroutine so nobody else modifies the RTPC
                    for (int j = 0; j < boxes.Count; j++)
                    {
                        if (j == ownerIndex) continue;
                        StopBoxCoroutine(boxes[j]);
                    }
                }
                // else: someone already owns → ignore
            }

            // EXIT
            if (!inside && b.wasInside)
            {
                if (ownerIndex == i)
                {
                    StopBoxCoroutine(b); // stop Enter if running
                    if (b.exitEnabled && b.exitIncrementAmount > 0f)
                        b.runningCoroutine = StartCoroutine(RunExit(b));
                    ownerIndex = -1;
                }
                // non-owner exit is ignored
            }

            b.wasInside = inside;
        }
    }

    private IEnumerator RunEnter(BoxItem b)
    {
        // If no Enter target is used, original behavior: step until min/max.
        if (!b.enterUseTarget)
        {
            while (true)
            {
                yield return new WaitForSeconds(b.enterIncrementInterval);

                // If ownership was lost while waiting, stop early.
                if (ownerIndex != boxes.IndexOf(b)) { b.runningCoroutine = null; yield break; }

                float dir = (b.enterOperation == OperationType.Sum) ? +1f : -1f;
                value = Clamp(value + dir * b.enterIncrementAmount, minValue, maxValue);
                ApplyRTPC(value);

                if (Mathf.Approximately(value, minValue) || Mathf.Approximately(value, maxValue))
                    break;
            }
            b.runningCoroutine = null;
            yield break;
        }

        // With Enter target: move towards target and stop when reached.
        float target = Clamp(b.enterTargetValue, minValue, maxValue);

        bool wrong =
            (b.enterOperation == OperationType.Sum && target <= value) ||
            (b.enterOperation == OperationType.Subtract && target >= value);

        if (wrong)
        {
            if (debugOn) Debug.Log($"[AKLD_TimerRTPC] ENTER target invalid in box: op={b.enterOperation}, current={value}, target={target}. No movement.");
            b.runningCoroutine = null;
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(b.enterIncrementInterval);

            // If ownership was lost while waiting, stop early.
            if (ownerIndex != boxes.IndexOf(b)) { b.runningCoroutine = null; yield break; }

            value = Mathf.MoveTowards(value, target, b.enterIncrementAmount);
            value = Clamp(value, minValue, maxValue);
            ApplyRTPC(value);

            if (Mathf.Approximately(value, target) ||
                Mathf.Approximately(value, minValue) ||
                Mathf.Approximately(value, maxValue))
                break;
        }
        b.runningCoroutine = null;
    }

    private IEnumerator RunExit(BoxItem b)
    {
        float target = Clamp(b.exitTargetValue, minValue, maxValue);

        bool wrong =
            (b.exitOperation == OperationType.Sum && target <= value) ||
            (b.exitOperation == OperationType.Subtract && target >= value);

        if (wrong)
        {
            if (debugOn) Debug.Log($"[AKLD_TimerRTPC] EXIT invalid: op={b.exitOperation}, current={value}, target={target}. No movement.");
            b.runningCoroutine = null;
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(b.exitIncrementInterval);

            value = Mathf.MoveTowards(value, target, b.exitIncrementAmount);
            value = Clamp(value, minValue, maxValue);
            ApplyRTPC(value);

            if (Mathf.Approximately(value, target) ||
                Mathf.Approximately(value, minValue) ||
                Mathf.Approximately(value, maxValue))
                break;
        }
        b.runningCoroutine = null;
    }

    private void OnValidate()
    {
        if (maxValue < minValue) (minValue, maxValue) = (maxValue, minValue);
        initialValue = Clamp(initialValue, minValue, maxValue);
        value = Clamp(value, minValue, maxValue);

        foreach (var b in boxes)
        {
            if (b == null) continue;
            b.size.x = Mathf.Max(kMinAxis, b.size.x);
            b.size.y = Mathf.Max(kMinAxis, b.size.y);
            b.size.z = Mathf.Max(kMinAxis, b.size.z);
        }
    }

    // ==================
    // Gizmos (always visible)
    // ==================
#if UNITY_EDITOR
    private void OnDrawGizmos() { DrawBoxesGizmos(); }
    private void OnDrawGizmosSelected() { DrawBoxesGizmos(); }

    private void DrawBoxesGizmos()
    {
        if (boxes == null) return;

        foreach (var b in boxes)
        {
            if (!b.enabled) continue;

            Vector3 c = transform.position + b.relativeCenter;
            Quaternion r = Quaternion.Euler(b.rotationEuler);
            Vector3 s = new Vector3(
                Mathf.Max(kMinAxis, b.size.x),
                Mathf.Max(kMinAxis, b.size.y),
                Mathf.Max(kMinAxis, b.size.z)
            );

            // Compute corners of the oriented cube
            Vector3 h = 0.5f * s;
            Vector3[] v = new Vector3[8];
            v[0] = c + r * new Vector3(-h.x, -h.y, -h.z);
            v[1] = c + r * new Vector3(+h.x, -h.y, -h.z);
            v[2] = c + r * new Vector3(-h.x, +h.y, -h.z);
            v[3] = c + r * new Vector3(+h.x, +h.y, -h.z);
            v[4] = c + r * new Vector3(-h.x, -h.y, +h.z);
            v[5] = c + r * new Vector3(+h.x, -h.y, +h.z);
            v[6] = c + r * new Vector3(-h.x, +h.y, +h.z);
            v[7] = c + r * new Vector3(+h.x, +h.y, +h.z);

            // 12 edges
            int[,] e = new int[,]
            {
                {0,1},{1,3},{3,2},{2,0},
                {4,5},{5,7},{7,6},{6,4},
                {0,4},{1,5},{2,6},{3,7}
            };

            // Thick anti-aliased outline (alpha = 1, thickness = 2)
            var lineCol = b.gizmoColor; lineCol.a = 1f;
            Handles.color = lineCol;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            for (int i = 0; i < 12; i++)
                Handles.DrawAAPolyLine(2f, v[e[i, 0]], v[e[i, 1]]);
        }
    }
#endif
}

#if UNITY_EDITOR
// ========================= Custom Editor (handles for ALL boxes) ===========================
// Move, rotate and scale every enabled box directly in Scene view (no extra UI).
[CustomEditor(typeof(AKLD_TimerRTPC))]
public class AKLD_TimerRTPC_Editor : Editor
{
    const float kMinHalf = 0.025f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // keep the default inspector clean
    }

    private void OnSceneGUI()
    {
        var comp = (AKLD_TimerRTPC)target;
        if (comp.boxes == null || comp.boxes.Count == 0) return;

        for (int i = 0; i < comp.boxes.Count; i++)
        {
            var b = comp.boxes[i];
            if (!b.enabled) continue;

            // Color for handles
            var hc = b.gizmoColor; hc.a = 1f;
            Handles.color = hc;

            Vector3 worldCenter = comp.transform.position + b.relativeCenter;
            Quaternion worldRot = Quaternion.Euler(b.rotationEuler);

            // ---- Move ----
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.PositionHandle(worldCenter, worldRot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(comp, $"Move Box {i} Center");
                b.relativeCenter = newCenter - comp.transform.position;
                EditorUtility.SetDirty(comp);
            }

            // ---- Rotate ----
            EditorGUI.BeginChangeCheck();
            Quaternion newRot = Handles.RotationHandle(worldRot, worldCenter);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(comp, $"Rotate Box {i}");
                Vector3 eul = (Quaternion.Inverse(Quaternion.identity) * newRot).eulerAngles;
                b.rotationEuler = eul;
                EditorUtility.SetDirty(comp);
            }

            // ---- Scale (aligned to the box rotation) ----
            EditorGUI.BeginChangeCheck();
            Vector3 half = new Vector3(
                Mathf.Max(kMinHalf, b.size.x * 0.5f),
                Mathf.Max(kMinHalf, b.size.y * 0.5f),
                Mathf.Max(kMinHalf, b.size.z * 0.5f)
            );

            Vector3 newHalf = Handles.ScaleHandle(
                half,
                worldCenter,
                newRot, // oriented scale gizmo
                HandleUtility.GetHandleSize(worldCenter)
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(comp, $"Resize Box {i}");
                newHalf.x = Mathf.Max(kMinHalf, newHalf.x);
                newHalf.y = Mathf.Max(kMinHalf, newHalf.y);
                newHalf.z = Mathf.Max(kMinHalf, newHalf.z);
                b.size = newHalf * 2f;
                EditorUtility.SetDirty(comp);
            }
        }
    }
}
#endif
