using UnityEngine;

namespace Core
{
    public class MainCamera : MonoBehaviour
    {
        private PortalController[] _portals;

        private void Awake()
        {
            _portals = FindObjectsOfType<PortalController>();
            Application.targetFrameRate = 45;
        }

        private void OnPreCull()
        {
            foreach (var portal in _portals)
            {
                portal.PrePortalRender();
            }

            foreach (var portal in _portals)
            {
                portal.Render();
            }

            foreach (var portal in _portals)
            {
                portal.PostPortalRender();
            }
        }
    }
}