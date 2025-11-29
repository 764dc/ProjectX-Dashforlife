using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Wall Jump")]
    public float wallJumpForceX = 8f;
    public float wallJumpForceY = 12f;
    public float wallJumpDelay = 1f;

    private Rigidbody2D rb;
    private float moveInput = 0f;
    private bool jumpPressed = false;

    private bool isGrounded = false;
    private bool isTouchingWall = false;
    private float wallTouchTime = 0f;
    private int wallDirection = 0;
    private bool wasTouchingWallLastFrame = false;

    [Header("Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public Transform wallCheck;
    public float wallCheckRadius = 0.2f;
    public LayerMask wallLayer;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
        Jump();
        WallJump();

        // Reset jumpPressed so jump only triggers once
        jumpPressed = false;
    }

    // -------------------------
    // NEW INPUT SYSTEM CALLBACKS
    // -------------------------

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpPressed = true;
    }

    // -------------------------
    // MOVEMENT
    // -------------------------

    void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void Jump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void WallJump()
    {
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);

        // Detect first frame touching wall
        if (isTouchingWall && !wasTouchingWallLastFrame)
        {
            wallTouchTime = Time.time;

            Collider2D wall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
            if (wall != null)
                wallDirection = (wall.transform.position.x < transform.position.x) ? 1 : -1;
        }

        // Only wall jump if delay is finished
        if (isTouchingWall && Time.time - wallTouchTime >= wallJumpDelay)
        {
            if (jumpPressed)
            {
                rb.linearVelocity = new Vector2(wallJumpForceX * wallDirection, wallJumpForceY);
                isTouchingWall = false;
            }
        }

        wasTouchingWallLastFrame = isTouchingWall;
    }


    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}