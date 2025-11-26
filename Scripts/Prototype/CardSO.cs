using UnityEngine;

namespace Prototype.Cards
{
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
        public int costSap = 0; // simple numeric cost example

        [Header("Presentation")]
        public Sprite artwork;
        public Sprite zoneIcon;
    }
}
