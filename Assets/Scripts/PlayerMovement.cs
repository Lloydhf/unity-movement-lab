using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// İlk ders: yalnızca 3D kapsülle, X/Y düzleminde hareket.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpSpeed = 7f;
    [SerializeField] private float fallLimit = -8f;

    private Rigidbody body;
    private Vector3 startPosition;
    private float moveInput;
    private bool jumpRequested;
    private bool resetRequested;
    private readonly HashSet<Collider> groundContacts = new HashSet<Collider>();

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
        body.useGravity = true;
        body.isKinematic = false;
        // Karakter devrilmesin ve ekranın içine/dışına hareket etmesin.
        body.constraints = RigidbodyConstraints.FreezeRotation
                         | RigidbodyConstraints.FreezePositionZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        // Tuşlar her görüntü karesinde okunur.
        Keyboard keyboard = Keyboard.current;
        moveInput = 0f;
        if (keyboard == null || !Application.isFocused) return;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput += 1f;
        if (keyboard.spaceKey.wasPressedThisFrame) jumpRequested = true;
        if (keyboard.rKey.wasPressedThisFrame) resetRequested = true;
    }

    private void FixedUpdate()
    {
        // Fizikle ilgili değişiklikler sabit aralıklı fizik adımında yapılır.
        if (resetRequested || body.position.y < fallLimit)
        {
            body.position = startPosition;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            groundContacts.Clear();
            jumpRequested = false;
            resetRequested = false;
            return;
        }

        Vector3 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        velocity.z = 0f;

        // Yalnızca ayaklarımız bir yüzeye değiyorsa zıpla.
        if (jumpRequested && groundContacts.Count > 0 && velocity.y <= 0.1f)
        {
            velocity.y = jumpSpeed;
            groundContacts.Clear();
        }

        body.linearVelocity = velocity;
        jumpRequested = false;
    }

    private void OnCollisionEnter(Collision collision) => UpdateGroundContact(collision);
    private void OnCollisionStay(Collision collision) => UpdateGroundContact(collision);
    private void OnCollisionExit(Collision collision) => groundContacts.Remove(collision.collider);

    private void UpdateGroundContact(Collision collision)
    {
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

    private void OnDisable()
    {
        groundContacts.Clear();
        moveInput = 0f;
        jumpRequested = false;
        resetRequested = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;
        moveInput = 0f;
        jumpRequested = false;
        resetRequested = false;
    }
}
