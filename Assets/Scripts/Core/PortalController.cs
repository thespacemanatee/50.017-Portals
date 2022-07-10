using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;

namespace Core
{
    public class PortalController : MonoBehaviour
    {
        private static readonly int DisplayMask = Shader.PropertyToID("displayMask");
        private static readonly int SliceCentre = Shader.PropertyToID("sliceCentre");
        private static readonly int SliceNormal = Shader.PropertyToID("sliceNormal");
        private static readonly int SliceOffsetDst = Shader.PropertyToID("sliceOffsetDst");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        [Header("Main Settings")] public PortalController linkedPortal;
        public MeshRenderer screen;
        public int recursionLimit = 5;

        [Header("Advanced Settings")] public float nearClipOffset = 0.05f;
        public float nearClipLimit = 0.2f;

        private readonly List<PortalUser> _trackedUsers = new();
        private Material _firstRecursionMat;
        private Camera _playerCam;
        private Camera _portalCam;
        private MeshFilter _screenMeshFilter;
        private RenderTexture _viewTexture;
        private Vector3 PortalCamPos => _portalCam.transform.position;


        private void Awake()
        {
            _playerCam = Camera.main;
            _portalCam = GetComponentInChildren<Camera>();
            _portalCam.enabled = false;
            _screenMeshFilter = screen.GetComponent<MeshFilter>();
            screen.material.SetInt(DisplayMask, 1);
        }

        private void LateUpdate()
        {
            HandleUsers();
        }

        private void OnTriggerEnter(Collider other)
        {
            var user = other.GetComponent<PortalUser>();
            if (user) HandleUserEnterPortal(user);
        }

        private void OnTriggerExit(Collider other)
        {
            var user = other.GetComponent<PortalUser>();
            if (!user || !_trackedUsers.Contains(user)) return;
            user.ExitPortalThreshold();
            _trackedUsers.Remove(user);
        }

        private void OnValidate()
        {
            if (linkedPortal != null) linkedPortal.linkedPortal = this;
        }

        private int SideOfPortal(Vector3 pos)
        {
            var currTransform = transform;
            return Math.Sign(Vector3.Dot(pos - currTransform.position, currTransform.forward));
        }

        private bool SameSideOfPortal(Vector3 posA, Vector3 posB)
        {
            return SideOfPortal(posA) == SideOfPortal(posB);
        }

        private void HandleUsers()
        {
            for (var i = 0; i < _trackedUsers.Count; i++)
            {
                var user = _trackedUsers[i];
                var userTransform = user.transform;
                var currTransform = transform;
                var m = linkedPortal.transform.localToWorldMatrix * currTransform.worldToLocalMatrix *
                        userTransform.localToWorldMatrix;

                var offsetFromPortal = userTransform.position - currTransform.position;
                var portalSide = Math.Sign(Vector3.Dot(offsetFromPortal, currTransform.forward));
                var portalSideOld = Math.Sign(Vector3.Dot(user.PreviousOffsetFromPortal, currTransform.forward));
                // Teleport the user if it has crossed from one side of the portal to the other
                if (portalSide != portalSideOld)
                {
                    var positionOld = userTransform.position;
                    var rotOld = userTransform.rotation;
                    user.Teleport(currTransform, linkedPortal.transform, m.GetColumn(3), m.rotation);
                    user.GraphicsClone.transform.SetPositionAndRotation(positionOld, rotOld);
                    // Can't rely on OnTriggerEnter/Exit to be called next frame since it depends on when FixedUpdate runs
                    linkedPortal.HandleUserEnterPortal(user);
                    _trackedUsers.RemoveAt(i);
                    i--;
                }
                else
                {
                    user.GraphicsClone.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                    //UpdateSliceParams(user);
                    user.PreviousOffsetFromPortal = offsetFromPortal;
                }
            }
        }

        // Called before any portal cameras are rendered for the current frame
        public void PrePortalRender()
        {
            foreach (var user in _trackedUsers) UpdateSliceParams(user);
        }

