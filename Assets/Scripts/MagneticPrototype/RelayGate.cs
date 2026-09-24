using UnityEngine;

namespace PortfolioMagnetics
{
    public class RelayGate : MonoBehaviour
    {
        public GoalSocket socket;
        public Transform panel;
        public Renderer statusLight;
        private Vector3 closedPosition;
        private Collider blockingCollider;
        private MaterialPropertyBlock properties;
        public bool IsOpen => socket != null && socket.IsSatisfied;

        private void Awake()
        {
            if (panel == null) panel = transform;
            closedPosition = panel.localPosition;
            blockingCollider = panel.GetComponent<Collider>();
            properties = new MaterialPropertyBlock();
        }

        private void Update()
        {
            bool open = IsOpen;
            if (blockingCollider != null) blockingCollider.enabled = !open;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition,
                closedPosition + (open ? Vector3.up * 3.6f : Vector3.zero), Time.deltaTime * 5f);
            if (statusLight == null) return;
            Color color = open ? new Color(0.15f, 1f, 0.68f) : new Color(1f, 0.43f, 0.15f);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", color * 1.4f);
            statusLight.SetPropertyBlock(properties);
        }

        public void ResetGate()
        {
            panel.localPosition = closedPosition;
            if (blockingCollider != null) blockingCollider.enabled = true;
        }
    }
}
