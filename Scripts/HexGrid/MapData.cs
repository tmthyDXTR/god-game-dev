using System.Collections.Generic;
using UnityEngine;

namespace HexGrid
{
    [System.Serializable]
    public class ResourceRecord
    {
        public Managers.ResourceManager.GameResource type;
        public int amount;
    }

    [System.Serializable]
    public class TileTypeRecord
    {
        public int q;
        public int r;
        public int s;
        public HexTileType type;
        public List<ResourceRecord> resources = new List<ResourceRecord>();
    }

    [CreateAssetMenu(menuName = "HexGrid/MapData")]
    public class MapData : ScriptableObject
    {
        public List<TileTypeRecord> tiles = new List<TileTypeRecord>();
    }
}
