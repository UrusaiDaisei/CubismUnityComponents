
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public sealed class PolygonBuilder
    {
        private List<Vector2> _nodes = new List<Vector2>();
        private List<int> _startIDs = new List<int>();

        public void CreateComponent(IEnumerable<Vector2> points)
        {
            _startIDs.Add(_nodes.Count);
            _nodes.AddRange(points);
        }

        public Polygon Build()
        {
            return new Polygon(_nodes.ToArray(), _startIDs.ToArray());
        }

        public void Clear()
        {
            _nodes.Clear();
            _startIDs.Clear();
        }
    }
}