using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerMovement
 * --------------
 * Handles first-person movement using Unity's CharacterController.
 *
 * Responsibilities:
 * - responsive grounded movement
 * - walking and sprinting
 * - preserve horizontal momentum during jumps
 * - allow limited air control
 * - support stronger sprint-jump momentum
 * - apply gravity
 * - handle jumping
 *
 * Sprinting changes the grounded movement speed.
 * When jumping, the current horizontal velocity is preserved, so sprinting
 * naturally produces a faster and longer jump without needing a separate
 * artificial sprint-jump force.
 */

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

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

        bool canSprint = sprintHeld && moveInput.y > 0.1f;

        float currentMoveSpeed = canSprint ? sprintSpeed : moveSpeed;

        if (isGrounded)
        {
            // Ground movement directly follows player input.
            horizontalVelocity = inputDirection * currentMoveSpeed;

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
            /*
             * Preserve the velocity we had when leaving the ground.
             *
             * Input can gradually influence airborne movement, but it does
             * not instantly replace the jump's existing momentum.
             */
            Vector3 targetVelocity =
                inputDirection * currentMoveSpeed;

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