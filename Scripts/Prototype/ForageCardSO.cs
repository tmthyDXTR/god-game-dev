using UnityEngine;
using HexGrid;

namespace Prototype.Cards
{
    [CreateAssetMenu(menuName = "Prototype/ForageCardSO", fileName = "ForageCard_")]
    public class ForageCardSO : CardSO
    {
        [Header("Forage")]
        [Tooltip("Min food added when foraging")] public int minFood = 1;
        [Tooltip("Max food added when foraging")] public int maxFood = 3;
        [Tooltip("Chance to find any food (0..1)")] [Range(0f, 1f)] public float successChance = 1f;
        [Tooltip("Only yields full results on Forest tiles; elsewhere yields reduced amount")] public bool prefersForest = true;
        [Tooltip("Multiplier applied when tile is not preferred (e.g., not forest)")] public float offTileMultiplier = 0.5f;

        // Play the card in the Overworld context. Expect `target` to be a HexTile.
        public override void PlayOverworld(object target)
        {
            if (target == null) return;
            var tile = target as HexTile;
            if (tile == null)
            {
                Debug.LogWarning($"ForageCard.PlayOverworld: expected HexTile target but got {target.GetType().Name}");
                return;
            }

            // success roll
            float r = Random.Range(0f, 1f);
            if (r > successChance)
            {
                Debug.Log($"ForageCard '{cardName}' failed to find anything (roll {r:0.00} > chance {successChance:0.00})");
                return;
            }

            int amount = Random.Range(minFood, maxFood + 1);
            if (prefersForest && tile.TileType != HexTileType.Forest)
            {
                amount = Mathf.Max(0, Mathf.FloorToInt(amount * offTileMultiplier));
            }

            if (amount <= 0)
            {
                Debug.Log($"ForageCard '{cardName}' found nothing relevant on tile {tile.name}");
                return;
            }

            tile.AddResource(Managers.ResourceManager.GameResource.Food, amount);
            Debug.Log($"ForageCard '{cardName}' added {amount} Food to tile {tile.name}");
        }
    }
}
