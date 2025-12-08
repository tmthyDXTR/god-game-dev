using UnityEngine;
using UnityEngine.Localization;

namespace Prototype.Cards
{
    public enum CardTargetType { Any, Tile, Unit, None }

    public enum CardZone { Both, Overworld, Combat }

    [CreateAssetMenu(menuName = "Prototype/CardSO", fileName = "CardSO_")]
    public class CardSO : ScriptableObject
    {
        [Header("Identity")]
        public string cardName;
        [TextArea(3,6)] public string description;
        public CardZone zone = CardZone.Both;

        [Header("Gameplay")]
        public bool exhaustsOnUse = false;
        public int costAP = 0; // simple numeric cost example

        [Header("Presentation")]
        public Sprite artwork;
        public Sprite zoneIcon;

        [Header("Localization")]
        [Tooltip("Localized name (falls back to `cardName` if empty)")]
        public LocalizedString localizedName;
        [Tooltip("Localized description (falls back to `description` if empty)")]
        public LocalizedString localizedDescription;

        [Header("Targeting")]
        [Tooltip("Simple target hint for UI/validation")]
        public CardTargetType targetType = CardTargetType.Tile;

        /// <summary>
        /// Validate whether this card can target the provided object. Default
        /// implementation uses the `targetType` hint and basic type checks.
        /// Override in derived cards for custom rules.
        /// </summary>
        public virtual bool CanTarget(object target)
        {
            if (target == null) return false;
            switch (targetType)
            {
                case CardTargetType.Tile:
                    return target is HexGrid.HexTile;
                case CardTargetType.Unit:
                    return target is HexGrid.ISelectable;
                case CardTargetType.Any:
                    return true;
                case CardTargetType.None:
                default:
                    return false;
            }
        }

        // Play hooks: override these in derived card types to implement behavior
        // when a card is played in a given zone. Default does nothing.
        public virtual void PlayOverworld(object target) { }
        public virtual void PlayCombat(object target) { }
    }
}
