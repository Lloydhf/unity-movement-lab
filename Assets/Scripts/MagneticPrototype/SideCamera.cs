using UnityEngine;

namespace PortfolioMagnetics
{
    public class SideCamera : MonoBehaviour
    {
        public Transform target;
        public float minimumX = -2f;
        public float maximumX = 6f;
        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 position = transform.position;
            position.x = Mathf.Lerp(position.x, Mathf.Clamp(target.position.x + 2f, minimumX, maximumX),
                1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
            transform.position = position;
        }
    }
}
