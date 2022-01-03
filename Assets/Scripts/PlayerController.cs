using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    
    [SerializeField] private float movementSpeed;    
    [SerializeField] private float groundCheckRadius;    
    [SerializeField] private Vector2 groundCheckSize;    
    [SerializeField] private float jumpSpeed;    
    [SerializeField] private float maxJumpTime;    
    [SerializeField] private float extraJumpTime;    
    [SerializeField] private int maxJumpCount;    
    [SerializeField] private int extraJumpCount;    
    [SerializeField] private float slopeCheckDistance;    
    [SerializeField] private float maxSlopeAngle;    
    [SerializeField] private Transform groundCheck;    
    [SerializeField]private LayerMask whatIsGround;    
    [SerializeField] private PhysicsMaterial2D noFriction;    
    [SerializeField] private PhysicsMaterial2D fullFriction;
    

    private float xInput;
    private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;

    private int facingDirection = 1;
    private bool isGrounded;
    private bool isOnSlope;
    private bool isJumping;
    private bool isFalling;
    private bool canWalkOnSlope;
    private bool canJump;

    private Vector2 newVelocity;
    private Vector2 newForce;
    private Vector2 capsuleColliderSize;

    private Vector2 slopeNormalPerp;

    private Rigidbody2D rb;
    private CapsuleCollider2D cc;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
        capsuleColliderSize = cc.size;
        extraJumpTime = maxJumpTime;
        extraJumpCount = maxJumpCount;
    }

    private void Update()
    {
        CheckInput();     
    }

    private void FixedUpdate()
    {
        CheckGround();
        SlopeCheck();
        ApplyMovement();
    }

    private void CheckInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        if (xInput == 1 && facingDirection == -1)
        {
            Flip();
        }
        else if (xInput == -1 && facingDirection == 1)
        {
            Flip();
        }

        if (Input.GetButton("Jump")){
            Jump();
        }else if(Input.GetButtonUp("Jump")){
            if (isJumping) {
                newVelocity.Set(rb.velocity.x, 0);
                rb.velocity = newVelocity;
            }
            if(--extraJumpCount > 0){
                canJump = true;
                extraJumpTime = maxJumpTime;
            }
        }

    }
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, whatIsGround);

        if(rb.velocity.y <= 0.0f)
        {
            isJumping = false;
        }

        if(isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle)
        {
            canJump = true;
            extraJumpCount = maxJumpCount;
            extraJumpTime = maxJumpTime;
        }

    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - (Vector3)(new Vector2(0.0f, capsuleColliderSize.y / 2));

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, whatIsGround);
        Debug.DrawRay(checkPos, transform.right, Color.gray);

        if (slopeHitFront)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);

        }
        else if (slopeHitBack)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {           
            slopeSideAngle = 0.0f;
            isOnSlope = false;
        }

    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {      
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, whatIsGround);

        if (hit)
        {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;            

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeDownAngle != lastSlopeAngle)
            {
                isOnSlope = true;
            }                       

            lastSlopeAngle = slopeDownAngle;
           
            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);

        }

        if (slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        if (isOnSlope && canWalkOnSlope && xInput == 0.0f)
        {
            rb.sharedMaterial = fullFriction;
        }
        else
        {
            rb.sharedMaterial = noFriction;
        }
    }
    
    
    private void Jump()
    {
        if(extraJumpCount > 0 && canJump){
            isJumping = true;
            newVelocity.Set(rb.velocity.x, jumpSpeed);
            rb.velocity = newVelocity;
            extraJumpTime -= Time.deltaTime;
            if(extraJumpTime <= 0){
                canJump = false;
                newVelocity.Set(rb.velocity.x, 0);
                rb.velocity = newVelocity;
            }
        }
    }
    
    private void ApplyMovement()
    {
        if (isGrounded && !isOnSlope && !isJumping) //if not on slope
        {
            newVelocity.Set(movementSpeed * xInput, 0.0f);
            rb.velocity = newVelocity;
        }
        else if (isGrounded && isOnSlope && canWalkOnSlope && !isJumping) //If on slope
        {
            newVelocity.Set(movementSpeed * slopeNormalPerp.x * -xInput, movementSpeed * slopeNormalPerp.y * -xInput);
            rb.velocity = newVelocity;
        }
        else if (!isGrounded) //If in air
        {
            newVelocity.Set(movementSpeed * xInput, rb.velocity.y);
            rb.velocity = newVelocity;
        }

    }

    private void Flip()
    {
        facingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }

}