using UnityEngine;

namespace Utils
{
    public struct MinMax3D
    {
        public float XMin;
        public float XMax;
        public float YMin;
        public float YMax;
        public float ZMin;
        public float ZMax;

        public MinMax3D(float min, float max)
        {
            XMin = min;
            XMax = max;
            YMin = min;
            YMax = max;
            ZMin = min;
            ZMax = max;
        }

        public void AddPoint(Vector3 point)
        {
            XMin = Mathf.Min(XMin, point.x);
            XMax = Mathf.Max(XMax, point.x);
            YMin = Mathf.Min(YMin, point.y);
            YMax = Mathf.Max(YMax, point.y);
            ZMin = Mathf.Min(ZMin, point.z);
            ZMax = Mathf.Max(ZMax, point.z);
        }
    }
}