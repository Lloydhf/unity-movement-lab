using UnityEngine;

namespace PortfolioMagnetics
{
    // Topun fizik durumunu ve yeniden başlatılmasını tek yerde tutar.
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class MagneticBall : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public bool Captured { get; private set; }

        private SphereCollider ballCollider;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Renderer[] ballRenderers;
        private bool[] initialRendererStates;
        private bool initialized;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            Body = GetComponent<Rigidbody>();
            ballCollider = GetComponent<SphereCollider>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            ballRenderers = GetComponentsInChildren<Renderer>(true);
            initialRendererStates = new bool[ballRenderers.Length];
            for (int i = 0; i < ballRenderers.Length; i++)
                initialRendererStates[i] = ballRenderers[i].enabled;

            Body.constraints = RigidbodyConstraints.FreezePositionZ
                             | RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationY;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.linearDamping = 0.35f;
            Body.angularDamping = 0.25f;
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            initialized = true;
        }

        public void Capture(Vector3 position)
        {
            Initialize();
            if (Captured || Body == null) return;
            Captured = true;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            // Yuvada duran top artık karakteri engellemez.
            ballCollider.enabled = false;
            Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Body.isKinematic = true;
            Body.position = position;
            transform.position = position;
        }

        public void ResetBall()
        {
            Initialize();
            if (Body == null) return;
            Captured = false;
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.position = startPosition;
            Body.rotation = startRotation;
            transform.SetPositionAndRotation(startPosition, startRotation);
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ballCollider.enabled = true;
            for (int i = 0; i < ballRenderers.Length; i++)
                if (ballRenderers[i] != null)
                    ballRenderers[i].enabled = initialRendererStates[i];
            Body.WakeUp();
        }
    }
}
