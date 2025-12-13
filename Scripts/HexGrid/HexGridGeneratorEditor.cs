using UnityEditor;
using UnityEngine;

namespace HexGrid
{
    [CustomEditor(typeof(HexGridGenerator))]
    public class HexGridGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
                DrawDefaultInspector();
                HexGridGenerator generator = (HexGridGenerator)target;
                GUILayout.Space(10);
                if (GUILayout.Button("Generate Grid"))
                {
                    generator.GenerateGrid();
                }
                if (GUILayout.Button("Clear Grid"))
                {
                    generator.ClearGrid();
                }
                if (GUILayout.Button("Add Food Resources to Forest Tiles"))
                {
                    generator.AddFoodResourcesToForestTiles();
                }
                if (GUILayout.Button("Add Materials Resources to Forest Tiles"))
                {
                    generator.AddMaterialsResourcesToForestTiles();
                }
                if (GUILayout.Button("Remove All Resources from Tiles"))
                {
                    generator.RemoveAllResources();
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Map Save/Load", EditorStyles.boldLabel);

                if (GUILayout.Button("Save Tile Types to MapData"))
                {
                    if (generator.mapData != null)
                        generator.SaveTileTypesToMap(generator.mapData);
                    else
                        Debug.LogWarning("Assign a MapData asset to save tile types.");
                }
                if (GUILayout.Button("Load Tile Types from MapData"))
                {
                    if (generator.mapData != null)
                        generator.LoadTileTypesFromMap(generator.mapData);
                    else
                        Debug.LogWarning("Assign a MapData asset to load tile types.");
                }
        }

        private void OnSceneGUI()
        {
            HexGridGenerator generator = (HexGridGenerator)target;
            if (!generator.paintMode) return;
            Event e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                var hit2D = Physics2D.Raycast(ray.origin, ray.direction);
                if (hit2D.collider != null)
                {
                    var tile = hit2D.collider.GetComponent<HexTile>();
                    if (tile != null)
                    {
                        Undo.RecordObject(tile, "Paint Hex Tile Type");
                        tile.SetTileType(generator.paintTileType);
                        EditorUtility.SetDirty(tile);
                        Selection.activeObject = null;
                    }
                }
            }
        }
    }
}
