using UnityEngine;

namespace Utils
{
    public static class CameraUtil
    {
        private static readonly Vector3[] CubeCornerOffsets =
        {
            new(1, 1, 1),
            new(-1, 1, 1),
            new(-1, -1, 1),
            new(-1, -1, -1),
            new(-1, 1, -1),
            new(1, -1, -1),
            new(1, 1, -1),
            new(1, -1, 1)
        };

        // http://wiki.unity3d.com/index.php/IsVisibleFrom
        public static bool VisibleFromCamera(Renderer renderer, Camera camera)
        {
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
        }

        public static bool BoundsOverlap(MeshFilter nearObject, MeshFilter farObject, Camera camera)
        {
            var near = GetScreenRectFromBounds(nearObject, camera);
            var far = GetScreenRectFromBounds(farObject, camera);

            // ensure far object is indeed further away than near object
            if (!(far.ZMax > near.ZMin)) return false;
            // Doesn't overlap on x axis
            if (far.XMax < near.XMin || far.XMin > near.XMax) return false;

            return !(far.YMax < near.YMin) && !(far.YMin > near.YMax);
        }

        // With thanks to http://www.turiyaware.com/a-solution-to-unitys-camera-worldtoscreenpoint-causing-ui-elements-to-display-when-object-is-behind-the-camera/
        public static MinMax3D GetScreenRectFromBounds(MeshFilter renderer, Camera mainCamera)
        {
            var minMax = new MinMax3D(float.MaxValue, float.MinValue);

            var localBounds = renderer.sharedMesh.bounds;
            var anyPointIsInFrontOfCamera = false;

            for (var i = 0; i < 8; i++)
            {
                var localSpaceCorner = localBounds.center + Vector3.Scale(localBounds.extents, CubeCornerOffsets[i]);
                var worldSpaceCorner = renderer.transform.TransformPoint(localSpaceCorner);
                var viewportSpaceCorner = mainCamera.WorldToViewportPoint(worldSpaceCorner);

                if (viewportSpaceCorner.z > 0)
                {
                    anyPointIsInFrontOfCamera = true;
                }
                else
                {
                    // If point is behind camera, it gets flipped to the opposite side
                    // So clamp to opposite edge to correct for this
                    viewportSpaceCorner.x = viewportSpaceCorner.x <= 0.5f ? 1 : 0;
                    viewportSpaceCorner.y = viewportSpaceCorner.y <= 0.5f ? 1 : 0;
                }

                // Update bounds with new corner point
                minMax.AddPoint(viewportSpaceCorner);
            }

            // All points are behind camera so just return empty bounds
            return !anyPointIsInFrontOfCamera ? new MinMax3D() : minMax;
        }
    }
}