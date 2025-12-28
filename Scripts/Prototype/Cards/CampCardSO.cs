using UnityEngine;
using HexGrid;

namespace Prototype.Cards
{
    [CreateAssetMenu(menuName = "Prototype/CampCardSO", fileName = "CampCard_")]
    public class CampCardSO : CardSO
    {
        [Header("Camp")]
        [Tooltip("If true, will refuse to place if tile already has a settlement")] public bool requireEmptyTile = true;
        [Tooltip("Camp sprite to display on the tile")] public Sprite campSprite;
        [Header("Placement")]
        [Tooltip("Allowed tile types for camp placement. Empty = any type allowed.")] public HexTileType[] allowedTileTypes;
        [Tooltip("Require at least this many population on the tile to place (0 = no requirement)")] public int requireMinPopulation = 1;
        [Tooltip("Local Z offset for camp visual")] public float visualZ = -0.2f;
        [Tooltip("Scale multiplier for the camp sprite")] public float spriteScale = 1f;

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

            // spawn a simple sprite visual under the tile
            CreateCampVisual(tile.transform, visualZ, spriteScale);
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

        private void CreateCampVisual(Transform parent, float zOffset, float scale)
        {
            // Avoid creating multiple visuals by name
            var existing = parent.Find("CampVisual");
            if (existing != null) return;

            var camp = new GameObject("CampVisual");
            camp.transform.SetParent(parent, worldPositionStays: false);
            camp.transform.localPosition = new Vector3(0f, 0f, zOffset);
            camp.transform.localScale = Vector3.one * scale;

            var sr = camp.AddComponent<SpriteRenderer>();
            sr.sprite = campSprite;
            // Set sorting order to be above tiles (100) but below trees (1000+)
            sr.sortingOrder = 500;
        }
    }
}
