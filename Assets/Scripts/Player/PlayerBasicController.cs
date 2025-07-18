using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using ZombieGame.Input;
using ZombieGame.Player.Animations;


namespace ZombieGame.Player
{
    public class PlayerBasicController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("Audio")]
        [SerializeField] private AudioClip LandingAudioClip;
        [SerializeField] private AudioClip[] FootstepAudioClips = new AudioClip[10];
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Camera")]
        [Tooltip("The main camera for player mode")]
        [SerializeField] private GameObject playerCamera;
        
        [Header("Ghost Mode")]
        [Tooltip("Ghost mode allows player to pass through colliders")]
        public bool isGhostMode = false;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private ZombieGameInputs _input;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private EyeAnimationHandler _eyeAnimationHandler;

        private PlayerMode _playerMode = PlayerMode.OnFoot;

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<ZombieGameInputs>();

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Get reference to EyeAnimationHandler
            _eyeAnimationHandler = GetComponentInChildren<EyeAnimationHandler>();
            
            // Subscribe to ghost mode toggle event
            _input.OnGhostModeToggleEvent += ToggleGhostMode;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (!isGhostMode)
            {
                JumpAndGravity();
                GroundedCheck();
            }
            else
            {
                // In ghost mode, disable gravity and grounded check
                _verticalVelocity = 0f;
                Grounded = true;
            }
            
            Move();
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }


        private void Move()
        {
            if (_playerMode != PlayerMode.OnFoot) return;

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  playerCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            if (isGhostMode)
            {
                // Ghost mode: move directly through Transform (bypasses colliders)
                transform.position += targetDirection.normalized * (_speed * Time.deltaTime) +
                                     new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
            }
            else
            {
                // Normal mode: use CharacterController (respects colliders)
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (_playerMode != PlayerMode.OnFoot) return;

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (_playerMode != PlayerMode.OnFoot) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (_playerMode != PlayerMode.OnFoot) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnValidate()
        {
            if (playerCamera == null)
            {
                playerCamera = GameObject.Find("PlayerCamera");
            }

            // Set default ground layer if not set
            if (GroundLayers == 0)
            {
                GroundLayers = 1 << LayerMask.NameToLayer("Default");
            }

#if UNITY_EDITOR
            // Set default audio clips if they're not assigned
            if (LandingAudioClip == null)
            {
                LandingAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/StarterAssets/ThirdPersonController/Character/Sfx/Player_Land.wav");
            }

            // Initialize array if needed
            if (FootstepAudioClips == null || FootstepAudioClips.Length != 10)
            {
                FootstepAudioClips = new AudioClip[10];
            }

            // Set default footstep clips if they're not assigned
            for (int i = 0; i < 10; i++)
            {
                if (FootstepAudioClips[i] == null)
                {
                    FootstepAudioClips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/StarterAssets/ThirdPersonController/Character/Sfx/Player_Footstep_{(i + 1):D2}.wav");
                }
            }
#endif
        }

        private void Awake()
        {
            if (playerCamera == null)
            {
                Debug.LogError("Player camera not found, please drag your player camera game object to the Player Camera field");
            }

            if (GetComponent<PlayerLoader>() == null)
            {
                Debug.LogError($"[PlayerBasicController] PlayerLoader component is required on {gameObject.name}! Add PlayerLoader to ensure all required components are properly set up.");
            }
        }

        public void ChangePlayerMode(PlayerMode playerMode)
        {
            _playerMode = playerMode;
        }


        public bool IsOnFoot()
        {
            return _playerMode == PlayerMode.OnFoot;
        }

        public bool IsInVehicle()
        {
            return _playerMode == PlayerMode.InVehicle;
        }

        public GameObject GetPlayerCamera()
        {
            return playerCamera;
        }

        public void ResetPlayerAnimator()
        {
            if (_animator != null)
            {
                // Reset to idle state instead of disabling
                _animator.SetFloat("Speed", 0f);
                _animator.SetBool("Grounded", true);
                _animator.SetBool("Jump", false);
                _animator.SetBool("FreeFall", false);
                // You might need to adjust these parameter names based on your animator
            }
        }

        /// <summary>
        /// Toggle ghost mode on/off
        /// </summary>
        private void ToggleGhostMode()
        {
            isGhostMode = !isGhostMode;
            
            Debug.Log($"Ghost Mode: {(isGhostMode ? "ENABLED" : "DISABLED")} - Press G to toggle");
            
            // Disable/enable the character controller collider
            _controller.enabled = !isGhostMode;
            
            // Visual feedback - change player material transparency or add glow effect
            if (isGhostMode)
            {
                Debug.Log("GHOST MODE ACTIVE: You can now pass through walls!");
            }
            else
            {
                Debug.Log("GHOST MODE DISABLED: Normal collision detection restored.");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_input != null)
            {
                _input.OnGhostModeToggleEvent -= ToggleGhostMode;
            }
        }

        #region Blinking System

        /// <summary>
        /// Trigger a manual blink using the EyeAnimationHandler
        /// </summary>
        public void Blink()
        {
            // if (_eyeAnimationHandler != null)
            // {
            //     _eyeAnimationHandler.Blink();
            // }
            // else
            // {
            //     Debug.LogWarning("EyeAnimationHandler not found! Add EyeAnimationHandler component to player or child object.");
            // }
        }

        #endregion
    }
} 