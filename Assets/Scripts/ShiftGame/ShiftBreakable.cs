using System;
using UnityEngine;

namespace ShiftGame
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShiftBreakable : MonoBehaviour
    {
        [Tooltip("Ağır robot üstüne bastıktan sonra kırılmaya kalan saniye.")]
        public float breakDelay = 0.5f;
        public Collider surface;
        public Renderer[] visuals;
        public bool IsBroken { get; private set; }
        public event Action Broken;

        private bool initialized;
        private bool countingDown;
        private float remaining;
        private bool initialSurfaceEnabled;
        private bool[] initialVisualStates;
        private Color[] initialColors;
        private MaterialPropertyBlock[] initialProperties;
        private MaterialPropertyBlock warningProperties;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Start() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            if (surface == null) surface = GetComponentInChildren<Collider>();
            if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>();
            initialSurfaceEnabled = surface != null && surface.enabled;
            initialVisualStates = new bool[visuals.Length];
            initialColors = new Color[visuals.Length];
            initialProperties = new MaterialPropertyBlock[visuals.Length];
            warningProperties = new MaterialPropertyBlock();
            for (int i = 0; i < visuals.Length; i++)
            {
                Renderer visual = visuals[i];
                initialVisualStates[i] = visual != null && visual.enabled;
                if (visual == null) continue;
                initialProperties[i] = new MaterialPropertyBlock();
                visual.GetPropertyBlock(initialProperties[i]);
                Material material = visual.sharedMaterial;
                initialColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId) : Color.white;
            }
            Rigidbody body = GetComponent<Rigidbody>();
            // Child collider temaslarını bu kök bileşen alır; zemin düşmez.
            body.isKinematic = true;
            body.useGravity = false;
            initialized = true;
        }

        private void Update()
        {
            if (!countingDown || IsBroken) return;
            // Time.deltaTime duraklatmada sıfırdır; geri sayım da durur.
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Break();
            else WarnBeforeBreaking();
        }

        private void OnCollisionEnter(Collision collision) => CheckHeavyContact(collision);
        private void OnCollisionStay(Collision collision) => CheckHeavyContact(collision);

        private void CheckHeavyContact(Collision collision)
        {
            Initialize();
            if (IsBroken || countingDown || surface == null) return;
            ShiftRobot robot = collision.collider.GetComponentInParent<ShiftRobot>();
            if (robot == null || !robot.IsHeavy) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                // Zeminin bakış açısından üstteki robotun temas normali aşağı yönlüdür.
                if (contact.normal.y < -0.45f && contact.point.y >= surface.bounds.max.y - 0.18f)
                {
                    countingDown = true;
                    remaining = Mathf.Max(0f, breakDelay);
                    if (remaining <= 0f) Break();
                    else WarnBeforeBreaking();
                    return;
                }
            }
        }

        public void Break()
        {
            Initialize();
            if (IsBroken) return;
            IsBroken = true;
            countingDown = false;
            if (surface != null) surface.enabled = false;
            foreach (Renderer visual in visuals) if (visual != null) visual.enabled = false;
            Broken?.Invoke();
        }

        public void ResetPanel()
        {
            Initialize();
            IsBroken = false;
            countingDown = false;
            remaining = 0f;
            if (surface != null) surface.enabled = initialSurfaceEnabled;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null)
                {
                    visuals[i].enabled = initialVisualStates[i];
                    visuals[i].SetPropertyBlock(initialProperties[i]);
                }
        }

        private void WarnBeforeBreaking()
        {
            // Paylaşılan materyali değiştirmeden yalnız bu panelin rengini uyarı için yakarız.
            float pulse = 0.4f + 0.3f * Mathf.Sin((breakDelay - remaining) * 16f);
            Color warning = new Color(1f, 0.28f, 0.06f);
            for (int i = 0; i < visuals.Length; i++)
            {
                Renderer visual = visuals[i];
                if (visual == null) continue;
                visual.GetPropertyBlock(warningProperties);
                Color color = Color.Lerp(initialColors[i], warning, pulse);
                warningProperties.SetColor(BaseColorId, color);
                warningProperties.SetColor(ColorId, color);
                warningProperties.SetColor(EmissionId, warning * (0.3f + pulse));
                visual.SetPropertyBlock(warningProperties);
            }
        }
    }
}
