using UnityEngine;
using VRC.SDKBase;

namespace VRCPA
{
    [AddComponentMenu("VRCPA/VirtualLens2 Setup")]
    [DisallowMultipleComponent]
    public sealed class VirtualLens2Setup : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private GameObject virtualLensObject;
        [SerializeField] private Transform cameraPose;

        public GameObject VirtualLensObject => virtualLensObject;
        public Transform CameraPose => cameraPose;
    }
}
