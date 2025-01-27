using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NinjaRunner.Player
{

    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private float intialPlayerSpeed = 4f;
        [SerializeField]
        private float maximumPlayerSpeed = 30f;
        [SerializeField]
        private float playerSppedIncreaseRate = 0.1f;
        [SerializeField]
        private float jumpHeight = 1.0f;
        [SerializeField]
        private float intialGravityValue = -9.81f;
        [SerializeField]
        private LayerMask groundLayer;
        [SerializeField]
        private LayerMask turnLayer;
        [SerializeField]
        private LayerMask obstacleLayer;
        [SerializeField]
        private LayerMask collectLayer;
        [SerializeField]
        private AnimationClip slideAnimationClip;
        [SerializeField]
        private AnimationClip jumpAnimationClip;
        [SerializeField]
        private float playerSpeed;
        [SerializeField]
        private float sideMovementSpeed = 4f; // Speed for side movement
        private float horizontalInput; // Store horizontal input

        private float gravity;
        private Vector3 movementDirection = Vector3.forward;
        private Vector3 playerVelocity;

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction slideAction;

        private CharacterController characterController;
        private Animator animator;
        private AudioSource audioSource;

        private int slidingAnimationId;
        private int jumpAnimationId;

        private bool sliding = false;
        private bool jumping = false;

        private int score = 0;

        public float internalLeft = -3.3f;
        public float internalRight = 3.3f;

        [SerializeField]
        private AudioClip coinCollectSound;

        [SerializeField]
        private UnityEvent<Vector3> turnEvent;
        [SerializeField]
        private UnityEvent<int> gameOverEvent;
        [SerializeField]
        private UnityEvent<int> scoreUpdateEvent;


        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            audioSource = GetComponent<AudioSource>();

            slidingAnimationId = Animator.StringToHash("Running Slide");
            jumpAnimationId = Animator.StringToHash("Running Forward Flip");

            turnAction = playerInput.actions["Turn"];
            jumpAction = playerInput.actions["Jump"];
            slideAction = playerInput.actions["Slide"];
            moveAction = playerInput.actions["Move"];
        }

        private void OnEnable()
        {
            turnAction.performed += PlayerTurn;
            slideAction.performed += PlayerSlide;
            jumpAction.performed += PlayerJump;
            moveAction.performed += PlayerMove;
            moveAction.canceled += PlayerMove;
        }

        private void OnDisable()
        {
            turnAction.performed -= PlayerTurn;
            slideAction.performed -= PlayerSlide;
            jumpAction.performed -= PlayerJump;
            moveAction.performed -= PlayerMove;
        }

        private void Start()
        {
            playerSpeed = intialPlayerSpeed;
            gravity = intialGravityValue;
        }

        private void PlayerMove(InputAction.CallbackContext context)
        {
            // Read the horizontal input as a Vector2
            Vector2 input = context.ReadValue<Vector2>();
            horizontalInput = input.x; // Store the x value for sideways movement
        }


        private void PlayerTurn(InputAction.CallbackContext context)
        {
            float turnValue = context.ReadValue<float>();
            Vector3? turnPosition = CheckTurn(context.ReadValue<float>());

            // Debug log to check the turn position
            if (!turnPosition.HasValue)
            {
                Debug.LogWarning("Turn position is null. Game Over.");
                GameOver();
                return;
            }

            Vector3 targetDirection = Quaternion.AngleAxis(90 * turnValue, Vector3.up) * movementDirection;

            turnEvent.Invoke(targetDirection);
            Turn(turnValue, turnPosition.Value);

            // Update movement direction after turning
            movementDirection = transform.forward.normalized;
        }

        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f, turnLayer);
            if (hitColliders.Length != 0)
            {
                Tile tile = hitColliders[0].transform.parent.GetComponent<Tile>();
                TileType type = tile.type;

                if ((type == TileType.LEFT && turnValue == -1) ||
                   (type == TileType.RIGHT && turnValue == 1) ||
                   (type == TileType.SIDEWAYS))
                {
                    return tile.pivot.position;
                }
            }
            return null;
        }

        private void Turn(float turnValue, Vector3 turnPosition)
        {
            Vector3 tempPlayerPostion = new Vector3(turnPosition.x, transform.position.y, turnPosition.z);
            characterController.enabled = false;
            transform.position = tempPlayerPostion;
            characterController.enabled = true;

            Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 90 * turnValue, 0);
            transform.rotation = targetRotation;
            movementDirection = transform.forward.normalized;
        }

        private void PlayerSlide(InputAction.CallbackContext context)
        {
            if (!sliding && IsGrounded())
            {
                StartCoroutine(Slide());
            }
        }

        private IEnumerator Slide()
        {
            sliding = true;
            // Shrink the collider
            Vector3 orginalControllerCenter = characterController.center;
            Vector3 newControllerCenter = orginalControllerCenter;
            characterController.height /= 2;
            newControllerCenter.y -= characterController.height / 2;
            characterController.center = newControllerCenter;
            // Play the sliding animation
            animator.Play(slidingAnimationId);
            yield return new WaitForSeconds(slideAnimationClip.length);
            // Set the charather controller back to normal after sliding.
            characterController.height *= 2;
            characterController.center = orginalControllerCenter;
            sliding = false;
        }

        private void PlayerJump(InputAction.CallbackContext context)
        {
            if (IsGrounded())
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * gravity * -3f);
                characterController.Move(playerVelocity * Time.deltaTime);
                StartCoroutine(Jump());
            }
        }

        private IEnumerator Jump()
        {
            jumping = true;
            animator.Play(jumpAnimationId);
            yield return new WaitForSeconds(jumpAnimationClip.length);
            jumping = false;
        }

        private void Update()
        {
            if (!IsGrounded(20f))
            {
                GameOver();
                return;
            }

            // Gradually increase player speed
            if (playerSpeed < maximumPlayerSpeed)
            {
                playerSpeed += playerSppedIncreaseRate * Time.deltaTime; // Increase speed over time
                gravity = intialGravityValue - playerSpeed;
            }

            characterController.Move(transform.forward * playerSpeed * Time.deltaTime);

            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            playerVelocity.y += gravity * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);

            // Handle side movement
            MoveSideways();

            // Check if the player is within the boundaries
            CheckBoundaries();
        }

        private void MoveSideways()
        {
            if (!characterController.enabled)
            {
                return;
            }
            // Calculate the new position based on horizontal input using the character's right direction
            Vector3 sideMovement = transform.right * horizontalInput * sideMovementSpeed * Time.deltaTime;
            characterController.Move(sideMovement);
        }

        private void CheckBoundaries()
        {
            // Check if the player is outside the level boundaries
            if (transform.position.x < internalLeft || transform.position.x > internalRight)
            {
                //GameOver();
            }
        }

        private bool IsGrounded(float length = 0.2f)
        {
            Vector3 raycastOrginFirst = transform.position;
            raycastOrginFirst.y -= characterController.height / 2f;
            raycastOrginFirst.y += 0.1f;

            Vector3 raycastOrginSecond = raycastOrginFirst;
            raycastOrginFirst -= transform.forward * 0.2f;
            raycastOrginSecond += transform.forward * 0.2f;

            Debug.DrawLine(raycastOrginFirst, Vector3.down, Color.green, 2f);
            Debug.DrawLine(raycastOrginSecond, Vector3.down, Color.red, 2f);


            if (Physics.Raycast(raycastOrginFirst, Vector3.down, out RaycastHit hit, length, groundLayer) ||
                    Physics.Raycast(raycastOrginSecond, Vector3.down, out RaycastHit hit2, length, groundLayer))
            {
                return true;
            }
            return false;
        }

        private void GameOver()
        {
            Debug.Log("Game Over");
            gameOverEvent.Invoke(score);
            gameObject.SetActive(false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
            {
                GameOver();
            }

            //if (((1 << hit.collider.gameObject.layer) & collectLayer) != 0)
            //{
            //    ScoreManager();
            //}
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Coin"))
            {
                ScoreManager();
            }
        }

        public void ScoreManager()
        {
            score++;
            scoreUpdateEvent.Invoke(score);
            audioSource.clip = coinCollectSound;
            audioSource.Play();

        }
    }
}
