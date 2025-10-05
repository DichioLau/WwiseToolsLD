/*
 * AKLD_DistanceBetweenObjects.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
 * 
 * Description:
 * Calculates the distance between two objects (X, Y, Z, or full 3D).
 * Maps the measured distance into a Wwise RTPC value with optional remap curve.
 * Supports two distance modes (NearIsMin / NearIsMax).
 * Includes gizmos for debug visualization in the Scene view.
 * 
 * Debug:
 * - Cyan line + spheres between the two objects.
 * - Optional axis-projected helper line.
 * - Distance label rendered in Scene view.
 */

using UnityEngine;

[AddComponentMenu("AKLD/AKLD DB GO")]
public class AKLD_DistanceBetweenObjects : MonoBehaviour
{
    // ----------------------
    // Inspector fields
    // ----------------------

    [Header("Distance between Objects to RTPC")]
    [Tooltip("First object to measure distance from.")]
    public Transform object1;
    [Tooltip("Second object to measure distance to.")]
    public Transform object2;

    [Space(6)]
    [Header("Choose Axis to Calculate Distance")]
    public AxisDistance axisDistance = AxisDistance.All;
    public enum AxisDistance { X, Y, Z, All }

    [Space(6)]
    [Header("Runtime (Read-only)")]
    [Tooltip("Last computed distance (Unity world units).")]
    public float distance;

    [Header("RTPC")]
    [SerializeField]
    [Tooltip("The Wwise RTPC that will be updated with the mapped distance value.")]
    private AK.Wwise.RTPC rtpc = null;

    [Space(6)]
    [Header("Remap (Distance -> RTPC)")]
    [Tooltip("Enable mapping from Unity distance into an RTPC range via curve.")]
    public bool enableRemap = true;
    [Tooltip("Minimum Unity distance for normalization.")]
    public float inputMin = 0f;
    [Tooltip("Maximum Unity distance for normalization.")]
    public float inputMax = 10f;
    [Tooltip("Curve to shape normalized distance (0..1) before mapping to output range.")]
    public AnimationCurve remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("Minimum RTPC value to send to Wwise.")]
    public float outputMin = 0f;
    [Tooltip("Maximum RTPC value to send to Wwise.")]
    public float outputMax = 100f;

    [Tooltip("Defines how RTPC reacts to distance.")]
    public DistanceMode distanceMode = DistanceMode.NearIsMin;
    public enum DistanceMode
    {
        NearIsMin, // close -> low RTPC value
        NearIsMax  // close -> high RTPC value (inverse)
    }

    [Space(6)]
    [Header("Gizmos (Scene View)")]
    [Tooltip("Enable/disable debug gizmos in Scene view.")]
    public bool drawGizmos = true;
    [Tooltip("Base gizmo color (cyan by default).")]
    public Color gizmoColor = Color.cyan;
    [Tooltip("Draw axis-projected helper line (useful if measuring only one axis).")]
    public bool drawAxisProjection = true;
    [Tooltip("Radius of spheres drawn at object positions.")]
    public float gizmoSphereRadius = 0.05f;

    // ----------------------
    // Unity lifecycle
    // ----------------------

    void Update()
    {
        if (object1 == null || object2 == null || rtpc == null)
            return;

        // Compute raw distance based on axis selection
        switch (axisDistance)
        {
            case AxisDistance.X: distance = Mathf.Abs(object1.position.x - object2.position.x); break;
            case AxisDistance.Y: distance = Mathf.Abs(object1.position.y - object2.position.y); break;
            case AxisDistance.Z: distance = Mathf.Abs(object1.position.z - object2.position.z); break;
            default: distance = Vector3.Distance(object1.position, object2.position); break;
        }

        float valueToSend = distance;

        if (enableRemap)
        {
            // Normalize distance into 0..1
            float t = Mathf.InverseLerp(inputMin, inputMax, distance);

            // Invert if NearIsMax mode
            if (distanceMode == DistanceMode.NearIsMax)
                t = 1f - t;

            // Apply curve and map to output range
            float shaped = remapCurve != null ? remapCurve.Evaluate(t) : t;
            valueToSend = Mathf.Lerp(outputMin, outputMax, Mathf.Clamp01(shaped));
        }

        // Send final value to Wwise RTPC
        rtpc.SetGlobalValue(valueToSend);
    }

    // ----------------------
    // Gizmos (Scene Debug)
    // ----------------------

    void OnDrawGizmos()
    {
        if (!drawGizmos || object1 == null || object2 == null)
            return;

        Vector3 p1 = object1.position;
        Vector3 p2 = object2.position;

        // Draw line and spheres
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawSphere(p1, gizmoSphereRadius);
        Gizmos.DrawSphere(p2, gizmoSphereRadius);

        // Optional axis projection line
        if (drawAxisProjection && axisDistance != AxisDistance.All)
        {
            Vector3 a = p1;
            Vector3 b = p2;

            switch (axisDistance)
            {
                case AxisDistance.X: a.y = b.y = (p1.y + p2.y) * 0.5f; a.z = b.z = (p1.z + p2.z) * 0.5f; break;
                case AxisDistance.Y: a.x = b.x = (p1.x + p2.x) * 0.5f; a.z = b.z = (p1.z + p2.z) * 0.5f; break;
                case AxisDistance.Z: a.x = b.x = (p1.x + p2.x) * 0.5f; a.y = b.y = (p1.y + p2.y) * 0.5f; break;
            }

            Color proj = gizmoColor; proj.a = 0.5f;
            Gizmos.color = proj;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, gizmoSphereRadius * 0.8f);
            Gizmos.DrawSphere(b, gizmoSphereRadius * 0.8f);
        }

#if UNITY_EDITOR
        // Draw label with distance
        float d;
        switch (axisDistance)
        {
            case AxisDistance.X: d = Mathf.Abs(p1.x - p2.x); break;
            case AxisDistance.Y: d = Mathf.Abs(p1.y - p2.y); break;
            case AxisDistance.Z: d = Mathf.Abs(p1.z - p2.z); break;
            default: d = Vector3.Distance(p1, p2); break;
        }
        var mid = (p1 + p2) * 0.5f + Vector3.up * 0.05f;
        UnityEditor.Handles.Label(mid, $"dist: {d:F3}");
#endif
    }
}
