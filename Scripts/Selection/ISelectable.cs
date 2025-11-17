using UnityEngine;
using HexGrid;

namespace HexGrid
{
    public interface ISelectable
    {
        // Called when the object is selected
        void OnSelected();

        // Called when the object is deselected
        void OnDeselected();

        // Provides hex coordinates when applicable (units on hex grid)
        Hex HexCoordinates { get; }

        // Underlying GameObject
        GameObject GameObject { get; }
    }
}
