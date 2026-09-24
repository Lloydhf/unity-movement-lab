using UnityEngine;

namespace ShiftGame
{
    /// <summary>Yandan takip kamerası. Sınırlar kameranın merkezinin gidebildiği konumlardır.</summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ShiftCamera : MonoBehaviour
    {
        public Transform target;
        public Vector2 minBounds = new Vector2(-1000f, -1000f);
        public Vector2 maxBounds = new Vector2(1000f, 1000f);
        [Min(1f)] public float size = 7.5f;
        [Min(0.1f)] public float followSharpness = 4f;

        private Camera view;
        private Rigidbody targetBody;
        private Transform cachedTarget;
        private Vector3 lastTargetPosition;
        private Vector3 focusPoint;
        private float focusRemaining;
        private float lookAhead;

        private void Awake()
        {
            view = GetComponent<Camera>();
            ConfigureCamera();
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void ConfigureCamera()
        {
            if (!view) view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = Mathf.Max(1f, size);
            transform.rotation = Quaternion.identity;
        }

        private void CacheTarget()
        {
            if (cachedTarget == target) return;
            cachedTarget = target;
            targetBody = target ? target.GetComponentInParent<Rigidbody>() : null;
            if (target) lastTargetPosition = target.position;
            lookAhead = 0f;
        }

        private void LateUpdate()
        {
            CacheTarget();
            if (!target || Time.deltaTime <= 0f) return;
            ConfigureCamera();

            float dt = Time.deltaTime;
            float horizontalVelocity = targetBody
                ? targetBody.linearVelocity.x
                : (target.position.x - lastTargetPosition.x) / dt;
            lastTargetPosition = target.position;
            float lookTarget = Mathf.Clamp(horizontalVelocity * 0.25f, -1.8f, 1.8f);
            lookAhead = Mathf.Lerp(lookAhead, lookTarget, 1f - Mathf.Exp(-3f * dt));

            Vector3 desired;
            if (focusRemaining > 0f)
            {
                desired = focusPoint + Vector3.up * 0.5f;
                focusRemaining = Mathf.Max(0f, focusRemaining - dt);
            }
            else
            {
                // Karakter merkezden hafif aşağıda kalır; üst HUD için boşluk sağlar.
                desired = target.position + new Vector3(lookAhead, 0.5f, 0f);
            }

            desired = ClampCenter(desired);
            float smoothing = 1f - Mathf.Exp(-Mathf.Max(0.1f, followSharpness) * dt);
            transform.position = ClampCenter(Vector3.Lerp(transform.position, desired, smoothing));
        }

        public void Focus(Vector3 point, float duration = 1.2f)
        {
            focusPoint = point;
            focusRemaining = Mathf.Max(0f, duration);
        }

        public void SnapToTarget()
        {
            CacheTarget();
            ConfigureCamera();
            focusRemaining = 0f;
            lookAhead = 0f;
            if (!target)
            {
                Vector3 position = transform.position;
                position.z = -25f;
                transform.position = position;
                return;
            }
            lastTargetPosition = target.position;
            transform.position = ClampCenter(target.position + Vector3.up * 0.5f);
        }

        private Vector3 ClampCenter(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, Mathf.Min(minBounds.x, maxBounds.x),
                Mathf.Max(minBounds.x, maxBounds.x));
            position.y = Mathf.Clamp(position.y, Mathf.Min(minBounds.y, maxBounds.y),
                Mathf.Max(minBounds.y, maxBounds.y));
            position.z = -25f;
            return position;
        }
    }
}
