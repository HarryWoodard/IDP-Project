using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Zombie_Player_Awareness))]
public class Zombie_Movement : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 2.5f; // Zombie speed while chasing player
    [SerializeField] private float wanderSpeed = 0.8f; // Zombie speed whilst they aren't aware of the player

    // Vector2(x,y) where x = minimum number of seconds and y = maximum number of seconds
    // Random value is selected between x and y
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(0.3f, 0.7f); // How long each step lasts (between 0.3 and 0.7 seconds)
    [SerializeField] private Vector2 pauseTimeRange = new Vector2(1f, 3f); // How long standing still between steps lasts (between 1 and 3 seconds)


    private Rigidbody2D rb; // Reference to Rigidbody2D component, applies physics to enemy
    private Animator animator; // Reference to Animator component, plays animations
    private Zombie_Player_Awareness awareness; // Reference to Zombie_Player_Awareness component

    private Vector2 moveDirection; // (0,0) means Zombie is standing still
    private Vector2 facingDir = Vector2.down; // facingDir = Which direction the enemy is facing
    private float currentSpeed;
    private float nextWanderChange; // Time when the Zombie should switch from walking to standing still whilst wandering
    private bool wasAware; // Was Zombie chasing player last step?

    // Awake function retrieves Rigidbody2D, Animator and Zombie_Player_Awareness refrence
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        awareness = GetComponent<Zombie_Player_Awareness>();

        // Start with idle animation facing down
        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }

    private void FixedUpdate() // Updates game 50 times per second
    {
        // If aware of player, chase them and if not, wander about
        if (awareness.AwareOfPlayer)
            Chase();
        else
            Wander();

        rb.linearVelocity = moveDirection * currentSpeed; // Applies which direction Zombie is moving in and their current movement speed
        UpdateAnimation();
    }

    private void Chase()
    {
        wasAware = true;
        currentSpeed = chaseSpeed;
        moveDirection = awareness.DirectionToPlayer; // Move directly towards player
    }

    private void Wander()
    {
        currentSpeed = wanderSpeed;

        // Just lost the player, stop and pause before wandering
        if (wasAware) 
        {
            wasAware = false;
            moveDirection = Vector2.zero;
            nextWanderChange = Time.time + Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        }

        if (Time.time < nextWanderChange) return; // Zombie continues whatever they were doing while wandering (walking or standing still)

        if (moveDirection != Vector2.zero)
        {
            // Stand still after finishing step
            moveDirection = Vector2.zero;
            nextWanderChange = Time.time + Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        }

        else 
        {
            // Step in a random direction
            moveDirection = Random.insideUnitCircle.normalized; // .insideUnitCircle.normalized = walk in random direction within a radius
            nextWanderChange = Time.time + Random.Range(wanderTimeRange.x, wanderTimeRange.y);
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

    private void OnCollisionEnter2D(Collision2D collision) // Called when Zombie collides with an object
    {
        // Bumped into a wall while wandering: end the step early
        if (!awareness.AwareOfPlayer && moveDirection != Vector2.zero)        
            nextWanderChange = Time.time;
    }
}
