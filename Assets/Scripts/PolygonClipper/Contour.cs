using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martinez
{
    public class Contour
    {
        public List<Vector2> points;
        public List<int> holeIds;
        public int holeOf;
        public int depth;

        private bool clockwise;
        private bool orientationCalculated;

        public Contour()
        {
            points = new List<Vector2>();
            holeIds = new List<int>();
        }
        public bool isExterior => this.holeOf == -1;
        public bool isClockwise
        {
            get
            {
                if (orientationCalculated)
                    return clockwise;
                orientationCalculated = true;
                var area = SignedArea(points);
                clockwise = area < 0 ? true : false;
                return clockwise;
            }
        }
        private static float SignedArea(IReadOnlyList<Vector2> data)
        {
            int count = data.Count;
            if (count < 3) return 0f;

            float area = 0f;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                area += Helper.crossProduct(data[i], data[j]);
            }

            return area * 0.5f;
        }

    }
}