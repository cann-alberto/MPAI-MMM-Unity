using GLTFast.Schema;
using Mirror;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace ReadyPlayerMe.Samples.QuickStart
{
    [RequireComponent(typeof(CharacterController), typeof(GroundCheck))]
    public class ThirdPersonMovementNetworked : NetworkBehaviour
    {
        private const float TURN_SMOOTH_TIME = 0.05f;

        [SerializeField]
        [Tooltip("Used to determine movement direction based on input and camera forward axis")]
        private Transform playerCamera;
        [SerializeField]
        [Tooltip("Move speed of the character in")]
        private float walkSpeed = 3f;
        [SerializeField]
        [Tooltip("Run speed of the character")]
        private float runSpeed = 8f;
        [SerializeField]
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        private float gravity = -18f;
        [SerializeField]
        [Tooltip("The height the player can jump ")]
        private float jumpHeight = 3f;

        private CharacterController controller;
        private GameObject avatar;

        private float verticalVelocity;
        private float turnSmoothVelocity;

        private bool jumpTrigger;
        public float CurrentMoveSpeed { get; private set; }
        private bool isRunning;

        private GroundCheck groundCheck;        

        private void Start()
        {
            if (!isLocalPlayer) return;
            playerCamera = Camera.main.transform;            
            controller = GetComponent<CharacterController>();
            groundCheck = GetComponent<GroundCheck>();

            var targetCamera = transform.Find("Camera Target");
            var cameraFollow = Camera.main?.GetComponentInParent<CameraFollow>();
            cameraFollow.enabled = true;
            cameraFollow.target = targetCamera.transform;
            cameraFollow.StartFollow();
            var cameraOrbit = Camera.main?.GetComponentInParent<CameraOrbit>();
            cameraOrbit.enabled = true;
            cameraOrbit.playerInput = GetComponent<PlayerInput>();

        }

        public void Setup(GameObject target)
        {
            avatar = target;
            if (playerCamera == null)
            {
                playerCamera = Camera.main.transform;
            }                                  
        }

        public void Move(float inputX, float inputY)
        {
            if (!isLocalPlayer) return;

            var moveDirection = playerCamera.right * inputX + playerCamera.forward * inputY;
            var moveSpeed = isRunning ? runSpeed : walkSpeed;

            JumpAndGravity();
            controller.Move(moveDirection.normalized * (moveSpeed * Time.deltaTime) + new Vector3(0.0f, verticalVelocity * Time.deltaTime, 0.0f));

            var moveMagnitude = moveDirection.magnitude;
            CurrentMoveSpeed = isRunning ? runSpeed * moveMagnitude : walkSpeed * moveMagnitude;

            if (moveMagnitude > 0)
            {
                RotateAvatarTowardsMoveDirection(moveDirection);
            }
        }

        private void RotateAvatarTowardsMoveDirection(Vector3 moveDirection)
        {
            //float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + transform.rotation.y;
            //float angle = Mathf.SmoothDampAngle(avatar.transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, TURN_SMOOTH_TIME);
            //avatar.transform.rotation = Quaternion.Euler(0, angle, 0);

            // Calcola l'angolo di rotazione target
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);

            // Applica la rotazione al padre (con NetworkTransform)
            transform.rotation = targetRotation;
        }

        private void JumpAndGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }

            if (jumpTrigger && controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpTrigger = false;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        public void SetIsRunning(bool running)
        {
            isRunning = running;
        }

        public bool TryJump()
        {
            jumpTrigger = false;
            if (controller.isGrounded)
            {
                jumpTrigger = true;
            }
            return jumpTrigger;
        }

        public bool IsGrounded()
        {
            if (verticalVelocity > 0)
            {
                return false;
            }
            return groundCheck.IsGrounded();
        }
    }
}
