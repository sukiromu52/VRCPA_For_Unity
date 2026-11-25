using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(VRCPA.Editor.VRCPACameraRigPlugin))]

namespace VRCPA.Editor
{
    public sealed class VRCPACameraRigPlugin : Plugin<VRCPACameraRigPlugin>
    {
        private const string ModularAvatarPlugin = "nadena.dev.modular-avatar";

        public override string DisplayName => "VRCPA Camera Rig";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .BeforePlugin(ModularAvatarPlugin)
                .Run("Resolve VRCPA Hips attachment", AttachToHips);

            InPhase(BuildPhase.Transforming)
                .AfterPlugin(ModularAvatarPlugin)
                .AfterPlugin("nadena.dev.modular-avatar.late-transform-stages")
                .Run("Place VRCPA camera in front of the viewpoint", PositionCamera);
        }

        private static void AttachToHips(BuildContext context)
        {
            var animator = context.AvatarRootObject.GetComponent<Animator>();
            var humanoid = animator ? animator.avatar : null;
            if (!humanoid || !humanoid.isValid || !humanoid.isHuman ||
                !animator.GetBoneTransform(HumanBodyBones.Hips))
                return;

            foreach (var setup in context.AvatarRootObject.GetComponentsInChildren<VirtualLens2Setup>(true))
            {
                if (!setup.transform.Find("Anchor/X/Y/Z"))
                    continue;

                if (!setup.TryGetComponent<ModularAvatarBoneProxy>(out var proxy))
                    proxy = setup.gameObject.AddComponent<ModularAvatarBoneProxy>();

                proxy.boneReference = HumanBodyBones.Hips;
                proxy.subPath = string.Empty;
                proxy.attachmentMode = BoneProxyAttachmentMode.AsChildAtRoot;
            }
        }

        private static void PositionCamera(BuildContext context)
        {
            var avatar = context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (!avatar)
                return;

            foreach (var setup in context.AvatarRootObject.GetComponentsInChildren<VirtualLens2Setup>(true))
                SetStartingPose(setup.transform, avatar);
        }

        internal static void SetStartingPose(Transform rig, VRCAvatarDescriptor avatar)
        {
            var anchor = rig.Find("Anchor");
            var yawBase = anchor ? anchor.Find("X/Y/Z") : null;
            if (!yawBase)
                return;

            var avatarRoot = avatar.transform;
            var viewpoint = avatarRoot.TransformPoint(avatar.ViewPosition);
            var initialPosition = viewpoint + avatarRoot.forward;
            var follow = rig.Find("AnchorFollow");
            if (follow)
                follow.SetPositionAndRotation(initialPosition, avatarRoot.rotation);
            anchor.SetPositionAndRotation(initialPosition, avatarRoot.rotation);

            // Turn the camera before its animated yaw and pitch, leaving translation in Anchor space.
            yawBase.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
