using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(StarterAssetsInputs))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FppLongsFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Walking speed in meters per second.")]
        public float MoveSpeed = 4.0f;

        [Tooltip("Sprint speed in meters per second.")]
        public float SprintSpeed = 6.5f;

        [Tooltip("Acceleration and deceleration rate.")]
        public float SpeedChangeRate = 12.0f;

        [Header("Jumping and Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -20.0f;
        public float JumpTimeout = 0.1f;
        public float GroundedStickForce = -2.0f;

        [Header("Ground Check")]
        public bool Grounded = true;
        public float GroundedOffset = -0.12f;
        public float GroundedRadius = 0.35f;
        public LayerMask GroundLayers = ~0;

        [Header("Look")]
        [Tooltip("The target followed by the Cinemachine camera.")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("Mouse/gamepad look sensitivity multiplier.")]
        public float LookSensitivity = 1.0f;

        public float TopClamp = 80.0f;
        public float BottomClamp = -80.0f;
        public bool LockCameraPosition;

        [Header("View Model")]
        [Tooltip("Keeps the FPP_Longs hands/weapon locked to the first-person camera.")]
        public bool AnchorViewModelToCamera = true;

        [Tooltip("Local offset from the camera target to the hands/weapon rig.")]
        public Vector3 ViewModelLocalOffset = new Vector3(0.0f, -1.9f, 0.75f);

        public Vector3 ViewModelGeoRotationOffset = new Vector3(270.0f, 0.0f, 0.0f);
        public Vector3 ViewModelRootRotationOffset = new Vector3(270.0f, 0.0000128689962f, 0.0f);
        public bool HideBodyRenderers = true;

        private const float Threshold = 0.01f;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private float _cinemachineTargetPitch;
        private float _speed;
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private Transform _viewModelGeo;
        private Transform _viewModelRoot;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _viewModelGeo = transform.Find("GEO");
            _viewModelRoot = transform.Find("root");
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
        }

        private void Start()
        {
            _jumpTimeoutDelta = JumpTimeout;
            ConfigureViewModelRenderers();
        }

        private void Update()
        {
            GroundedCheck();
            JumpAndGravity();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
            AnchorViewModel();
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (CinemachineCameraTarget == null || LockCameraPosition)
            {
                return;
            }

            if (_input.look.sqrMagnitude >= Threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                float yawDelta = _input.look.x * LookSensitivity * deltaTimeMultiplier;
                float pitchDelta = _input.look.y * LookSensitivity * deltaTimeMultiplier;

                transform.Rotate(Vector3.up * yawDelta);
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch + pitchDelta, BottomClamp, TopClamp);
            }

            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
        }

        private void AnchorViewModel()
        {
            if (!AnchorViewModelToCamera || CinemachineCameraTarget == null)
            {
                return;
            }

            Transform cameraTarget = CinemachineCameraTarget.transform;
            SetViewModelTransform(_viewModelGeo, cameraTarget, ViewModelGeoRotationOffset);
            SetViewModelTransform(_viewModelRoot, cameraTarget, ViewModelRootRotationOffset);
        }

        private void SetViewModelTransform(Transform viewModelRoot, Transform cameraTarget, Vector3 rotationOffset)
        {
            if (viewModelRoot == null)
            {
                return;
            }

            viewModelRoot.SetPositionAndRotation(
                cameraTarget.TransformPoint(ViewModelLocalOffset),
                cameraTarget.rotation * Quaternion.Euler(rotationOffset));
        }

        private void ConfigureViewModelRenderers()
        {
            if (!HideBodyRenderers || _viewModelGeo == null)
            {
                return;
            }

            Renderer[] renderers = _viewModelGeo.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string rendererName = renderer.gameObject.name.ToLowerInvariant();
                renderer.enabled = rendererName.Contains("arm")
                    || rendererName.Contains("forearm")
                    || rendererName.Contains("hand");
            }
        }

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;
            }

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1.0f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            Vector3 targetDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + Vector3.up * (_verticalVelocity * Time.deltaTime));
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = GroundedStickForce;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2.0f * Gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                _input.jump = false;
            }

            _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360.0f) angle += 360.0f;
            if (angle > 360.0f) angle -= 360.0f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
