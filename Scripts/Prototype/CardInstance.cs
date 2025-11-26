using System;
using Prototype.Cards;

namespace Prototype.Cards
{
    // Lightweight runtime wrapper in case you need instance-specific state
    [Serializable]
    public class CardInstance
    {
        public CardSO cardSO;
        public bool isExhausted = false;
        public int instanceId;

        public CardInstance(CardSO so, int id)
        {
            cardSO = so;
            instanceId = id;
        }
    }
}
