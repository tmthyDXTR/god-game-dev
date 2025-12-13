using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Managers;

public class ResourceDebugger : EditorWindow
{
    ResourceManager rm;
    int editAmount = 1;
    Managers.ResourcePreset preset;
    bool setAsDefaultApplyInEdit = true;
    bool setAsDefaultApplyOnPlay = true;

    [MenuItem("Tools/Resource Debugger %r")]
    public static void OpenWindow()
    {
        var w = GetWindow<ResourceDebugger>("Resource Debugger");
        w.minSize = new Vector2(360, 160);
    }

    void OnGUI()
    {
        GUILayout.Label("Resource Manager Debugger", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        rm = (ResourceManager)EditorGUILayout.ObjectField("ResourceManager", rm, typeof(ResourceManager), true);
        if (rm == null)
        {
            if (GUILayout.Button("Find ResourceManager in Scene"))
            {
                rm = FindFirstObjectByType<ResourceManager>();
                if (rm == null) Debug.LogWarning("No ResourceManager found in the active scene.");
            }
            if (GUILayout.Button("Create ResourceManager"))
            {
                var go = new GameObject("ResourceManager");
                Undo.RegisterCreatedObjectUndo(go, "Create ResourceManager");
                rm = go.AddComponent<ResourceManager>();
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
            return;
        }

        EditorGUILayout.Space();
        editAmount = EditorGUILayout.IntField("Amount (for add/remove/set)", editAmount);
        preset = (Managers.ResourcePreset)EditorGUILayout.ObjectField("Preset (optional)", preset, typeof(Managers.ResourcePreset), false);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (preset != null)
        {
            EditorGUILayout.BeginHorizontal();
            setAsDefaultApplyInEdit = EditorGUILayout.ToggleLeft("Apply in Edit Mode", setAsDefaultApplyInEdit, GUILayout.Width(140));
            setAsDefaultApplyOnPlay = EditorGUILayout.ToggleLeft("Apply on Play Start", setAsDefaultApplyOnPlay, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Set Preset as ResourceManager Default"))
            {
                Undo.RecordObject(rm, "Set ResourceManager Default Preset");
                rm.defaultPreset = preset;
                rm.applyPresetInEditMode = setAsDefaultApplyInEdit;
                rm.applyPresetOnPlayStart = setAsDefaultApplyOnPlay;
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Seed From Preset (Overwrite)"))
            {
                if (EditorUtility.DisplayDialog("Seed Resources", "Overwrite global resources from preset? This will replace current values.", "OK", "Cancel"))
                {
                    Undo.RecordObject(rm, "Seed Resource Preset");
                    rm.ApplyPreset(preset);
                    EditorUtility.SetDirty(rm);
                    if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
                }
            }
            if (GUILayout.Button("Add From Preset"))
            {
                Undo.RecordObject(rm, "Add Resource Preset");
                rm.AddResource(ResourceManager.GameResource.Food, Mathf.Max(0, preset.food));
                rm.AddResource(ResourceManager.GameResource.Materials, Mathf.Max(0, preset.materials));
                rm.AddResource(ResourceManager.GameResource.Faith, Mathf.Max(0, preset.faith));
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Label("Global Resources", EditorStyles.boldLabel);

        foreach (ResourceManager.GameResource res in System.Enum.GetValues(typeof(ResourceManager.GameResource)))
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(res.ToString(), GUILayout.Width(100));
            int current = rm.GetAmount(res);
            EditorGUILayout.LabelField(current.ToString(), GUILayout.Width(60));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                Undo.RecordObject(rm, "Add Resource");
                rm.AddResource(res, Mathf.Max(0, editAmount));
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                Undo.RecordObject(rm, "Remove Resource");
                rm.TryRemoveResource(res, Mathf.Max(0, editAmount));
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                Undo.RecordObject(rm, "Set Resource");
                rm.SetResource(res, Mathf.Max(0, editAmount));
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                Undo.RecordObject(rm, "Clear Resource");
                rm.SetResource(res, 0);
                EditorUtility.SetDirty(rm);
                if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rm.gameObject.scene);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Display"))
        {
            Repaint();
        }
    }
}
