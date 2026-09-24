using System;
using UnityEngine;

namespace ShiftGame
{
    public sealed class ShiftPiston : MonoBehaviour
    {
        public string pistonId;
        public ShiftGate[] gates;
        public ShiftBridge[] bridges;
        public Transform plunger;
        public Renderer indicator;
        public bool IsLatched { get; private set; }
        public event Action Activated;

        private Vector3 raisedPosition;
        private MaterialPropertyBlock indicatorProperties;
        private bool initialized;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            Initialize();
            UpdateIndicator();
        }

        private void Initialize()
        {
            if (initialized) return;
            if (plunger != null) raisedPosition = plunger.localPosition;
            indicatorProperties = new MaterialPropertyBlock();
            initialized = true;
        }

        public bool TryActivate(ShiftRobot robot)
        {
            Initialize();
            if (IsLatched || robot == null || !robot.IsHeavy || !robot.IsGrounded) return false;
            CapsuleCollider robotCollider = robot.GetComponent<CapsuleCollider>();
            float feetY = robotCollider != null ? robotCollider.bounds.min.y : robot.transform.position.y - 0.8f;
            // Piston kökü basılabilir yüzeyin yüksekliğindedir; gövde merkezi değildir.
            if (Mathf.Abs(robot.transform.position.x - transform.position.x) > 0.95f
                || Mathf.Abs(feetY - transform.position.y) > 0.65f) return false;

            IsLatched = true;
            if (gates != null) foreach (ShiftGate gate in gates) if (gate != null) gate.Open();
            if (bridges != null) foreach (ShiftBridge bridge in bridges) if (bridge != null) bridge.Extend();
            UpdateIndicator();
            Activated?.Invoke();
            return true;
        }

        private void Update()
        {
            Initialize();
            if (plunger != null)
                plunger.localPosition = Vector3.MoveTowards(plunger.localPosition,
                    raisedPosition + (IsLatched ? Vector3.down * 0.16f : Vector3.zero), Time.deltaTime);
        }

        private void UpdateIndicator()
        {
            if (indicator == null) return;
            indicator.GetPropertyBlock(indicatorProperties);
            Color color = IsLatched ? new Color(0.2f, 1f, 0.55f) : new Color(1f, 0.64f, 0.15f);
            indicatorProperties.SetColor(BaseColorId, color);
            indicatorProperties.SetColor(EmissionColorId, color * 0.35f);
            indicator.SetPropertyBlock(indicatorProperties);
        }

        public void ResetPiston()
        {
            Initialize();
            IsLatched = false;
            if (plunger != null) plunger.localPosition = raisedPosition;
            UpdateIndicator();
            // Bağlı kapı/köprüleri oda yöneticisi tek bir yerde sıfırlar.
        }
    }
}
