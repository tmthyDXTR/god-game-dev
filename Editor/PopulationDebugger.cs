using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HexGrid;

public class PopulationDebugger : EditorWindow
{
    HexGridGenerator generator;
    HexTile selectedTile;

    int q = 0, r = 0;
    int addAmount = 1;
    int setValue = 0;

    [MenuItem("Tools/Population Debugger %p")]
    public static void OpenWindow()
    {
        var w = GetWindow<PopulationDebugger>("Population Debugger");
        w.minSize = new Vector2(320, 140);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Population Debugger", EditorStyles.boldLabel);

        generator = (HexGridGenerator)EditorGUILayout.ObjectField("Grid Generator", generator, typeof(HexGridGenerator), true);
        if (generator == null)
        {
            if (GUILayout.Button("Find HexGridGenerator in Scene"))
            {
                generator = FindFirstObjectByType<HexGridGenerator>();
                if (generator == null) Debug.LogWarning("No HexGridGenerator found in the active scene.");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select Tile", EditorStyles.boldLabel);

        selectedTile = (HexTile)EditorGUILayout.ObjectField("Selected Tile", selectedTile, typeof(HexTile), true);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Scene Selection"))
        {
            var go = Selection.activeGameObject;
            if (go != null)
            {
                var t = go.GetComponent<HexTile>();
                if (t != null)
                    selectedTile = t;
                else
                    Debug.LogWarning("Selected GameObject has no HexTile component.");
            }
        }
        if (GUILayout.Button("Clear Selection"))
        {
            selectedTile = null;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Find By Coordinates", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        q = EditorGUILayout.IntField("q", q);
        r = EditorGUILayout.IntField("r", r);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Find Tile At q,r"))
        {
            if (generator == null)
            {
                Debug.LogWarning("No HexGridGenerator assigned.");
            }
            else
            {
                var hex = new Hex(q, r);
                if (generator.tiles.TryGetValue(hex, out var tile))
                {
                    selectedTile = tile;
                }
                else
                {
                    Debug.LogWarning($"No tile found at {hex.q},{hex.r},{hex.s}");
                }
            }
        }

        EditorGUILayout.Space();
        if (selectedTile == null)
        {
            EditorGUILayout.HelpBox("No tile selected. Use scene selection or find by coordinates.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Tile: {selectedTile.name}  Hex: {selectedTile.HexCoordinates.q},{selectedTile.HexCoordinates.r},{selectedTile.HexCoordinates.s}");
        EditorGUILayout.LabelField("Current Population", selectedTile.populationCount.ToString());

        EditorGUILayout.BeginHorizontal();
        addAmount = EditorGUILayout.IntField("Add/Remove", addAmount);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            if (pm == null)
            {
                Debug.LogWarning("No PopulationManager found in scene. Cannot spawn agents.");
            }
            else
            {
                Undo.RecordObject(selectedTile, "Add Population");
                int spawnCount = Mathf.Max(0, addAmount);
                for (int i = 0; i < spawnCount; i++)
                {
                    pm.SpawnAgent(selectedTile, true);
                }
                EditorUtility.SetDirty(selectedTile);
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(selectedTile.gameObject.scene);
            }
        }
        if (GUILayout.Button("Remove", GUILayout.Width(80)))
        {
            var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            if (pm == null)
            {
                Debug.LogWarning("No PopulationManager found in scene. Cannot despawn agents.");
            }
            else
            {
                Undo.RecordObject(selectedTile, "Remove Population");
                int removeCount = Mathf.Max(0, addAmount);
                var agents = FindObjectsByType<PopulationAgent>(FindObjectsSortMode.None);
                int removed = 0;
                foreach (var a in agents)
                {
                    if (removed >= removeCount) break;
                    if (a.currentTile == selectedTile)
                    {
                        pm.DespawnAgent(a);
                        removed++;
                    }
                }
                if (removed == 0)
                    Debug.LogWarning("No agents found on selected tile to remove.");
                EditorUtility.SetDirty(selectedTile);
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(selectedTile.gameObject.scene);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        setValue = EditorGUILayout.IntField("Set To", setValue);
        if (GUILayout.Button("Set", GUILayout.Width(60)))
        {
            var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            if (pm == null)
            {
                Debug.LogWarning("No PopulationManager found in scene. Cannot modify agents.");
            }
            else
            {
                Undo.RecordObject(selectedTile, "Set Population");
                int current = selectedTile.populationCount;
                int target = Mathf.Max(0, setValue);
                if (target > current)
                {
                    int toSpawn = target - current;
                    for (int i = 0; i < toSpawn; i++) pm.SpawnAgent(selectedTile, true);
                }
                else if (target < current)
                {
                    int toRemove = current - target;
                    var agents = FindObjectsByType<PopulationAgent>(FindObjectsSortMode.None);
                    int removed = 0;
                    foreach (var a in agents)
                    {
                        if (removed >= toRemove) break;
                        if (a.currentTile == selectedTile)
                        {
                            pm.DespawnAgent(a);
                            removed++;
                        }
                    }
                }
                EditorUtility.SetDirty(selectedTile);
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(selectedTile.gameObject.scene);
            }
        }
        if (GUILayout.Button("Clear", GUILayout.Width(80)))
        {
            var pm = PopulationManager.Instance ?? FindFirstObjectByType<PopulationManager>();
            if (pm == null)
            {
                Debug.LogWarning("No PopulationManager found in scene. Cannot modify agents.");
            }
            else
            {
                Undo.RecordObject(selectedTile, "Clear Population");
                var agents = FindObjectsByType<PopulationAgent>(FindObjectsSortMode.None);
                int removed = 0;
                foreach (var a in agents)
                {
                    if (a.currentTile == selectedTile)
                    {
                        pm.DespawnAgent(a);
                        removed++;
                    }
                }
                EditorUtility.SetDirty(selectedTile);
                if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(selectedTile.gameObject.scene);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Ping Tile"))
        {
            EditorGUIUtility.PingObject(selectedTile.gameObject);
        }
    }
}
