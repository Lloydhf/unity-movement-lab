using UnityEngine;

namespace ShiftGame
{
    /// <summary>Yalnızca görsel parçaları oynatır; karakterin fizik gövdesine dokunmaz.</summary>
    [DisallowMultipleComponent]
    public sealed class ShiftRobotVisual : MonoBehaviour
    {
        public ShiftRobot robot;
        public Transform torso;
        public Transform head;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftArm;
        public Transform rightArm;
        public Renderer[] modeLights;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color LightColor = new Color(0.15f, 0.95f, 1f);
        private static readonly Color HeavyColor = new Color(1f, 0.6f, 0.12f);

        private struct RestPose
        {
            public Transform part;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public RestPose(Transform value)
            {
                part = value;
                position = value ? value.localPosition : Vector3.zero;
                rotation = value ? value.localRotation : Quaternion.identity;
                scale = value ? value.localScale : Vector3.one;
            }

            public void Apply(Vector3 offset, float roll, Vector3 scaleMultiplier)
            {
                if (!part) return;
                part.localPosition = position + offset;
                part.localRotation = rotation * Quaternion.Euler(0f, 0f, roll);
                part.localScale = Vector3.Scale(scale, scaleMultiplier);
            }
        }

        private RestPose torsoPose, headPose, leftLegPose, rightLegPose, leftArmPose, rightArmPose;
        private MaterialPropertyBlock propertyBlock;
        private float walkPhase;
        private float idlePhase;
        private float walking;
        private float horizontalSpeed;
        private float heavyBlend;
        private float landingPulse;
        private float previousVerticalSpeed;
        private bool wasGrounded;
        private bool initialized;

        private void Start()
        {
            if (!robot) robot = GetComponentInParent<ShiftRobot>();
            torsoPose = new RestPose(torso);
            headPose = new RestPose(head);
            leftLegPose = new RestPose(leftLeg);
            rightLegPose = new RestPose(rightLeg);
            leftArmPose = new RestPose(leftArm);
            rightArmPose = new RestPose(rightArm);
            propertyBlock = new MaterialPropertyBlock();
            wasGrounded = robot && robot.IsGrounded;
            heavyBlend = robot && robot.IsHeavy ? 1f : 0f;
            initialized = true;
            ApplyLightColor();
        }

        private void LateUpdate()
        {
            if (!initialized || !robot || !robot.Body || Time.deltaTime <= 0f) return;

            float dt = Time.deltaTime;
            Vector3 velocity = robot.Body.linearVelocity;
            bool grounded = robot.IsGrounded;
            float smoothing = 1f - Mathf.Exp(-12f * dt);
            float walkTarget = grounded && robot.ControlsEnabled
                ? Mathf.Clamp01(Mathf.Abs(velocity.x) / 3f)
                : 0f;
            walking = Mathf.Lerp(walking, walkTarget, smoothing);
            horizontalSpeed = Mathf.Lerp(horizontalSpeed, velocity.x, smoothing);
            heavyBlend = Mathf.Lerp(heavyBlend, robot.IsHeavy ? 1f : 0f, smoothing);

            // Temas anındaki kısa sıkışma yalnızca model üzerinde görünür.
            if (grounded && !wasGrounded && previousVerticalSpeed < -0.7f)
                landingPulse = Mathf.Clamp01(Mathf.Abs(previousVerticalSpeed) / 9f);
            wasGrounded = grounded;
            previousVerticalSpeed = velocity.y;
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, dt * 3.5f);

            idlePhase += dt * 2.4f;
            walkPhase = Mathf.Repeat(walkPhase + dt * Mathf.Abs(horizontalSpeed) * 3.6f, Mathf.PI * 2f);
            float step = Mathf.Sin(walkPhase) * walking;
            float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.035f * walking;
            float breathing = Mathf.Sin(idlePhase) * 0.007f * (1f - walking);
            float crouch = heavyBlend * 0.06f + landingPulse * 0.065f;
            float torsoLean = Mathf.Clamp(-horizontalSpeed * 1.1f, -5f, 5f);
            float squash = landingPulse * 0.11f;

            torsoPose.Apply(new Vector3(0f, bob + breathing - crouch, 0f), torsoLean,
                new Vector3(1f + squash * 0.5f + heavyBlend * 0.025f,
                    1f - squash - heavyBlend * 0.025f, 1f));
            headPose.Apply(new Vector3(Mathf.Clamp(horizontalSpeed * 0.005f, -0.025f, 0.025f),
                bob * 0.75f + breathing - crouch * 0.8f, 0f),
                Mathf.Clamp(-horizontalSpeed * 0.8f, -4f, 4f), Vector3.one);

            float legSpread = heavyBlend * 0.055f;
            float stride = Mathf.Lerp(18f, 11f, heavyBlend);
            leftLegPose.Apply(new Vector3(-legSpread, Mathf.Max(0f, step) * 0.065f, 0f),
                step * stride + heavyBlend * 3f, Vector3.one);
            rightLegPose.Apply(new Vector3(legSpread, Mathf.Max(0f, -step) * 0.065f, 0f),
                -step * stride - heavyBlend * 3f, Vector3.one);

            float airborneLift = !grounded ? Mathf.Clamp(velocity.y * 1.2f, -9f, 9f) : 0f;
            leftArmPose.Apply(new Vector3(-heavyBlend * 0.035f, bob * 0.3f - crouch * 0.4f, 0f),
                -step * 14f - heavyBlend * 8f - airborneLift, Vector3.one);
            rightArmPose.Apply(new Vector3(heavyBlend * 0.035f, bob * 0.3f - crouch * 0.4f, 0f),
                step * 14f + heavyBlend * 8f + airborneLift, Vector3.one);
            ApplyLightColor();
        }

        private void ApplyLightColor()
        {
            if (modeLights == null || propertyBlock == null) return;
            Color color = Color.Lerp(LightColor, HeavyColor, heavyBlend);
            float glow = 1.3f + Mathf.Sin(idlePhase) * 0.08f;
            foreach (Renderer lightRenderer in modeLights)
            {
                if (!lightRenderer) continue;
                lightRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetColor(EmissionId, color * glow);
                lightRenderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        private void OnDisable()
        {
            if (!initialized) return;
            torsoPose.Apply(Vector3.zero, 0f, Vector3.one);
            headPose.Apply(Vector3.zero, 0f, Vector3.one);
            leftLegPose.Apply(Vector3.zero, 0f, Vector3.one);
            rightLegPose.Apply(Vector3.zero, 0f, Vector3.one);
            leftArmPose.Apply(Vector3.zero, 0f, Vector3.one);
            rightArmPose.Apply(Vector3.zero, 0f, Vector3.one);
        }
    }
}