        // Manually render the camera attached to this portal
        // Called after PrePortalRender, and before PostPortalRender
        public void Render()
        {
            // Skip rendering the view from this portal if player is not looking at the linked portal
            if (!CameraUtil.VisibleFromCamera(linkedPortal.screen, _playerCam)) return;

            CreateViewTexture();

            var localToWorldMatrix = _playerCam.transform.localToWorldMatrix;
            var renderPositions = new Vector3[recursionLimit];
            var renderRotations = new Quaternion[recursionLimit];

            var startIndex = 0;
            _portalCam.projectionMatrix = _playerCam.projectionMatrix;
            for (var i = 0; i < recursionLimit; i++)
            {
                if (i > 0)
                    // Don't recurse if linked portal is not visible through this portal
                    if (!CameraUtil.BoundsOverlap(_screenMeshFilter, linkedPortal._screenMeshFilter, _portalCam))
                        break;

                localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix *
                                     localToWorldMatrix;
                var renderOrderIndex = recursionLimit - i - 1;
                renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
                renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

                _portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex],
                    renderRotations[renderOrderIndex]);
                startIndex = renderOrderIndex;
            }

            // Hide screen so that camera can see through portal
            screen.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            linkedPortal.screen.material.SetInt(DisplayMask, 0);

            for (var i = startIndex; i < recursionLimit; i++)
            {
                _portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
                // TODO: Figure out why this doesn't work properly
                // SetNearClipPlane();
                HandleClipping();
                _portalCam.Render();

                if (i == startIndex) linkedPortal.screen.material.SetInt(DisplayMask, 1);
            }

