using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    // =========================
    // COMPONENTS
    // =========================

    private CharacterController controller;
    private Animator animator;
    private AudioSource audioSource;
    private Camera mainCam;


    // =========================
    // PLAYER DATA
    // =========================

    private int score;

    private float maxHealth;
    private float currentHealth;

    private Vector3 velocity;
    private bool isGrounded;


    // =========================
    // AUDIO
    // =========================

    [Header("Audio Clips")]
    public AudioClip coinCollectSound;
    public AudioClip enemyAttackSound;


    // =========================
    // MOVEMENT SETTINGS
    // =========================

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    private float rotationVelocity;


    // =========================
    // INPUT
    // =========================

    [Header("Key Binds")]
    public InputAction moveaction;
    public InputAction runaction;
    public InputAction jumpaction;


    // =========================
    // GRAVITY & JUMP
    // =========================

    [Header("Gravity & Jump")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpheight = 1.5f;

    [SerializeField] private LayerMask jumpableLayer;

    [SerializeField] private float groundCheckDistance = 0.2f;

    [SerializeField] private Transform groundCheckPoint;


    // =========================
    // UI
    // =========================

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreTextObject;
    [SerializeField] private Image HealthBarFG;


    // =========================
    // GAME MANAGER
    // =========================

    [Header("GameManager")]
    [SerializeField] private GameManager gameManager;


    // =========================
    // ENABLE INPUT
    // =========================

    private void OnEnable()
    {
        moveaction.Enable();
        runaction.Enable();
        jumpaction.Enable();
    }


    // =========================
    // DISABLE INPUT
    // =========================

    private void OnDisable()
    {
        moveaction.Disable();
        runaction.Disable();
        jumpaction.Disable();
    }


    // =========================
    // START
    // =========================

    private void Start()
    {
        // Get Components
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Camera
        mainCam = Camera.main;

        // Score
        score = 0;

        // Health
        maxHealth = 100f;
        currentHealth = maxHealth;

        // Update Health Bar
        UpdateHealthBar();

        // Ground Check
        if (groundCheckPoint == null)
        {
            Debug.LogError(
                "Ground Check Point is NOT assigned in Player Inspector!"
            );
        }

        // Camera Check
        if (mainCam == null)
        {
            Debug.LogError("Main Camera NOT FOUND!");
        }

        // Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        HandleMovement();
    }


    // =========================
    // MOVEMENT
    // =========================

    private void HandleMovement()
    {
        // Safety checks
        if (groundCheckPoint == null)
        {
            return;
        }

        if (mainCam == null)
        {
            return;
        }

        if (controller == null)
        {
            return;
        }


        // =========================
        // GROUND CHECK
        // =========================

        isGrounded = Physics.CheckSphere(
            groundCheckPoint.position,
            groundCheckDistance,
            jumpableLayer
        );


        // =========================
        // KEEP PLAYER ON GROUND
        // =========================

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }


        // =========================
        // MOVEMENT INPUT
        // =========================

        Vector2 input = moveaction.ReadValue<Vector2>();

        Vector3 direction = Vector3.zero;

        if (input.magnitude > 0.1f)
        {
            direction = new Vector3(
                input.x,
                0f,
                input.y
            ).normalized;
        }


        // =========================
        // RUN
        // =========================

        bool isRunning = runaction.IsPressed();

        float currentSpeed = isRunning
            ? runSpeed
            : walkSpeed;


        // =========================
        // MOVE PLAYER
        // =========================

        if (direction != Vector3.zero)
        {
            float targetAngle =
                Mathf.Atan2(
                    direction.x,
                    direction.z
                ) * Mathf.Rad2Deg
                + mainCam.transform.eulerAngles.y;


            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                rotationSmoothTime
            );


            transform.rotation = Quaternion.Euler(
                0f,
                angle,
                0f
            );


            Vector3 moveDirection =
                Quaternion.Euler(
                    0f,
                    targetAngle,
                    0f
                ) * Vector3.forward;


            controller.Move(
                moveDirection.normalized
                * currentSpeed
                * Time.deltaTime
            );
        }


        // =========================
        // ANIMATION
        // =========================

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                direction.magnitude * currentSpeed,
                0.1f,
                Time.deltaTime
            );
        }


        // =========================
        // JUMP
        // =========================

        if (jumpaction.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(
                jumpheight * -2f * gravity
            );

            if (animator != null)
            {
                animator.SetTrigger("jump");
            }
        }


        // =========================
        // GRAVITY
        // =========================

        velocity.y += gravity * Time.deltaTime;


        // Apply gravity
        controller.Move(
            velocity * Time.deltaTime
        );
    }


    // =========================
    // TRIGGER ENTER
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        // =========================
        // GIFT / COIN COLLECTION
        // =========================

        if (other.CompareTag("Coins"))
        {
            score++;

            // Score UI
            if (scoreTextObject != null)
            {
                scoreTextObject.text =
                    "Score: " + score;
            }

            Debug.Log(
                "Gift Collected! Score = " + score
            );


            // Game Win
            if (gameManager != null)
            {
                gameManager.GameWin(score);
            }


            // Coin Sound
            if (
                audioSource != null &&
                coinCollectSound != null
            )
            {
                audioSource.PlayOneShot(
                    coinCollectSound
                );
            }


            // Destroy Gift
            Destroy(other.gameObject);

            return;
        }


        // =========================
        // ENEMY TRIGGER ATTACK
        // =========================

        if (
            other.CompareTag("enemyAi") ||
            other.transform.root.CompareTag("enemyAi")
        )
        {
            TakeDamage(20f);
        }
    }


    // =========================
    // CONTROLLER COLLISION
    // =========================

    private void OnControllerColliderHit(
        ControllerColliderHit hit
    )
    {
        // =========================
        // ENEMY COLLISION ATTACK
        // =========================

        if (
            hit.gameObject.CompareTag("enemyAi") ||
            hit.transform.root.CompareTag("enemyAi")
        )
        {
            TakeDamage(20f);
        }
    }


    // =========================
    // TAKE DAMAGE
    // =========================

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Prevent negative health
        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }


        // Console
        Debug.Log(
            "Enemy attacked! Health = "
            + currentHealth
        );


        // Update health bar
        UpdateHealthBar();


        // Enemy attack sound
        if (
            audioSource != null &&
            enemyAttackSound != null
        )
        {
            audioSource.PlayOneShot(
                enemyAttackSound
            );
        }


        // Player Dead
        if (currentHealth <= 0f)
        {
            Debug.Log("PLAYER DEAD!");

            if (gameManager != null)
            {
                gameManager.GameLose();
            }
        }
    }


    // =========================
    // HEALTH BAR
    // =========================

    private void UpdateHealthBar()
    {
        if (HealthBarFG != null)
        {
            HealthBarFG.fillAmount =
                currentHealth / maxHealth;
        }
    }
}