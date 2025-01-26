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
        private AnimationClip slideAnimationClip;
        [SerializeField]
        private AnimationClip jumpAnimationClip;

        private float playerSpeed;
        private float gravity;
        private Vector3 movementDirection = Vector3.forward;
        private Vector3 playerVelocity;

        private PlayerInput playerInput;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction slideAction;

        private CharacterController characterController;
        private Animator animator;

        private int slidingAnimationId;
        private int jumpAnimationId;

        private bool sliding = false;
        private bool jumping = false;

        [SerializeField]
        private UnityEvent<Vector3> turnEvent;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();

            slidingAnimationId = Animator.StringToHash("Running Slide");
            jumpAnimationId = Animator.StringToHash("Running Forward Flip");

            turnAction = playerInput.actions["Turn"];
            jumpAction = playerInput.actions["Jump"];
            slideAction = playerInput.actions["Slide"];
        }

        private void OnEnable()
        {
            turnAction.performed += PlayerTurn;
            slideAction.performed += PlayerSlide;
            jumpAction.performed += PlayerJump;
        }

        private void OnDisable()
        {
            turnAction.performed -= PlayerTurn;
            slideAction.performed -= PlayerSlide;
            jumpAction.performed -= PlayerJump;
        }

        private void Start()
        {
            playerSpeed = intialPlayerSpeed;
            gravity = intialGravityValue;
        }

        private void PlayerTurn(InputAction.CallbackContext context)
        {

            Vector3? turnPosition = CheckTurn(context.ReadValue<float>());
            if (!turnPosition.HasValue)
            {
                GameOver();
                return;
            }

            Vector3 targetDirection = Quaternion.AngleAxis(90 * context.ReadValue<float>(), Vector3.up) *
                movementDirection;

            turnEvent.Invoke(targetDirection);

            Turn(context.ReadValue<float>(), turnPosition.Value);
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
            yield return new WaitForSeconds(jumpAnimationClip.length + 1f);
            jumping = false;
        }

        private void Update()
        {
            if (!IsGrounded(20f))
            {
                GameOver();
                return;
            }

            characterController.Move(transform.forward * playerSpeed * Time.deltaTime);

            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            playerVelocity.y += gravity * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);
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
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
            {
                GameOver();
            }
        }
    }
}
