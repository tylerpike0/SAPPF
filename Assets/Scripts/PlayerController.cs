using System.Linq;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public Controls controls;
    [SerializeField] BoxCollider2D groundDetectionHitbox;
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] Animator animator;

    [SerializeField] float moveAcceleration = 10;
    [SerializeField] float maxSpeed = 1;
    [SerializeField] float jumpStrength = 10;
    [SerializeField] float jumpBuffer = 0.1f;
    [SerializeField] float fastFallAcceleration = -10f;
    [SerializeField] float maxFastFallSpeed = -10f;
    [SerializeField] int maxAirJumps = 2;
    [SerializeField] int airJumps;
    [SerializeField] bool wasOnGound;

    public float timeLastJumpPressed;
    


    private void Awake() {
        controls = new Controls();
        controls.Enable();

        controls.Player.Jump.performed += ctx =>
        {
            if (IsOnGround())
            {
                Jump();
            }
            else if (airJumps > 0)
            {
                AirJump();
                airJumps -= 1;
            }
            else
            {
                timeLastJumpPressed = Time.time;
            }
        };

    }

    private void Update()
    {
        if (timeLastJumpPressed + jumpBuffer >= Time.time && IsOnGround())
        {
            timeLastJumpPressed = -1;
            Jump();
        }

        if (controls.Player.Move.IsPressed())
        {
            Move(moveAcceleration * controls.Player.Move.ReadValue<Vector2>().x * Time.deltaTime);

            if (controls.Player.Move.ReadValue<Vector2>().y < 0 && !IsOnGround())
            {
                FastFall();
            }
        }

        if (!wasOnGound && IsOnGround()) 
        {
            OnLand();
        }


        EndFrame();
    }

    private void EndFrame()
    {
        wasOnGound = IsOnGround();
    }

    private void FastFall()
    {
        float amount = fastFallAcceleration * Time.deltaTime;
        if (!(rb2D.velocity.y * amount > 0 && (rb2D.velocity.y + amount) < maxFastFallSpeed))
        {
            rb2D.velocity += new Vector2(0, amount);
        }
    }

    private void OnLand()
    {
        airJumps = maxAirJumps;
        animator.SetBool("isJumping", false);

    }
    private void Move(float amount)
    {
        if (!(rb2D.velocity.x * amount > 0 && Mathf.Abs(rb2D.velocity.x + amount) > maxSpeed))
        {
            rb2D.velocity += new Vector2(amount, 0);
            Debug.Log("trymover" + amount);
        }
    }

    private void Jump() {
        rb2D.velocity = Vector2.up * jumpStrength + rb2D.velocity * Vector2.right;
        animator.SetBool("isJumping", true);
    }

    private void AirJump()
    {
        Jump();
    }

    private bool IsOnGround()
    {
        return groundDetectionHitbox.IsTouchingLayers(LayerMask.GetMask("Ground"));
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    
}
