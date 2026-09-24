using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftGame
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ShiftRobot : MonoBehaviour
    {
        [Tooltip("İki modun ortak yürüme hızı.")]
        public float moveSpeed = 4.4f;
        [Tooltip("Hafif modun yukarı zıplama hızı.")]
        public float lightJump = 7.2f;
        [Tooltip("Ağır modun yukarı zıplama hızı.")]
        public float heavyJump = 3.8f;
        [Tooltip("Bölüm başlangıcındaki mod; gövdenin şekli iki modda da aynıdır.")]
        public bool initialHeavy = true;

        public Rigidbody Body { get; private set; }
        public bool IsHeavy { get; private set; }
        public bool ControlsEnabled { get; private set; } = true;
        public bool IsGrounded => contacts.Count > 0 && Time.fixedTime >= ignoreGroundUntil;
        public event Action<bool> ModeChanged;
        public event Action<float> Landed;

        private const float CoyoteTime = 0.10f;
        private const float JumpBuffer = 0.12f;
        private readonly HashSet<Collider> contacts = new HashSet<Collider>();
        private CapsuleCollider capsule;
        private PhysicsMaterial frictionless;
        private PhysicsMaterial originalMaterial;
        private float moveInput;
        private float jumpUntil = float.NegativeInfinity;
        private float lastGroundAt = float.NegativeInfinity;
        private float ignoreGroundUntil;
        private float fallSpeed;
        private bool jumpConsumed = true;
        private bool wasGrounded;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.height = 1.6f;
            capsule.radius = 0.32f;
            capsule.center = Vector3.zero;
            Body.isKinematic = false;
            Body.useGravity = true;
            Body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 0f;
            originalMaterial = capsule.sharedMaterial;
            frictionless = new PhysicsMaterial("Shift robot - no friction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
                hideFlags = HideFlags.DontSave
            };
            capsule.sharedMaterial = frictionless;
            IsHeavy = initialHeavy;
            ApplyMass();
        }

        private void Update()
        {
            moveInput = 0f;
            Keyboard keyboard = Keyboard.current;
            if (!ControlsEnabled || keyboard == null || (!Application.isFocused && !Application.isBatchMode)) return;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput += 1f;
            if (keyboard.spaceKey.wasPressedThisFrame)
                jumpUntil = Time.time + Mathf.Max(JumpBuffer, Time.fixedDeltaTime);
        }

        private void FixedUpdate()
        {
            if (Body == null || Body.isKinematic) return;
            contacts.RemoveWhere(Unavailable);
            bool grounded = IsGrounded;
            if (grounded)
            {
                if (!wasGrounded) Landed?.Invoke(fallSpeed);
                fallSpeed = 0f;
                lastGroundAt = Time.fixedTime;
                jumpConsumed = false;
            }
            else fallSpeed = Mathf.Max(fallSpeed, -Body.linearVelocity.y);
            wasGrounded = grounded;

            Vector3 velocity = Body.linearVelocity;
            velocity.x = ControlsEnabled ? moveInput * moveSpeed : 0f;
            velocity.z = 0f;
            // Kenardan hemen sonra ve yere değmeden hemen önce küçük bir tolerans verilir.
            bool canJump = !jumpConsumed && Time.fixedTime - lastGroundAt <= CoyoteTime;
            if (ControlsEnabled && Time.fixedTime <= jumpUntil && canJump)
            {
                velocity.y = IsHeavy ? heavyJump : lightJump;
                jumpConsumed = true;
                jumpUntil = float.NegativeInfinity;
                lastGroundAt = float.NegativeInfinity;
                ignoreGroundUntil = Time.fixedTime + 0.06f;
                contacts.Clear();
                wasGrounded = false;
            }
            Body.linearVelocity = velocity;
        }

        public void SetHeavy(bool heavy)
        {
            bool changed = IsHeavy != heavy;
            IsHeavy = heavy;
            ApplyMass();
            if (changed) ModeChanged?.Invoke(heavy);
        }

        private void ApplyMass()
        {
            // Mod değişimi collider'ı büyütmez; böylece duvarın içinde sıkışmayız.
            if (Body != null) Body.mass = IsHeavy ? 8f : 1f;
        }

        public void SetControlsEnabled(bool enabled)
        {
            ControlsEnabled = enabled;
            ClearInput();
            if (Body == null || Body.isKinematic) return;
            Vector3 velocity = Body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            Body.linearVelocity = velocity;
        }

        public void Teleport(Vector3 position)
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            transform.position = position;
            if (Body != null)
            {
                Body.position = position;
                if (!Body.isKinematic)
                {
                    Body.linearVelocity = Vector3.zero;
                    Body.angularVelocity = Vector3.zero;
                    Body.WakeUp();
                }
            }
            ClearInput();
            ClearGround();
            ignoreGroundUntil = Time.fixedTime + Time.fixedDeltaTime;
        }

        private void OnCollisionEnter(Collision collision) => ReadGround(collision);
        private void OnCollisionStay(Collision collision) => ReadGround(collision);
        private void OnCollisionExit(Collision collision) => contacts.Remove(collision.collider);

        private void ReadGround(Collision collision)
        {
            if (Body == null || Time.fixedTime < ignoreGroundUntil || (jumpConsumed && Body.linearVelocity.y > 0.1f))
            {
                contacts.Remove(collision.collider);
                return;
            }
            for (int i = 0; i < collision.contactCount; i++)
            {
                // Yana değen duvarı zemin saymayız.
                if (collision.GetContact(i).normal.y > 0.6f)
                {
                    contacts.Add(collision.collider);
                    return;
                }
            }
            contacts.Remove(collision.collider);
        }

        private static bool Unavailable(Collider value) => value == null || !value.enabled || !value.gameObject.activeInHierarchy;
        private void ClearInput() { moveInput = 0f; jumpUntil = float.NegativeInfinity; }
        private void ClearGround()
        {
            contacts.Clear();
            jumpConsumed = true;
            lastGroundAt = float.NegativeInfinity;
            fallSpeed = 0f;
            wasGrounded = false;
        }
        private void OnDisable() { ClearInput(); ClearGround(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }
        private void OnDestroy()
        {
            if (capsule != null && capsule.sharedMaterial == frictionless) capsule.sharedMaterial = originalMaterial;
            if (frictionless != null) Destroy(frictionless);
        }
    }
}
