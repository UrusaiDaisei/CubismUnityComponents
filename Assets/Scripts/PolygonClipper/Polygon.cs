using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public struct Polygon
    {
        public List<Vector2> nodes;
        public List<int> startIDs;
        public Polygon(int size)
        {
            nodes = new List<Vector2>(size);
            startIDs = new List<int>();
        }
        public Polygon(int NodeSize, int Components)
        {
            nodes = new List<Vector2>(NodeSize);
            startIDs = new List<int>(Components);
        }
        public void AddComponent()
        {
            startIDs.Add(this.nodes.Count);
        }
        public void Clear()
        {
            nodes.Clear();
            startIDs.Clear();
        }
    }
}

