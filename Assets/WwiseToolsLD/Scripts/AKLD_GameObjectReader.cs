/*
 * AKLD_GameObjectReader.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Wwise + Unity helper
 *
 * What this does (in plain audio terms):
 * - Casts a ray from this GameObject in a chosen direction (Forward/Back/Left/Right/Up/Down).
 * - When the ray hits something, it checks a list of "checkers".
 * - Each checker can react either to a single scene instance OR to ALL instances of a prefab.
 * - On the first valid hit it posts a Wwise "Event On Enter". When the hit changes or is lost,
 *   it posts the matching "Event On Exit". (One checker fires once until you leave.)
 *
 * How to use it:
 * 1) Add this component to any GameObject (e.g., your player or a probe).
 * 2) Choose the Direction and Max Distance for the raycast.
 * 3) Add one or more Checkers:
 *    - Match Mode = InstanceOnly → drag a SCENE instance into "Instance Reference".
 *    - Match Mode = PrefabAllInstances → drag the PREFAB asset into "Prefab Reference".
 * 4) Assign your Wwise events: "Event On Enter" (start) and "Event On Exit" (stop).
 *
 * Visual debug:
 * - Toggle "Show Raycast" to always draw the ray (Scene view + Game view with Gizmos enabled).
 * - The ray is also drawn in OnDrawGizmos so you can see it while paused.
 *
 * Technical notes:
 * - When Match Mode is PrefabAllInstances, matching is done by a robust "base-name" compare:
 *   it strips "(Clone)" and numeric suffixes like " (1)", " (2)", etc., so all instances match.
 * - If "Camera Affects Direction" is enabled, Forward/Back/Left/Right use the camera orientation.
 * - OnEnter fires once per checker until you leave that target; OnExit fires on target change/leave.
 */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ray directions available for the reader.
/// </summary>
public enum Direction { Forward, Back, Up, Down, Left, Right }



[AddComponentMenu("AKLD/AKLD Game Object Reader")]
public class AKLD_GameObjectReader : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Inspector (general)
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Ray Settings")]
    [Tooltip("Direction in which the ray will be cast (relative to object or camera).")]
    public Direction selectedDirection = Direction.Forward;

    [Tooltip("Maximum distance of the raycast (units in world space).")]
    public float maxDistance = 10.0f;

    [Header("Checkers (matching + Wwise behavior)")]
    [Tooltip("List of GameObject checkers. Each defines what objects to react to and what Wwise events to post.")]
    public List<GameObjectChecker> checkers = new List<GameObjectChecker>();

    [Header("Debug")]
    [Tooltip("If true, logs the name of the hit object in the Console.")]
    public bool debugHitObjectName = false;

    [Tooltip("If true, draws a red debug line showing the ray direction in Scene/Game view.")]
    public bool showRaycast = true;

    [Header("Camera Influence")]
    [Tooltip("If true, ray directions Forward/Back/Left/Right are based on the camera orientation.")]
    public bool cameraAffectsDirection = false;

    [Tooltip("Optional reference to a camera. If null, defaults to Camera.main.")]
    public GameObject cameraObject;

    // ─────────────────────────────────────────────────────────────────────────────
    // Runtime cache / state
    // ─────────────────────────────────────────────────────────────────────────────
    private GameObject ownerGO;
    private Camera cam;
    private GameObject lastHitObject;
    private Vector3 lastRayDirection; // used by OnDrawGizmos

    // ─────────────────────────────────────────────────────────────────────────────
    // Unity Messages
    // ─────────────────────────────────────────────────────────────────────────────
    void Start()
    {
        ownerGO = this.gameObject;
        cam = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
    }

    void Update()
    {
        Vector3 origin = transform.position;
        Vector3 rayDirection = GetDirection();
        lastRayDirection = rayDirection;

        // Visual debug (runtime)
        if (showRaycast)
        {
            Debug.DrawLine(origin, origin + rayDirection * maxDistance, Color.red);
        }

        Ray ray = new Ray(origin, rayDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (debugHitObjectName)
                Debug.Log("GameObject Name: " + hitObject.name);

            // If the hit target changed, fire Exit on the previous one
            if (lastHitObject != null && lastHitObject != hitObject)
            {
                foreach (var checker in checkers)
                {
                    if (checker.IsValidGameObject(lastHitObject))
                    {
                        checker.PlayEventOnExit(ownerGO, lastHitObject);
                        checker.ResetEventSent();
                        break;
                    }
                }
            }

            // Fire Enter once for the first checker that matches
            foreach (var checker in checkers)
            {
                if (checker.IsValidGameObject(hitObject) && !checker.eventSent)
                {
                    checker.PlayEventOnEnter(ownerGO, hitObject);
                    checker.eventSent = true;
                    break;
                }
            }

            lastHitObject = hitObject;
        }
        else
        {
            // No hit → if we had a previous one, fire Exit
            if (lastHitObject != null)
            {
                foreach (var checker in checkers)
                {
                    if (checker.IsValidGameObject(lastHitObject))
                    {
                        checker.PlayEventOnExit(ownerGO, lastHitObject);
                        checker.ResetEventSent();
                        break;
                    }
                }

                lastHitObject = null;
            }
        }
    }

    void OnDisable()
    {
        // Clean stop if component is disabled while targeting something
        if (lastHitObject != null)
        {
            foreach (var checker in checkers)
            {
                if (checker.IsValidGameObject(lastHitObject))
                {
                    checker.PlayEventOnExit(ownerGO, lastHitObject);
                    checker.ResetEventSent();
                    break;
                }
            }
            lastHitObject = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────
    private Vector3 GetDirection()
    {
        switch (selectedDirection)
        {
            case Direction.Forward: return (cameraAffectsDirection && cam != null) ? cam.transform.forward : transform.forward;
            case Direction.Back: return (cameraAffectsDirection && cam != null) ? -cam.transform.forward : -transform.forward;
            case Direction.Up: return Vector3.up;
            case Direction.Down: return Vector3.down;
            case Direction.Left: return (cameraAffectsDirection && cam != null) ? -cam.transform.right : -transform.right;
            case Direction.Right: return (cameraAffectsDirection && cam != null) ? cam.transform.right : transform.right;
            default: return transform.forward;
        }
    }

    // Scene view debug (always visible if Show Raycast is on)
    void OnDrawGizmos()
    {
        if (!showRaycast) return;

        Gizmos.color = Color.red;
        Vector3 rayDirection = Application.isPlaying ? lastRayDirection : transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + rayDirection * maxDistance);
    }
}

