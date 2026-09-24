using UnityEngine;

namespace PortfolioMagnetics
{
    public sealed class MagnetEmitter : MonoBehaviour
    {
        public MagneticBall targetBall;
        [Min(0.1f)] public float fieldRadius = 5f;
        [Min(0f)] public float acceleration = 18f;
        [Min(0.1f)] public float maxBallSpeed = 10f;
        public Renderer indicatorRenderer;
        public Color inactiveColor = new Color(0.18f, 0.28f, 0.34f);
        public Color activeColor = new Color(0.1f, 0.95f, 1f);
        public bool IsActive { get; private set; }

        private MaterialPropertyBlock indicatorProperties;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start() => RefreshIndicator();

        public void Toggle() => SetActive(!IsActive);

        public void SetActive(bool active)
        {
            IsActive = active;
            RefreshIndicator();
        }

        public void ResetMagnet() => SetActive(false);

        private void RefreshIndicator()
        {
            if (indicatorRenderer == null) return;
            if (indicatorProperties == null) indicatorProperties = new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(indicatorProperties);
            Color color = IsActive ? activeColor : inactiveColor;
            indicatorProperties.SetColor(BaseColorId, color);
            indicatorProperties.SetColor(ColorId, color);
            indicatorProperties.SetColor(EmissionColorId, IsActive ? color * 0.4f : Color.black);
            indicatorRenderer.SetPropertyBlock(indicatorProperties);
        }

        private void FixedUpdate()
        {
            if (!IsActive || targetBall == null || targetBall.Captured) return;
            Rigidbody body = targetBall.Body;
            if (body == null || body.isKinematic) return;

            Vector3 offset = transform.position - body.position;
            offset.z = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared > fieldRadius * fieldRadius) return;

            // Kuvvet yalnızca ekrandaki X/Y düzleminde uygulanır.
            if (distanceSquared > 0.0001f)
                body.AddForce(offset / Mathf.Sqrt(distanceSquared) * acceleration,
                    ForceMode.Acceleration);

            Vector3 velocity = body.linearVelocity;
            velocity.z = 0f;
            // Çok hızlı topun duvarların içinden geçme riskini azaltır.
            float speedLimit = Mathf.Max(0.1f, maxBallSpeed);
            if (velocity.sqrMagnitude > speedLimit * speedLimit)
                velocity = velocity.normalized * speedLimit;
            body.linearVelocity = velocity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsActive ? activeColor : Color.gray;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, fieldRadius));
        }
    }
}
