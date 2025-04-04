using UnityEngine;

public class Player_Movement : MonoBehaviour{
    [Header("Components")]
    public Rigidbody2D body;
    public BoxCollider2D groundCheck;
    public LayerMask surfaceMask;  // Una sola màscara per a totes les superfícies
    public Animator animator;


    [Header("Físiques")]
    public float acceleration;
    [Range(0f, 1f)]
    public float groundDecay;



    [Header("Jump i Falling")]
    public float jumpSpeed;
    public float slowMoSalt;
    public float fallMultiplier;
    public float maxFallSpeed;
    public float lastGroundTime;
    public float lastJumpTime;
    public float jumpCoyoteTime;



    [Header("Velocitats")]
    public float maxXSpeed;



    [Header("Bools")]
    public bool isGrounded;
    public bool isGrabbingLedge;
    public bool isTouchingWall;  // Serà actualitzat pel WallCheck
    public bool isWallSliding;   // Serà actualitzat pel WallCheck



    [Header("Ledge Grabing")]
    [SerializeField] Vector2 ledgeGrabingOffset;

    [Header("Inpunts")]
    float xInput;
    float yInput;
    bool jumpPressed;
    
    // Referències als components externs
    private WallCheck wallCheck;

    void Awake()
    {
        // Obtenim la referència al component WallCheck
        wallCheck = GetComponentInChildren<WallCheck>();
        if (wallCheck == null)
        {
            Debug.LogError("No s'ha trobat el component WallCheck. Assegureu-vos que està adjunt a un fill de l'objecte.");
        }
    }

    void Update()
    {
        CheckInput();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleLedgeGrabbing();
        
        if (!isGrabbingLedge) {
            Moviment_Horitzontal();
            HandleFall();
            HandleJumpBuffer();
            HandleJump();
            Aplicar_Friccio();
            HandleSlowMoJump();
        }
    }

    void CheckInput()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");

        // Detectar el botó de salt en Update per no perdre l'input
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            lastJumpTime = Time.time;
        }
    }

    void CheckGround()
    {
        // Comprovació de terra mitjançant el BoxCollider2D
        isGrounded = Physics2D.OverlapAreaAll(groundCheck.bounds.min, groundCheck.bounds.max, surfaceMask).Length > 0;
    }

    void HandleLedgeGrabbing()
    {
        if (isGrabbingLedge)
        {
            // Mantenim la velocitat zero mentre estem agafats
            body.linearVelocity = Vector2.zero;
            
            // Processar la lògica de salt des de la paret
            if (jumpPressed)
            {
                // Desenganxar-se de la paret
                ReleaseLedge();
                
                // Si s'està prement una direcció, saltar en aquesta direcció
                if (Mathf.Abs(xInput) > 0)
                {
                    body.linearVelocity = new Vector2(xInput * maxXSpeed, jumpSpeed);
                }
                else
                {
                    // Si no es prem cap direcció, saltar cap amunt
                    body.linearVelocity = new Vector2(0, jumpSpeed);
                }
                
                // Reiniciar la bandera de salt
                jumpPressed = false;
            }
        }
    }

    void Moviment_Horitzontal()
    {
        if (Mathf.Abs(xInput) > 0)
        {
            float increment = xInput * acceleration;
            float newSpeed = Mathf.Clamp(body.linearVelocity.x + increment, -maxXSpeed, maxXSpeed);
            body.linearVelocity = new Vector2(xInput * maxXSpeed, body.linearVelocity.y);

            Canvi_Direccio_Canvas();
        }
    }

    void Canvi_Direccio_Canvas()
    {
        float direction = Mathf.Sign(xInput);
        transform.localScale = new Vector3(direction, 1, 1);
    }

    void HandleJump()
    {
        //Si està al terra o agafat a una paret pot saltar
        if (jumpPressed && (isGrounded || isTouchingWall))
        {
            if (isGrounded)
            {
                // Salt normal des del terra
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            }
            else if (isTouchingWall)
            {
                // Salt des de la paret (wall jump)
                float wallJumpDirection = wallCheck.GetWallJumpDirection();
                body.linearVelocity = new Vector2(wallJumpDirection * maxXSpeed * 0.75f, jumpSpeed);
            }
            
            // Reiniciar la bandera de salt
            jumpPressed = false;
        }
    }

    void HandleFall()
    {
        //En el seu punt màxim de la Y (salt) - Si està caient, aplicar una gravetat extra i limitar la velocitat de caiguda
        if (body.linearVelocity.y < 0 && !isWallSliding)
        {
            body.linearVelocity += Vector2.down * fallMultiplier * Time.deltaTime;
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(body.linearVelocity.y, -maxFallSpeed));
        }
    }

    void HandleSlowMoJump()
    {
        if (!isGrounded && Mathf.Abs(body.linearVelocity.y) < 0.1f && Mathf.Abs(body.linearVelocity.y) > 0.1f)
        {
            body.gravityScale = slowMoSalt;
        }
        else
        {
            body.gravityScale = 1f;
        }
    }

    void HandleJumpBuffer()
    {
        if (isGrounded && lastJumpTime > 0 && Time.time - lastJumpTime <= lastGroundTime)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            lastJumpTime = -1f; // Reiniciem el buffer després del salt
        }
    }

    void Aplicar_Friccio()
    {
        if (isGrounded && xInput == 0 && body.linearVelocity.y <= 0)
        {
            body.linearVelocity *= groundDecay;
        }
    }

    public void GrabLedge(Vector2 ledgePosition, bool changePosition = true)
    {
        isGrabbingLedge = true;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 0;

        if (changePosition)
        {
            transform.position = new Vector2(ledgePosition.x, ledgePosition.y - ledgeGrabingOffset.y);
        }
    }

    public void ReleaseLedge()
    {
        isGrabbingLedge = false;
        body.gravityScale = 1;
    }

    // Mètode públic per actualitzar l'estat del wallSliding des del WallCheck
    public void SetWallSliding(bool sliding)
    {
        isWallSliding = sliding;
    }

    void UpdateAnimations()
    {
        animator.SetBool("isRunning", Mathf.Abs(xInput) > 0 && isGrounded);
        animator.SetBool("isJumping", !isGrounded && body.linearVelocity.y > 0);
        animator.SetBool("isFalling", !isGrounded && body.linearVelocity.y < 0);
        animator.SetBool("isGrabbingLedge", isGrabbingLedge);
        animator.SetBool("isWallSliding", isWallSliding);
    }

    // Útil per visualitzar els raycasts i el groundCheck en l'Scene View
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.bounds.center, groundCheck.bounds.size);
        }
    }
}