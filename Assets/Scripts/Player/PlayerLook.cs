using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerLook
 * ----------
 * Handles first-person camera rotation.
 *
 * Horizontal mouse movement rotates the Player root.
 * Vertical mouse movement rotates CameraTarget.
 *
 * Both axes use explicit accumulated rotations so their behaviour
 * remains consistent and predictable.
 */

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;

    [Header("Look")]
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private Vector2 lookInput;

    public float CurrentPitch => pitch;

    private float yaw;
    private float pitch;

    private void Start()
    {
        yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}