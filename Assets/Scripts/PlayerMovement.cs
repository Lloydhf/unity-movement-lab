using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 3D fizik kullanıyoruz; karakter yalnızca ekranın X/Y düzleminde ilerliyor.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpSpeed = 7f;
    [SerializeField] private float fallLimit = -8f;
    [SerializeField, Range(0f, 0.25f)] private float coyoteTime = 0.10f;
    [SerializeField, Range(0f, 0.25f)] private float jumpBufferTime = 0.12f;

    public Rigidbody Body => body;
    public bool IsGrounded => groundContacts.Count > 0 && Time.fixedTime >= ignoreGroundUntil;

    // Açık olduğunda bölüm yöneticisi topu, kapıyı ve karakteri birlikte sıfırlar.
    public bool ManagedRestart { get; set; }
    public event System.Action RestartRequested;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private PhysicsMaterial movementMaterial;
    private PhysicsMaterial originalMaterial;
    private Vector3 startPosition;
    private float moveInput;
    private float jumpBufferedUntil = float.NegativeInfinity;
    private float lastGroundedAt = float.NegativeInfinity;
    private float ignoreGroundUntil;
    private bool jumpConsumed = true;
    private bool resetRequested;
    private bool controlsEnabled = true;
    private readonly HashSet<Collider> groundContacts = new HashSet<Collider>();

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        startPosition = transform.position;
        body.useGravity = true;
        body.isKinematic = false;
        // Karakter devrilmesin ve ekranın içine/dışına hareket etmesin.
        body.constraints = RigidbodyConstraints.FreezeRotation
                         | RigidbodyConstraints.FreezePositionZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Duvara doğru yürürken sürtünme karakteri duvara yapıştırmasın.
        // Bu yalnızca fizik yüzeyidir; karakterin görünen rengini değiştirmez.
        originalMaterial = capsule.sharedMaterial;
        movementMaterial = new PhysicsMaterial("Player - No friction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum,
            hideFlags = HideFlags.DontSave
        };
        capsule.sharedMaterial = movementMaterial;
    }

    private void Update()
    {
        // Tuşlar her görüntü karesinde okunur.
        Keyboard keyboard = Keyboard.current;
        moveInput = 0f;
        if (!controlsEnabled || keyboard == null || (!Application.isFocused && !Application.isBatchMode)) return;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput += 1f;
        // Yere değmeden hemen önce basılan zıplamayı çok kısa süre hatırla.
        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpBufferedUntil = Time.time + Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);
        if (keyboard.rKey.wasPressedThisFrame) resetRequested = true;
    }

    private void FixedUpdate()
    {
        if (body.isKinematic) return;

        // Fizikle ilgili değişiklikler sabit aralıklı fizik adımında yapılır.
        if (resetRequested || (controlsEnabled && body.position.y < fallLimit))
        {
            resetRequested = false;
            if (ManagedRestart && RestartRequested != null)
                RestartRequested.Invoke();
            else
                ResetToSpawn();
            return;
        }

        // Silinmiş veya kapatılmış bir platform yerde sayılmamalı.
        groundContacts.RemoveWhere(IsUnavailable);
        if (IsGrounded)
        {
            lastGroundedAt = Time.fixedTime;
            jumpConsumed = false;
        }

        Vector3 velocity = body.linearVelocity;
        velocity.x = controlsEnabled ? moveInput * moveSpeed : 0f;
        velocity.z = 0f;

        // Kenardan yeni ayrıldıysan küçük bir tolerans var; havada ikinci zıplama yok.
        bool canJump = !jumpConsumed && Time.fixedTime - lastGroundedAt <= coyoteTime;
        if (controlsEnabled && Time.fixedTime <= jumpBufferedUntil && canJump)
        {
            velocity.y = jumpSpeed;
            jumpBufferedUntil = float.NegativeInfinity;
            jumpConsumed = true;
            lastGroundedAt = float.NegativeInfinity;
            // Ayrıldığımız zeminin bir önceki fizik adımındaki temasını kullanma.
            ignoreGroundUntil = Time.fixedTime + 0.06f;
            groundContacts.Clear();
        }

        body.linearVelocity = velocity;
    }

    // Bölüm kurulurken hareket değerlerini ayarlamak için kullanılır.
    // Başlangıç konumu Awake sırasında kaydedilir; nesneyi bileşeni eklemeden önce yerleştir.
    public void Configure(float speed, float jump, float fall)
    {
        moveSpeed = Mathf.Max(0f, speed);
        jumpSpeed = Mathf.Max(0f, jump);
        fallLimit = fall;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        ClearInput();
        if (body != null && !body.isKinematic)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }
    }

    public void ResetToSpawn()
    {
        if (body == null) return;
        body.position = startPosition;
        transform.position = startPosition;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
        ClearInput();
        ClearGround();
        ignoreGroundUntil = Time.fixedTime + Time.fixedDeltaTime;
    }

    private void OnCollisionEnter(Collision collision) => UpdateGroundContact(collision);
    private void OnCollisionStay(Collision collision) => UpdateGroundContact(collision);
    private void OnCollisionExit(Collision collision) => groundContacts.Remove(collision.collider);

    private void UpdateGroundContact(Collision collision)
    {
        if (Time.fixedTime < ignoreGroundUntil || (jumpConsumed && body.linearVelocity.y > 0.1f))
        {
            groundContacts.Remove(collision.collider);
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            // Duvara yandan değmek, yerde olmak sayılmaz.
            if (collision.GetContact(i).normal.y > 0.6f)
            {
                groundContacts.Add(collision.collider);
                return;
            }
        }
        groundContacts.Remove(collision.collider);
    }

    private static bool IsUnavailable(Collider contact)
        => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy;

    private void ClearInput()
    {
        moveInput = 0f;
        jumpBufferedUntil = float.NegativeInfinity;
        resetRequested = false;
    }

    private void ClearGround()
    {
        groundContacts.Clear();
        lastGroundedAt = float.NegativeInfinity;
        jumpConsumed = true;
    }

    private void OnDisable()
    {
        ClearInput();
        ClearGround();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;
        ClearInput();
    }

    private void OnDestroy()
    {
        if (capsule != null && capsule.sharedMaterial == movementMaterial)
            capsule.sharedMaterial = originalMaterial;
        if (movementMaterial != null) Destroy(movementMaterial);
    }
}
