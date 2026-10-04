using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    private Vector2 facingDir = Vector2.down;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 moveVelocity = moveInput;
        if (moveVelocity.sqrMagnitude > 1f)
        {
            moveVelocity.Normalize();
        }

        rb.linearVelocity = moveVelocity * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        Vector2 newInput = context.ReadValue<Vector2>();

        if(context.canceled)
        {
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", facingDir.x);
            animator.SetFloat("LastInputY", facingDir.y);
            moveInput = Vector2.zero;
            animator.SetFloat("InputX", 0f);
            animator.SetFloat("InputY", 0f);
            return;
        }

        animator.SetBool("isWalking", true);

        bool hasX = !Mathf.Approximately(newInput.x, 0f);
        bool hasY = !Mathf.Approximately(newInput.y, 0f);
        bool hadX = !Mathf.Approximately(moveInput.x, 0f);
        bool hadY = !Mathf.Approximately(moveInput.y, 0f);

        if (hasX && !hasY)
        {
            // Only horizontal held
            facingDir = new Vector2(Mathf.Sign(newInput.x), 0f);
        }
        else if (hasY && !hasX)
        {
            // Only vertical held
            facingDir = new Vector2(0f, Mathf.Sign(newInput.y));
        }
        else if (hasX && hasY)
        {
            // Diagonal: face whichever axis was pressed most recently
            if (!hadY)      // Y just got added -> face vertically
                facingDir = new Vector2(0f, Mathf.Sign(newInput.y));
            else if (!hadX) // X just got added -> face horizontally
                facingDir = new Vector2(Mathf.Sign(newInput.x), 0f);
            // otherwise neither is new, so keep the current facing
        }

        moveInput = newInput;

        animator.SetFloat("InputX", facingDir.x);
        animator.SetFloat("InputY", facingDir.y);
    }
}
