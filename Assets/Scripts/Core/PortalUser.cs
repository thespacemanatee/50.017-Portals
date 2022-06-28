using System.Linq;
using UnityEngine;

namespace Core
{
    public class PortalUser : MonoBehaviour
    {
        private static readonly int SliceNormal = Shader.PropertyToID("sliceNormal");
        private static readonly int SliceOffsetDst = Shader.PropertyToID("sliceOffsetDst");

        public GameObject graphicsObject;
        public GameObject GraphicsClone { get; set; }
        public Vector3 PreviousOffsetFromPortal { get; set; }
        public Material[] OriginalMaterials { get; set; }
        public Material[] CloneMaterials { get; set; }

        public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
        }

        // Called when first touches portal
        public virtual void EnterPortalThreshold()
        {
            if (GraphicsClone == null)
            {
                GraphicsClone = Instantiate(graphicsObject, graphicsObject.transform.parent, true);
                GraphicsClone.transform.localScale = graphicsObject.transform.localScale;
                OriginalMaterials = GetMaterials(graphicsObject);
                CloneMaterials = GetMaterials(GraphicsClone);
            }
            else
            {
                GraphicsClone.SetActive(true);
            }
        }

        // Called once no longer touching portal (excluding when teleporting)
        public virtual void ExitPortalThreshold()
        {
            GraphicsClone.SetActive(false);
            // Disable slicing
            foreach (var t in OriginalMaterials) t.SetVector(SliceNormal, Vector3.zero);
        }

        public void SetSliceOffsetDst(float dst, bool clone)
        {
            for (var i = 0; i < OriginalMaterials.Length; i++)
                if (clone)
                    CloneMaterials[i].SetFloat(SliceOffsetDst, dst);
                else
                    OriginalMaterials[i].SetFloat(SliceOffsetDst, dst);
        }

        private static Material[] GetMaterials(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<MeshRenderer>();
            return renderers.SelectMany(renderer => renderer.materials).ToArray();
        }
    }
}