using UnityEngine;

namespace ShiftGame
{
    public sealed class ShiftGate : MonoBehaviour
    {
        public Transform panel;
        public Collider blocker;
        [Tooltip("Kapının açılırken kendi koordinatlarında gideceği uzaklık.")]
        public Vector3 openOffset = new Vector3(0f, 3f, 0f);
        public bool IsOpen { get; private set; }
        private Vector3 closedPosition;
        private bool initialized;

        private void Start() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (panel == null) panel = transform;
            if (blocker == null) blocker = panel.GetComponentInChildren<Collider>();
            closedPosition = panel.localPosition;
            initialized = true;
        }

        private void Update()
        {
            Initialize();
            panel.localPosition = Vector3.MoveTowards(panel.localPosition,
                closedPosition + (IsOpen ? openOffset : Vector3.zero), 5f * Time.deltaTime);
        }

        public void Open()
        {
            Initialize();
            IsOpen = true;
            if (blocker != null) blocker.enabled = false;
        }

        public void ResetGate()
        {
            Initialize();
            IsOpen = false;
            panel.localPosition = closedPosition;
            if (blocker != null) blocker.enabled = true;
        }
    }
}
