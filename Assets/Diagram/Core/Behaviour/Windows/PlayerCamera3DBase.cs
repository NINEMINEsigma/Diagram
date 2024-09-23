using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace Diagram
{
    public class PlayerCamera3DBase : LineBehaviour
    {
        [Header("Player Camera 3D")]
#if ENABLE_INPUT_SYSTEM
        public bool IsResetCoveredInputAction = true;
#endif
        const float k_MouseSensitivityMultiplier = 0.01f;

        public float mouseSensitivity = 100f;

        [SerializeField] private Transform playerBody;
        public Transform PlayerBody
        {
            get
            {
                if (playerBody == null)
                    playerBody = transform.parent;
                return playerBody;
            }
        }

        float xRotation = 0f;
#if ENABLE_INPUT_SYSTEM
        [SerializeField] InputAction movement;
        [SerializeField] InputAction jump;
#endif

        public override void Reset()
        {
            base.Reset();
            if (IsResetCoveredInputAction)
            {
                movement = new InputAction("PlayerMovement", binding: "<Gamepad>/leftStick");
                movement.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/s")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/a")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/d")
                    .With("Right", "<Keyboard>/rightArrow");
                jump = new InputAction("PlayerJump", binding: "<Gamepad>/a");
                jump.AddBinding("<Keyboard>/Space");
            }
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
#if ENABLE_INPUT_SYSTEM

            movement.Enable();
            jump.Enable();
#endif
        }

        private CharacterController characterController;
        public CharacterController MyCharacterController
        {
            get
            {
                if (characterController == null)
                    characterController = PlayerBody.SeekComponent<CharacterController>();
                return characterController;
            }
        }
        public float speed = 12f;
        public float gravity = -10f;
        public float jumpHeight = 2f;

        public Transform groundCheck;
        public float groundDistance = 0.4f;
        public LayerMask groundMask;


        Vector3 velocity;
        bool isGrounded;


        private void LookWithMouseUpdate()
        {
            bool unlockPressed = false, lockPressed = false;

#if ENABLE_INPUT_SYSTEM
            float mouseX = 0, mouseY = 0;

            if (Mouse.current != null)
            {
                var delta = Mouse.current.delta.ReadValue() / 15.0f;
                mouseX += delta.x;
                mouseY += delta.y;
                lockPressed = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame;
            }
            if (Gamepad.current != null)
            {
                var value = Gamepad.current.rightStick.ReadValue() * 2;
                mouseX += value.x;
                mouseY += value.y;
            }
            if (Keyboard.current != null)
            {
                unlockPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }

            mouseX *= mouseSensitivity * k_MouseSensitivityMultiplier;
            mouseY *= mouseSensitivity * k_MouseSensitivityMultiplier;
#else
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * k_MouseSensitivityMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * k_MouseSensitivityMultiplier;

        unlockPressed = Input.GetKeyDown(KeyCode.Escape);
        lockPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif

            if (unlockPressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (lockPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                PlayerBody.Rotate(Vector3.up * mouseX);
            }
        }
        private void PlayerMovementUpdate()
        {
            float x;
            float z;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            var delta = movement.ReadValue<Vector2>();
            x = delta.x;
            z = delta.y;
            jumpPressed = Mathf.Approximately(jump.ReadValue<float>(), 1);
#else
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        jumpPressed = Input.GetButtonDown("Jump");
#endif

            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            Vector3 move = transform.right * x + transform.forward * z;

            MyCharacterController.Move(move * speed * Time.deltaTime);

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;

            MyCharacterController.Move(velocity * Time.deltaTime);
        }

        private void Update()
        {
            LookWithMouseUpdate();
            PlayerMovementUpdate();
        }
    }
}
