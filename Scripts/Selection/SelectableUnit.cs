using UnityEngine;
using HexGrid;

namespace HexGrid
{
    [RequireComponent(typeof(Collider2D))]
    public class SelectableUnit : MonoBehaviour, ISelectable
    {
        [Tooltip("Reference to the GodBeast entity on this GameObject (optional). If not assigned, hex coords will default.)")]
        public GodBeast.GodBeast unit;

        public Hex HexCoordinates
        {
            get
            {
                if (unit != null)
                    return unit.HexCoordinates;
                return new Hex(0, 0, 0);
            }
        }

        public GameObject GameObject => gameObject;

        public void OnSelected()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && SelectionManager.Instance != null)
                sr.color = SelectionManager.Instance.selectColor;
        }

        public void OnDeselected()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && SelectionManager.Instance != null)
                sr.color = SelectionManager.Instance.normalColor;
        }
    }
}