[System.Serializable]
public class GameObjectChecker
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Matching mode
    // ─────────────────────────────────────────────────────────────────────────────
    public enum MatchMode
    {
        InstanceOnly,       // React to one specific SCENE instance (drag a scene object)
        PrefabAllInstances  // React to ALL instances of a PREFAB (drag the prefab asset)
    }

    [Tooltip("How this checker matches objects: either one specific instance or all instances of a prefab.")]
    public MatchMode matchMode = MatchMode.InstanceOnly;

    [Tooltip("Scene instance reference (used when Match Mode = InstanceOnly).")]
    public GameObject instanceReference;

    [Tooltip("Prefab asset reference (used when Match Mode = PrefabAllInstances).")]
    public GameObject prefabReference;

    // ─────────────────────────────────────────────────────────────────────────────
    // Wwise behavior
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Wwise Events")]
    [Tooltip("If true, the Wwise event will be posted on the OTHER GameObject hit instead of this component's GameObject.")]
    public bool postOnOther = false;

    [Tooltip("Wwise event to play when entering (ray starts hitting a valid object).")]
    public AK.Wwise.Event eventOnEnter;

    [Tooltip("Wwise event to play when exiting (ray stops hitting a valid object or changes target).")]
    public AK.Wwise.Event eventOnExit;

    [HideInInspector] public bool eventSent = false;

    // ─────────────────────────────────────────────────────────────────────────────
    // Matching logic
    // ─────────────────────────────────────────────────────────────────────────────
    public bool IsValidGameObject(GameObject go)
    {
        switch (matchMode)
        {
            case MatchMode.InstanceOnly:
                return instanceReference != null &&
                       instanceReference.scene.IsValid() &&
                       go == instanceReference;

            case MatchMode.PrefabAllInstances:
                if (prefabReference == null) return false;
                string prefabBase = GetBaseName(prefabReference.name);
                string hitBase = GetBaseName(go.name);
                return prefabBase.Equals(hitBase, System.StringComparison.Ordinal);

            default:
                return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Wwise triggers
    // ─────────────────────────────────────────────────────────────────────────────
    public void PlayEventOnEnter(GameObject owner, GameObject other)
    {
        if (eventOnEnter == null) return;
        if (postOnOther) eventOnEnter.Post(other);
        else eventOnEnter.Post(owner);
    }

    public void PlayEventOnExit(GameObject owner, GameObject other)
    {
        if (eventOnExit == null) return;
        if (postOnOther) eventOnExit.Post(other);
        else eventOnExit.Post(owner);
    }

    public void ResetEventSent() => eventSent = false;

    // ─────────────────────────────────────────────────────────────────────────────
    // Utility: strip "(Clone)" and numeric suffixes like " (1)", " (2)", etc.
    // ─────────────────────────────────────────────────────────────────────────────
    private static string GetBaseName(string name)
    {
        // Remove "(Clone)"
        string clean = name.Replace("(Clone)", "").Trim();

        // Remove " (number)" suffixes if present
        if (clean.EndsWith(")"))
        {
            int idx = clean.LastIndexOf('(');
            if (idx > 0)
            {
                string maybeNumber = clean.Substring(idx + 1, clean.Length - idx - 2);
                int n;
                if (int.TryParse(maybeNumber, out n))
                {
                    clean = clean.Substring(0, idx).Trim();
                }
            }
        }

        return clean;
    }
}
