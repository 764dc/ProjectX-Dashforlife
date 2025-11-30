using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 15f;
    public float deceleration = 15f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public int extraJumps = 1;
    private int jumpsLeft;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;
    private bool isGrounded;

    [Header("Wall Check")]
    public Transform rightWallCheck;
    public Transform leftWallCheck;
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;

    private bool isTouchingRightWall;
    private bool isTouchingLeftWall;
    private bool isWallSliding;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 1.5f;

    [Header("Wall Jump / Infinite Wave")]
    public Vector2 wallClimbForce = new Vector2(4f, 12f);

    [Header("Ledge Fix")]
    public float chestRayHeight = 0.4f;
    public float ledgeDetectForward = 0.5f;
    public float ledgeDetectUp = 0.9f;
    public float ledgeDetectRadius = 0.16f;
    public float ledgeStepUpVelocity = 8f;

    [Header("Ladder")]
    public LayerMask ladderLayer;
    public float ladderCheckRadius = 0.3f;
    public float climbSpeed = 4f;
    private bool isOnLadder;
    private float climbInput;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.7f;
    private bool isDashing;
    private bool canDash = true;
    private float dashTime;

    private float moveInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = extraJumps;
    }

    void Update()
    {
        if (!isDashing)
            moveInput = Input.GetAxisRaw("Horizontal");

        // ---------- Ground check ----------
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        if (isGrounded)
            jumpsLeft = extraJumps;

        // ---------- Ladder check ----------
        isOnLadder = Physics2D.OverlapCircle(transform.position, ladderCheckRadius, ladderLayer);

        if (isOnLadder)
            rb.gravityScale = 0f;
        else
            rb.gravityScale = 3f;

        climbInput = isOnLadder ? Input.GetAxisRaw("Vertical") : 0;

        // ---------- Jump ----------
        if (Input.GetButtonDown("Jump"))
        {
            if (isOnLadder)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isGrounded)
            {
                Jump();
            }
            else if (jumpsLeft > 0)
            {
                Jump();
                jumpsLeft--;
            }
        }

        // ---------- Wall detection ----------
        isTouchingRightWall = Physics2D.Raycast(rightWallCheck.position, Vector2.right, wallCheckDistance, wallLayer);
        isTouchingLeftWall = Physics2D.Raycast(leftWallCheck.position, Vector2.left, wallCheckDistance, wallLayer);

        // ---------- Wall Slide ----------
        if (!isGrounded && !isOnLadder && (isTouchingRightWall || isTouchingLeftWall) && moveInput != 0)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }

        // ---------- WALL WAVE + BACK JUMP ----------
        if ((isTouchingRightWall || isTouchingLeftWall) && Input.GetButtonDown("Jump"))
        {
            if (isTouchingRightWall)
            {
                rb.linearVelocity = new Vector2(-wallClimbForce.x, wallClimbForce.y);
            }
            else if (isTouchingLeftWall)
            {
                rb.linearVelocity = new Vector2(wallClimbForce.x, wallClimbForce.y);
            }
        }

        // ---------- Fix Ledge ----------
        FixLedgeStuck();

        // ---------- Dash ----------
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
            StartDash();

        if (isDashing)
        {
            Dash();
            return;
        }
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            float targetSpeed = moveInput * moveSpeed;
            float speedDiff = targetSpeed - rb.linearVelocity.x;
            float accelRate = Mathf.Abs(targetSpeed) > 0.1f ? acceleration : deceleration;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x + speedDiff * accelRate * Time.fixedDeltaTime, rb.linearVelocity.y);

            if (isOnLadder)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, climbInput * climbSpeed);
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // ---------- Dash ----------
    void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTime = dashDuration;
    }

    void Dash()
    {
        if (dashTime > 0)
        {
            float dashDir = moveInput;
            if (dashDir == 0) dashDir = Mathf.Sign(transform.localScale.x);

            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0);
            dashTime -= Time.deltaTime;
        }
        else
        {
            isDashing = false;
            Invoke(nameof(ResetDash), dashCooldown);
        }
    }

    void ResetDash()
    {
        canDash = true;
    }

    // ---------- Ledge Fix ----------
    void FixLedgeStuck()
    {
        if (!(isTouchingRightWall || isTouchingLeftWall) || isGrounded) return;

        float side = isTouchingLeftWall ? 1f : -1f;

        Vector2 chestOrigin = (Vector2)transform.position + Vector2.up * chestRayHeight;
        bool strongContact = Physics2D.Raycast(chestOrigin, Vector2.right * side, wallCheckDistance + 0.05f, wallLayer);

        if (!strongContact)
        {
            Vector2 ledgePos = (Vector2)transform.position + new Vector2(side * ledgeDetectForward, ledgeDetectUp);
            bool isLedge = Physics2D.OverlapCircle(ledgePos, ledgeDetectRadius, whatIsGround);

            if (isLedge)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, ledgeStepUpVelocity);
                isWallSliding = false;
            }
        }
    }

    // ---------- Debug ----------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rightWallCheck.position, rightWallCheck.position + Vector3.right * wallCheckDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(leftWallCheck.position, leftWallCheck.position + Vector3.left * wallCheckDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, ladderCheckRadius);

        Gizmos.color = Color.magenta;
        Vector2 dir = Vector2.right;
        if (isTouchingRightWall) dir = Vector2.left;
        Vector2 lp = (Vector2)transform.position + new Vector2(dir.x * ledgeDetectForward, ledgeDetectUp);
        Gizmos.DrawWireSphere(lp, ledgeDetectRadius);
    }
}