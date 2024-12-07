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
        [Tooltip("<target> Reset")]
        public bool IsResetCoveredInputAction = true;
#endif
        const float k_MouseSensitivityMultiplier = 0.01f;

        [Tooltip("<target> LookWithMouseUpdate")]
        public float mouseSensitivity = 100f;

        [Tooltip("<player body> set default = transform.parent\n<target> PlayerBody")]
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
        [Tooltip("<player head> set default = transform\n<target> PlayerHead")]
        [SerializeField] private Transform playerHead;
        public Transform PlayerHead
        {
            get
            {
                if (playerHead == null)
                    playerHead = transform;
                return playerHead;
            }
        }

        float xRotation = 0f;
#if ENABLE_INPUT_SYSTEM
        public InputAction movement;
        public InputAction jump;
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

        [Tooltip("<character controller> set default = PlayerBody.SeekComponent<CharacterController>()\n<target> MyCharacterController")]
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
        [Tooltip("<target> { Lateral movement speed , Jumping strength, Forward speed}")]
        public Vector3 speed = new(0.7f, 2f, 1f);
        [Tooltip("<target> Forward speed")]
        public float dashSpeedMultiplier = 1.7f;
        public float gravity = -9.8f;

        [Tooltip("<foot> must set")]
        public Transform groundCheck;
        [Tooltip("<target> PlayerMovementUpdate")]
        public float groundDistance = 0.4f;
        public LayerMask groundMask;


#if UNITY_EDITOR
        public Vector3 velocity;
        public bool isGrounded;
#else
        Vector3 velocity;
        bool isGrounded;
#endif


        private void LookWithMouseUpdate()
        {
            MouseMovement(out var unlockPressed, out var lockPressed, out var mouseX, out var mouseY);
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
                BodyAndHeadRotating(mouseX, mouseY);
            }
        }

        private void BodyAndHeadRotating(float x, float y)
        {
            xRotation -= y;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            PlayerHead.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            PlayerBody.Rotate(Vector3.up * x);
        }

        private void MouseMovement(out bool unlockPressed, out bool lockPressed, out float mouseX, out float mouseY)
        {
            unlockPressed = false;
            lockPressed = false;
#if ENABLE_INPUT_SYSTEM
            mouseX = 0;
            mouseY = 0;
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
            Vector3 planeSpeed = speed;
            float jumpHeight = planeSpeed.y;
            planeSpeed.y = 0;

            MyCharacterController.Move(Time.deltaTime * planeSpeed.Merge(move, EasyVec.MutiType.Muti));
            if (Mathf.Approximately(x, 0))
            {
                velocity.x -= velocity.x / 2;
            }
            if (Mathf.Approximately(z, 0))
            {
                velocity.z -= velocity.z / 2;
            }

            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;

            MyCharacterController.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
        }

        private void Update()
        {
            LookWithMouseUpdate();
            PlayerMovementUpdate();
        }
    }
}
