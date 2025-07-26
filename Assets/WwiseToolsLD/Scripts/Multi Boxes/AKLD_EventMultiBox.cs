/*
 * AKLD_EventMultiBox.cs
 * 
 * Author: Lautaro Dichio
 * Description:
 * This script defines and manages multiple 3D box areas in Unity.
 * It triggers Wwise events when a specified GameObject enters or exits each area.
 * Also includes full editor integration for in-scene editing of position, rotation, and size.
 * 
 * Designed for technical sound designers working with Unity + Wwise.
 * Supports tagging and visualization for better organization and workflow.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class AKLD_EventMultiBox : MonoBehaviour
{
    // The object we want to check if it's inside any area
    public GameObject objectToCheck;

    // Optional audio tag used to group and filter which boxes to display
    public string audioTag = "";

    // If true, show all boxes from all instances regardless of tag
    public bool showAllBoxes = false;

    // Gizmo toggles (hidden in the default Inspector, shown via custom editor)
    [HideInInspector] public bool showPositionGizmo = true;
    [HideInInspector] public bool showRotationGizmo = true;
    [HideInInspector] public bool showSizeGizmo = true;

    // Class that defines each box area
    [System.Serializable]
    public class AreaData
    {
        // Basic box settings
        [Header("Box Settings")]
        public Vector3 relativeCenter = Vector3.zero;               // Center relative to this GameObject
        public Vector3 size = new Vector3(1f, 1f, 1f);              // Size of the box
        public Color gizmoColor = Color.yellow;                     // Color of the box gizmo
        public Quaternion rotation = Quaternion.identity;           // Rotation of the box

        // Wwise event settings for entering the box
        [Header("Wwise Event On Enter")]
        public AK.Wwise.Event enterEvent = null;                    // Event to trigger when entering
        public bool DebugOn = false;                                // If true, logs a message on entry
        public string message = "Inside the box";                   // Message to log on entry

        // Wwise event settings for exiting the box
        [Header("Wwise Event On Exit")]
        public bool stopOnExit = true;                              // If true, stops the entry event on exit
        public bool OnExitEnabled = false;                          // If true, allows a separate exit event
        public AK.Wwise.Event eventOnExit = null;                   // Event to trigger on exit
        public bool DebugOnExit = false;                            // If true, logs a message on exit
        public string messageOnExit = "Outside the box";            // Message to log on exit

        [HideInInspector] public bool wasInside = false;            // Tracks whether the object was previously inside
    }

    // List of defined areas (can be edited in the Inspector)
    public List<AreaData> areas = new List<AreaData>() { new AreaData() };

    // Called every frame
    private void Update()
    {
        // Only run during play mode and if we have a valid object to check
        if (!Application.isPlaying || objectToCheck == null) return;

        // Check each defined area
        foreach (var area in areas)
        {
            // Determine if the object is currently inside this area
            bool isInside = IsInsideArea(objectToCheck, area);

            // If the object just entered
            if (isInside && !area.wasInside)
            {
                // Trigger the entry event
                if (area.enterEvent != null)
                    area.enterEvent.Post(this.gameObject);

                // Print debug message if enabled
                if (area.DebugOn)
                    Debug.Log(area.message);
            }
            // If the object just exited
            else if (!isInside && area.wasInside)
            {
                // Stop the entry event if required
                if (area.stopOnExit && area.enterEvent != null)
                    area.enterEvent.Stop(this.gameObject);

                // Trigger exit event if enabled
                if (area.OnExitEnabled && area.eventOnExit != null)
                    area.eventOnExit.Post(this.gameObject);

                // Print debug message if enabled
                if (area.DebugOnExit)
                    Debug.Log(area.messageOnExit);
            }

            // Save current state for next frame
            area.wasInside = isInside;
        }
    }

    // Checks if the object is within the bounds of the area (using OverlapBox)
    private bool IsInsideArea(GameObject objectToCheck, AreaData area)
    {
        Vector3 worldCenter = transform.position + area.relativeCenter;

        Collider[] overlaps = Physics.OverlapBox(
            worldCenter,
            area.size * 0.5f,
            area.rotation
        );

        foreach (var col in overlaps)
        {
            if (col.gameObject == objectToCheck)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    // Custom editor to enhance Inspector and Scene view functionality
    [CustomEditor(typeof(AKLD_EventMultiBox))]
    public class AKLD_EventMultiBoxEditor : Editor
    {
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AKLD_EventMultiBox manager = (AKLD_EventMultiBox)target;

            // Display script reference (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((AKLD_EventMultiBox)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            // Draw all properties except those we want to handle manually
            DrawPropertiesExcluding(serializedObject, "m_Script", "audioTag", "showAllBoxes");

            // Gizmo toggles
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
            manager.showPositionGizmo = EditorGUILayout.Toggle("Show Position Gizmo", manager.showPositionGizmo);
            manager.showRotationGizmo = EditorGUILayout.Toggle("Show Rotation Gizmo", manager.showRotationGizmo);
            manager.showSizeGizmo = EditorGUILayout.Toggle("Show Size Gizmo", manager.showSizeGizmo);

            serializedObject.ApplyModifiedProperties();

            // Audio tag filter and global display toggle
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmo Display Settings", EditorStyles.boldLabel);
            manager.audioTag = EditorGUILayout.TextField("Audio Tag", manager.audioTag);
            manager.showAllBoxes = EditorGUILayout.Toggle("Show All Boxes", manager.showAllBoxes);
        }

        // Called when scene view is active; used to draw gizmos in scene
        private void OnSceneGUI()
        {
            var selected = (AKLD_EventMultiBox)target;
            var all = Object.FindObjectsOfType<AKLD_EventMultiBox>();
            string referenceTag = selected.audioTag;

            // Draw all boxes from all instances
            if (selected.showAllBoxes)
            {
                foreach (var instance in all)
                {
                    DrawGizmos(instance);
                }
            }
            // If tag is empty, only draw boxes from this object
            else if (string.IsNullOrEmpty(referenceTag))
            {
                DrawGizmos(selected);
            }
            // Otherwise, draw only boxes that share the same tag
            else
            {
                foreach (var instance in all)
                {
                    if (instance.audioTag == referenceTag)
                    {
                        DrawGizmos(instance);
                    }
                }
            }
        }

        // Draws visual gizmos and handles in the Scene view for editing
        private void DrawGizmos(AKLD_EventMultiBox manager)
        {
            foreach (var area in manager.areas)
            {
                Vector3 center = manager.transform.position + area.relativeCenter;
                Handles.color = area.gizmoColor;

                // Apply transformation for rotation and scale
                Matrix4x4 matrix = Matrix4x4.TRS(center, area.rotation, Vector3.one);
                using (new Handles.DrawingScope(matrix))
                {
                    Handles.DrawWireCube(Vector3.zero, area.size);
                }

                // Position handle (move box)
                if (manager.showPositionGizmo)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(center, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Move Area");
                        area.relativeCenter += newPos - center;
                    }
                }

                // Rotation handle (rotate box)
                if (manager.showRotationGizmo)
                {
                    EditorGUI.BeginChangeCheck();
                    Quaternion newRot = Handles.RotationHandle(area.rotation, center);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Rotate Area");
                        area.rotation = newRot;
                    }
                }

                // Scale handle (resize box)
                if (manager.showSizeGizmo)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newSize = Handles.ScaleHandle(area.size, center, area.rotation, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Resize Area");
                        area.size = newSize;
                    }
                }
            }
        }
    }
#endif
}
