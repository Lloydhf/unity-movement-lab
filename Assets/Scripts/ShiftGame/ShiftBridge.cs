using UnityEngine;

namespace ShiftGame
{
    public sealed class ShiftBridge : MonoBehaviour
    {
        [Tooltip("Köprünün geçilecek kısmı; collider ve görseller bu dalda olmalı.")]
        public Transform deck;
        public Collider surface;
        public Renderer[] visuals;
        public bool IsExtended { get; private set; }
        private bool initialized;

        private void Start()
        {
            Initialize();
            ApplyState();
        }

        private void Initialize()
        {
            if (initialized) return;
            if (deck == null) deck = transform;
            if (surface == null) surface = deck.GetComponentInChildren<Collider>();
            if (visuals == null || visuals.Length == 0) visuals = deck.GetComponentsInChildren<Renderer>();
            initialized = true;
        }

        public void Extend()
        {
            Initialize();
            IsExtended = true;
            ApplyState();
        }

        public void ResetBridge()
        {
            Initialize();
            IsExtended = false;
            ApplyState();
        }

        private void ApplyState()
        {
            // Fizik zemini ve görüntü aynı anda açılır; görünmez bir köprü oluşmaz.
            if (surface != null) surface.enabled = IsExtended;
            foreach (Renderer visual in visuals) if (visual != null) visual.enabled = IsExtended;
        }
    }
}
