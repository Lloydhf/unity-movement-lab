using System;
using UnityEngine;

namespace PortfolioMagnetics
{
    public sealed class GoalSocket : MonoBehaviour
    {
        public MagneticBall targetBall;
        public Transform capturePoint;
        [Min(0.01f)] public float captureRadius = 0.38f;
        public bool IsSatisfied { get; private set; }
        public event Action Captured;

        private void FixedUpdate()
        {
            if (IsSatisfied || targetBall == null || targetBall.Captured) return;
            Rigidbody body = targetBall.Body;
            if (body == null) return;
            Vector3 socketPosition = capturePoint != null
                ? capturePoint.position : transform.position;
            if ((body.position - socketPosition).sqrMagnitude > captureRadius * captureRadius)
                return;

            // Yuva, topun merkezini yakalayınca kapıya haber verir.
            IsSatisfied = true;
            targetBall.Capture(socketPosition);
            Captured?.Invoke();
        }

        // Bölüm sıfırlanırken top için de ResetBall çağrılır.
        public void ResetSocket() => IsSatisfied = false;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsSatisfied ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(capturePoint != null ? capturePoint.position : transform.position,
                Mathf.Max(0.01f, captureRadius));
        }
    }
}