            // Unhide objects hidden at start of render
            screen.shadowCastingMode = ShadowCastingMode.On;
        }

        private void HandleClipping()
        {
            // There are two main graphical issues when slicing users
            // 1. Tiny sliver of mesh drawn on backside of portal
            //    Ideally the oblique clip plane would sort this out, but even with 0 offset, tiny sliver still visible
            // 2. Tiny seam between the sliced mesh, and the rest of the model drawn onto the portal screen
            // This function tries to address these issues by modifying the slice parameters when rendering the view from the portal
            // Would be great if this could be fixed more elegantly, but this is the best I can figure out for now
            const float hideDst = -1000;
            const float showDst = 1000;
            var screenThickness = linkedPortal.ProtectScreenFromClipping(_portalCam.transform.position);

            foreach (var user in _trackedUsers)
            {
                // Addresses issue 1
                user.SetSliceOffsetDst(
                    // Addresses issue 2
                    SameSideOfPortal(user.transform.position, PortalCamPos) ? hideDst : showDst, false);

                // Ensure clone is properly sliced, in case it's visible through this portal:
                var cloneSideOfLinkedPortal = -SideOfPortal(user.transform.position);
                var camSameSideAsClone = linkedPortal.SideOfPortal(PortalCamPos) == cloneSideOfLinkedPortal;
                user.SetSliceOffsetDst(camSameSideAsClone ? screenThickness : -screenThickness, true);
            }

            foreach (var linkedUser in linkedPortal._trackedUsers)
            {
                var userPos = linkedUser.graphicsObject.transform.position;
                // Handle clone of linked portal coming through this portal:
                var cloneOnSameSideAsCam = linkedPortal.SideOfPortal(userPos) != SideOfPortal(PortalCamPos);
                // Addresses issue 2
                // Addresses issue 1
                linkedUser.SetSliceOffsetDst(cloneOnSameSideAsCam ? hideDst : showDst, true);

                // Ensure user of linked portal is properly sliced, in case it's visible through this portal:
                var isCamSameSideAsUser =
                    linkedPortal.SameSideOfPortal(linkedUser.transform.position, PortalCamPos);
                linkedUser.SetSliceOffsetDst(isCamSameSideAsUser ? screenThickness : -screenThickness, false);
            }
        }

        // Called once all portals have been rendered, but before the player camera renders
        public void PostPortalRender()
        {
            foreach (var user in _trackedUsers) UpdateSliceParams(user);

            ProtectScreenFromClipping(_playerCam.transform.position);
        }

        private void CreateViewTexture()
        {
            if (_viewTexture != null && _viewTexture.width == Screen.width &&
                _viewTexture.height == Screen.height) return;
            if (_viewTexture != null) _viewTexture.Release();

            _viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            // Render the view from the portal camera to the view texture
            _portalCam.targetTexture = _viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture(MainTex, _viewTexture);
        }

        // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
        private float ProtectScreenFromClipping(Vector3 viewPoint)
        {
            var halfHeight = _playerCam.nearClipPlane * Mathf.Tan(_playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfWidth = halfHeight * _playerCam.aspect;
            var dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, _playerCam.nearClipPlane).magnitude;

            // TODO: Fix this hack to get the screen to not clip with the camera near plane
            // var screenTransform = screen.transform;
            // var currTransform = transform;
            // var isCamFacingSameDirAsPortal = Vector3.Dot(currTransform.forward, currTransform.position - viewPoint) > 0;
            // var localScale = screenTransform.localScale;
            // localScale = new Vector3(localScale.x, localScale.y, dstToNearClipPlaneCorner);
            // screenTransform.localScale = localScale;
            // screenTransform.localPosition = Vector3.forward * dstToNearClipPlaneCorner *
            //                                 (isCamFacingSameDirAsPortal ? 0.5f : -0.5f);
            return dstToNearClipPlaneCorner;
        }

        private void UpdateSliceParams(PortalUser user)
        {
            // Calculate slice normal
            var currTransform = transform;
            var userPosition = user.transform.position;
            var linkedPortalTransform = linkedPortal.transform;

            var side = SideOfPortal(userPosition);
            var sliceNormal = currTransform.forward * -side;
            var cloneSliceNormal = linkedPortalTransform.forward * side;

            // Calculate slice centre
            var slicePos = currTransform.position;
            var cloneSlicePos = linkedPortalTransform.position;

            // Adjust slice offset so that when player standing on other side of portal to the object, the slice doesn't clip through
            float sliceOffsetDst = 0;
            float cloneSliceOffsetDst = 0;
            var screenThickness = screen.transform.localScale.z;

            var playerSameSideAsUser = SameSideOfPortal(_playerCam.transform.position, userPosition);
            if (!playerSameSideAsUser) sliceOffsetDst = -screenThickness;

            var playerSameSideAsCloneAppearing = side != linkedPortal.SideOfPortal(_playerCam.transform.position);
            if (!playerSameSideAsCloneAppearing) cloneSliceOffsetDst = -screenThickness;

            // Apply parameters
            for (var i = 0; i < user.OriginalMaterials.Length; i++)
            {
                user.OriginalMaterials[i].SetVector(SliceCentre, slicePos);
                user.OriginalMaterials[i].SetVector(SliceNormal, sliceNormal);
                user.OriginalMaterials[i].SetFloat(SliceOffsetDst, sliceOffsetDst);

                user.CloneMaterials[i].SetVector(SliceCentre, cloneSlicePos);
                user.CloneMaterials[i].SetVector(SliceNormal, cloneSliceNormal);
                user.CloneMaterials[i].SetFloat(SliceOffsetDst, cloneSliceOffsetDst);
            }
        }

        // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
        // Note that this affects precision of the depth buffer, which can cause issues with effects like screenspace AO
        private void SetNearClipPlane()
        {
            var clipPlane = transform;
            var position = clipPlane.position;
            var forward = clipPlane.forward;
            var dot = Math.Sign(Vector3.Dot(forward, position - _portalCam.transform.position));

            var camSpacePos = _portalCam.worldToCameraMatrix.MultiplyPoint(position);
            var camSpaceNormal = _portalCam.worldToCameraMatrix.MultiplyVector(forward) * dot;
            var camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

            // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
            if (Mathf.Abs(camSpaceDst) > nearClipLimit)
            {
                var clipPlaneCameraSpace =
                    new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

                // Update projection based on new clip plane
                // Calculate matrix with player cam so that player camera settings (fov, etc) are used
                _portalCam.projectionMatrix = _playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
            }
            else
            {
                _portalCam.projectionMatrix = _playerCam.projectionMatrix;
            }
        }

        private void HandleUserEnterPortal(PortalUser user)
        {
            if (_trackedUsers.Contains(user)) return;
            user.EnterPortalThreshold();
            user.PreviousOffsetFromPortal = user.transform.position - transform.position;
            _trackedUsers.Add(user);
        }
    }
}