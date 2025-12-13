using UnityEngine;
using HexGrid;

namespace Prototype.Cards
{
    [CreateAssetMenu(menuName = "Prototype/CampCardSO", fileName = "CampCard_")]
    public class CampCardSO : CardSO
    {
        [Header("Camp")]
        [Tooltip("If true, will refuse to place if tile already has a settlement")] public bool requireEmptyTile = true;
        [Header("Placement")]
        [Tooltip("Allowed tile types for camp placement. Empty = any type allowed.")] public HexTileType[] allowedTileTypes;
        [Tooltip("Require at least this many population on the tile to place (0 = no requirement)")] public int requireMinPopulation = 1;
        [Tooltip("Local Z offset for camp visual")] public float visualZ = -0.2f;
        [Tooltip("Triangle size for the camp marker")] public float triangleSize = 0.30f;

        public override void PlayOverworld(object target)
        {
            if (target == null) return;
            var tile = target as HexTile;
            if (tile == null)
            {
                Debug.LogWarning($"CampCard.PlayOverworld: expected HexTile target but got {target.GetType().Name}");
                return;
            }

            // Check placement rules: tile type
            if (allowedTileTypes != null && allowedTileTypes.Length > 0)
            {
                bool ok = false;
                foreach (var t in allowedTileTypes)
                    if (t == tile.TileType) { ok = true; break; }
                if (!ok)
                {
                    Debug.Log($"CampCard '{cardName}' cannot be placed on tile type {tile.TileType}");
                    return;
                }
            }

            // require population on tile
            if (requireMinPopulation > 0)
            {
                // HexTile exposes populationCount
                if (tile.populationCount < requireMinPopulation)
                {
                    Debug.Log($"CampCard '{cardName}' requires at least {requireMinPopulation} population on tile, found {tile.populationCount}");
                    return;
                }
            }

            var existing = tile.GetComponent<Settlement>();
            if (existing != null && requireEmptyTile)
            {
                Debug.Log($"CampCard '{cardName}' could not place camp: tile already has settlement {existing.settlementId}");
                return;
            }

            if (existing == null)
            {
                var settlement = tile.gameObject.AddComponent<Settlement>();
                settlement.settlementId = System.Guid.NewGuid().ToString();
                settlement.settlementOwner = "Player";
                settlement.settlementLevel = 1;
                settlement.isMobile = true;
            }

            // spawn a simple triangular visual under the tile
            CreateCampVisual(tile.transform, visualZ, triangleSize);
            Debug.Log($"CampCard '{cardName}' placed camp on tile {tile.HexCoordinates}");
        }

        // Ensure targeting respects placement rules so highlighting only shows
        // tiles that would actually accept this camp (type, population, emptiness)
        public override bool CanTarget(object target)
        {
            if (target == null) return false;
            var tile = target as HexGrid.HexTile;
            if (tile == null) return false;

            // tile type constraint
            if (allowedTileTypes != null && allowedTileTypes.Length > 0)
            {
                bool ok = false;
                foreach (var t in allowedTileTypes)
                    if (t == tile.TileType) { ok = true; break; }
                if (!ok) return false;
            }

            // population requirement
            if (requireMinPopulation > 0)
            {
                if (tile.populationCount < requireMinPopulation) return false;
            }

            // if the card requires the tile to be empty, reject tiles with existing settlements
            if (requireEmptyTile)
            {
                var existing = tile.GetComponent<Settlement>();
                if (existing != null) return false;
            }

            return true;
        }

        private void CreateCampVisual(Transform parent, float zOffset, float size)
        {
            // Avoid creating multiple visuals by name
            var existing = parent.Find("CampVisual");
            if (existing != null) return;

            var camp = new GameObject("CampVisual");
            camp.transform.SetParent(parent, worldPositionStays: false);
            camp.transform.localPosition = new Vector3(0f, 0f, zOffset);

            var mf = camp.AddComponent<MeshFilter>();
            var mr = camp.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            float h = size;
            float w = size;
            mesh.vertices = new Vector3[] {
                new Vector3(0f, h, 0f),
                new Vector3(w, -h, 0f),
                new Vector3(-w, -h, 0f)
            };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            var shader = Shader.Find("Sprites/Default");
            Material mat = null;
            if (shader != null)
                mat = new Material(shader) { color = Color.red };
            else
                mat = new Material(Shader.Find("Standard")) { color = Color.red };
            mr.material = mat;
        }
    }
}
