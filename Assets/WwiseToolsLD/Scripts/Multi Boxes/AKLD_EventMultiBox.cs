using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class AKLD_EventMultiBox : MonoBehaviour
{
    public GameObject objectToCheck;

    public string audioTag = "";
    public bool showAllBoxes = false;

    [HideInInspector] public bool showPositionGizmo = true;
    [HideInInspector] public bool showRotationGizmo = true;
    [HideInInspector] public bool showSizeGizmo = true;

    [System.Serializable]
    public class AreaData
    {
        [Header("Box Settings")]
        public Vector3 relativeCenter = Vector3.zero;
        public Vector3 size = new Vector3(1f, 1f, 1f);
        public Color gizmoColor = Color.yellow;
        public Quaternion rotation = Quaternion.identity;

        [Header("Wwise Event On Enter")]
        public AK.Wwise.Event enterEvent = null;
        public bool DebugOn = false;
        public string message = "Inside the box";

        [Header("Wwise Event On Exit")]
        public bool stopOnExit = true;
        public bool OnExitEnabled = false;
        public AK.Wwise.Event eventOnExit = null;
        public bool DebugOnExit = false;
        public string messageOnExit = "Outside the box";

        [HideInInspector] public bool wasInside = false;
    }

    public List<AreaData> areas = new List<AreaData>() { new AreaData() };

    private void Update()
    {
        if (!Application.isPlaying || objectToCheck == null) return;

        foreach (var area in areas)
        {
            bool isInside = IsInsideArea(objectToCheck, area);

            if (isInside && !area.wasInside)
            {
                if (area.enterEvent != null)
                    area.enterEvent.Post(this.gameObject);

                if (area.DebugOn)
                    Debug.Log(area.message);
            }
            else if (!isInside && area.wasInside)
            {
                if (area.stopOnExit && area.enterEvent != null)
                    area.enterEvent.Stop(this.gameObject);

                if (area.OnExitEnabled && area.eventOnExit != null)
                    area.eventOnExit.Post(this.gameObject);

                if (area.DebugOnExit)
                    Debug.Log(area.messageOnExit);
            }

            area.wasInside = isInside;
        }
    }

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
    [CustomEditor(typeof(AKLD_EventMultiBox))]
    public class AKLD_EventMultiBoxEditor : Editor
    {
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AKLD_EventMultiBox manager = (AKLD_EventMultiBox)target;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((AKLD_EventMultiBox)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            DrawPropertiesExcluding(serializedObject, "m_Script", "audioTag", "showAllBoxes");


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
            manager.showPositionGizmo = EditorGUILayout.Toggle("Show Position Gizmo", manager.showPositionGizmo);
            manager.showRotationGizmo = EditorGUILayout.Toggle("Show Rotation Gizmo", manager.showRotationGizmo);
            manager.showSizeGizmo = EditorGUILayout.Toggle("Show Size Gizmo", manager.showSizeGizmo);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gizmo Display Settings", EditorStyles.boldLabel);
            manager.audioTag = EditorGUILayout.TextField("Audio Tag", manager.audioTag);
            manager.showAllBoxes = EditorGUILayout.Toggle("Show All Boxes", manager.showAllBoxes);
        }

        private void OnSceneGUI()
        {
            var selected = (AKLD_EventMultiBox)target;
            var all = Object.FindObjectsOfType<AKLD_EventMultiBox>();
            string referenceTag = selected.audioTag;

            if (selected.showAllBoxes)
            {
                foreach (var instance in all)
                {
                    DrawGizmos(instance);
                }
            }
            else if (string.IsNullOrEmpty(referenceTag))
            {
                DrawGizmos(selected);
            }
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

        private void DrawGizmos(AKLD_EventMultiBox manager)
        {
            foreach (var area in manager.areas)
            {
                Vector3 center = manager.transform.position + area.relativeCenter;
                Handles.color = area.gizmoColor;

                Matrix4x4 matrix = Matrix4x4.TRS(center, area.rotation, Vector3.one);
                using (new Handles.DrawingScope(matrix))
                {
                    Handles.DrawWireCube(Vector3.zero, area.size);
                }

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
