using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerAwareness_Controller))]
public class Enemy_Movement : MonoBehaviour
{
    [SerializeField]
    private float chaseSpeed = 2.5f;

    [SerializeField]
    private float wanderSpeed = 0.8f;

    //How long each step lasts
    [SerializeField]
    private Vector2 wanderTimeRange = new Vector2(0.3f, 0.7f);

    //How long standing still between steps lasts
    [SerializeField]
    private Vector2 pauseTimeRange = new Vector2(1f, 3f);


    private Rigidbody2D rb;
    private Animator animator;
    private PlayerAwareness_Controller awareness;

    private Vector2 moveDirection;
    private Vector2 facingDir = Vector2.down;
    private float currentSpeed;
    private float nextWanderChange;
    private bool wasAware;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        awareness = GetComponent<PlayerAwareness_Controller>();

        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }

    private void FixedUpdate()
    {
        if (awareness.AwareOfPlayer)
            Chase();
        else
            Wander();

        rb.linearVelocity = moveDirection * currentSpeed;
        UpdateAnimation();
    }

    private void Chase()
    {
        wasAware = true;
        currentSpeed = chaseSpeed;
        moveDirection = awareness.DirectionToPlayer;
    }

    private void Wander()
    {
        currentSpeed = wanderSpeed;

        //Stand still after losing player
        if (wasAware) 
        {
            wasAware = false;
            moveDirection = Vector2.zero;
            nextWanderChange = Time.time + Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        }

        if (Time.time < nextWanderChange) return;

        if (moveDirection != Vector2.zero)
        {
            // Stand still after finishing step
            moveDirection = Vector2.zero;
            nextWanderChange = Time.time + Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        }

        else 
        {
            // Step in a random direction
            moveDirection = Random.insideUnitCircle.normalized;
            nextWanderChange = Time.time + Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        }
    }

    private void UpdateAnimation()
    {
        bool moving = moveDirection != Vector2.zero;
        animator.SetBool("isWalking", moving);

        if (moving)
        {
            // Convert to a cardinal direction so it matches the 4 animations
            facingDir = Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y)
                ? new Vector2(Mathf.Sign(moveDirection.x), 0f)
                : new Vector2(0f, Mathf.Sign(moveDirection.y));

            animator.SetFloat("InputX", facingDir.x);
            animator.SetFloat("InputY", facingDir.y);
        }

        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bumped into a wall while wandering: end the step early
        if (!awareness.AwareOfPlayer && moveDirection != Vector2.zero)        
            nextWanderChange = Time.time;
    }
}
