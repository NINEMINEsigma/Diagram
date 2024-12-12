using Diagram.Arithmetic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace Diagram
{
    public class PlayerCamera3DBase : LineBehaviour
    {
        public Timer.ITimer MyTimer = new Timer.UnityTime();

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
            ResetTimer();
        }

        [_Note_("<target> Reset")]
        public virtual void ResetTimer()
        {
            MyTimer = new Timer.UnityTime();
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
        //[Tooltip("<target> { Lateral movement speed , Jumping strength, Forward speed}")]
        //public Vector3 speed = new(0.7f, 2f, 1f);
        [Tooltip("<target> Forward speed")]
        public float dashSpeedMultiplier = 1.7f;
        [Tooltip("<target> Y jump")]
        public float JumpIntensity = 2f;
        [Tooltip("<target> PlayerMovementUpdate")]
        public float gravity = -9.8f;

        [Tooltip("<foot> must set")]
        public Transform groundCheck;
        [Tooltip("<target> PlayerMovementUpdate")]
        public float groundDistance = 0.4f;
        public LayerMask groundMask;


        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] Vector3 velocity = Vector3.zero;
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] Vector3 acceleration = Vector3.zero;
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] Vector3 decayAcceleration = new(1000f, 0.78f, 2000f);
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] Vector3 maxVelocity = new(3, 10, 5);
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] Vector3 maxAcceleration = new(50, 10, 50);
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] float maxVelocityScalar = 10;
        [SerializeField, Tooltip("<target> PlayerMovementUpdate")] bool isGrounded = false;
        [Tooltip("<target> PlayerMovementUpdate")] public bool isDash = false;
        
        [Tooltip("<target> PlayerMovementUpdate")] public bool isLockMovement = false;
        [Tooltip("<target> MouseMovement")] public bool isLockMouseLock = false;

        [_Note_("<target> PlayerMovementUpdate\n<Functional>  this.velocity.y = DefaultGoundVelocity;")]
        public const float DefaultGoundVelocity = -2f;


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
            mouseX = 0;
            mouseY = 0;

            if (isLockMouseLock)
                return;

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
            if (isLockMovement)
                return;

            float x;
            float z;
            float jumpPressed = 0;
            Vector3 lastVelocity = this.velocity;

#if ENABLE_INPUT_SYSTEM
            var delta = movement.ReadValue<Vector2>();
            x = delta.x;
            z = delta.y;
            jumpPressed = jump.ReadValue<float>();
#else
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        jumpPressed = Input.GetButtonDown("Jump")?1:0;
#endif
            bool isJump = !Mathf.Approximately(jumpPressed, 0);

            this.isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            if (this.isGrounded && isJump == false && velocity.y < 0)
            {
                this.velocity.y = DefaultGoundVelocity;
                this.acceleration.y = Mathf.Clamp(this.acceleration.y, 0, Mathf.Infinity);
            }
            if (this.isGrounded && isJump == false && velocity.y > 0)
            {
                this.acceleration.y = Mathf.Clamp(this.acceleration.y, Mathf.NegativeInfinity, 0);
            }

            // Self Acceleration Update
            this.acceleration +=
                // The acceleration of lateral movement
                //PlayerHead.right * x +
                // The acceleration of forward movement
                //PlayerHead.forward * z * (this.isDash ? this.dashSpeedMultiplier : 1)+
                // Placeholders
                Vector3.zero;
            // Limit Acceleration
            this.acceleration.x = Mathf.Clamp(this.acceleration.x, -this.maxAcceleration.x, this.maxAcceleration.x);
            this.acceleration.y = Mathf.Clamp(this.acceleration.y, -this.maxAcceleration.y, this.maxAcceleration.y);
            this.acceleration.z = Mathf.Clamp(this.acceleration.z, -this.maxAcceleration.z, this.maxAcceleration.z);
            // Self Velocity Update
            float deltaTime = this.MyTimer.deltaTime;
            this.velocity +=
                // From Acceleration * Timer Config
                this.acceleration * deltaTime +
                // Longitudinal velocity affected by gravity and jumps
                (this.isGrounded ? (jumpPressed * this.JumpIntensity * Vector3.up) : Vector3.zero) + deltaTime * this.gravity * Vector3.up +
                // The velocity of lateral movement
                (PlayerHead.right).SetY(0).normalized * x +
                // The velocity of forward movement
                (this.isDash ? this.dashSpeedMultiplier : 1) * z * (PlayerHead.forward).SetY(0).normalized +
                // Placeholders
                Vector3.zero;
            // Limit Velocity
            this.velocity.x = Mathf.Clamp(this.velocity.x, -this.maxVelocity.x, this.maxVelocity.x);
            this.velocity.y = Mathf.Clamp(this.velocity.y, -this.maxVelocity.y, this.maxVelocity.y);
            this.velocity.z = Mathf.Clamp(this.velocity.z, -this.maxVelocity.z, this.maxVelocity.z);
            this.velocity = Vector3.ClampMagnitude(this.velocity, this.maxVelocityScalar);
            // Tranform
            MyCharacterController.Move(this.velocity * deltaTime);
            // Decay Acceleration (Simulate Resistance)
            this.acceleration.x += this.decayAcceleration.x * -this.velocity.x * deltaTime;
            this.acceleration.z += this.decayAcceleration.z * -this.velocity.z * deltaTime;
            if (Mathf.Approximately(x, 0) && lastVelocity.x * this.velocity.x < 0) 
            {
                this.acceleration.x = 0;
                this.velocity.x = 0;
            }
            if (Mathf.Approximately(z, 0) && lastVelocity.z * this.velocity.z < 0) 
            {
                this.acceleration.z = 0;
                this.velocity.z = 0;
            }
        }

        private void Update()
        {
            LookWithMouseUpdate();
            PlayerMovementUpdate();
        }
    }
}
