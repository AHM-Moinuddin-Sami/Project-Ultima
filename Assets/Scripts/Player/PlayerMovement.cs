using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerMovement
 * --------------
 * Handles first-person CharacterController locomotion.
 *
 * Responsibilities:
 * - accelerated walking and sprinting
 * - forward-only sprinting
 * - smooth ground deceleration
 * - jumping
 * - preserving takeoff momentum while airborne
 * - limited air control
 * - gravity
 *
 * Horizontal velocity is stored independently from input so momentum
 * can continue naturally after jumping.
 */

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController characterController;

    private Vector2 moveInput;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool sprintHeld;
    private bool jumpRequested;

    public bool IsGrounded => characterController.isGrounded;
    public bool IsSprinting => sprintHeld && moveInput.y > 0.1f;
    public float HorizontalSpeed => horizontalVelocity.magnitude;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        bool isGrounded = characterController.isGrounded;

        Vector3 inputDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        // Sprint only while primarily moving forward.
        bool canSprint = sprintHeld && moveInput.y > 0.1f;

        float currentMoveSpeed =
            canSprint ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity =
            inputDirection * currentMoveSpeed;

        if (isGrounded)
        {
            if (inputDirection.sqrMagnitude > 0f)
            {
                // Smoothly accelerate toward the requested movement velocity.
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    acceleration * Time.deltaTime
                );
            }
            else
            {
                // Smoothly slow down when movement input is released.
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    deceleration * Time.deltaTime
                );
            }

            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }

            if (jumpRequested)
            {
                verticalVelocity =
                    Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Preserve takeoff momentum while still allowing limited
            // directional adjustment during the jump.
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                airAcceleration * Time.deltaTime
            );
        }

        jumpRequested = false;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintHeld = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpRequested = true;
        }
    }
}