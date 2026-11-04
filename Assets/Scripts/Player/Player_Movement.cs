using UnityEngine;
using UnityEngine.InputSystem; // Allows for me to use InputAction.CallbackContext

public class Player_Movement : MonoBehaviour // Moves player and tells the Animator which of the 4 directions to show
{
    [SerializeField] private float moveSpeed = 5f; // How fast the player moves

    private Rigidbody2D rb; // Reference to Rigidbody2D component, applies physics to player
    private Animator animator; // Reference to Animator component, plays animations
    private Vector2 moveInput; // Stores the X and Y input values that allows for the player to move in 8 directions

    // Facing is always cardinbal (one of 4 directions). Never diagonal.
    private Vector2 facingDir = Vector2.down; // facingDir = Which direction the player is facing (frozen while firing)
    private Vector2 desiredFacing = Vector2.down; // desiredFacing = Which direction the player would be facing if they weren't firing 
    private bool facingLocked; // If fire button is being held, the direction the player is facing in won't change

    // Which axis was pressed most recently
    private int pressCounter;
    private int lastXOrder;
    private int lastYOrder;

    public Vector2 FacingDir => facingDir; // Public read-only access to the private facingDir variable

    // Awake function retrieves Rigidbody2d and Animator refrence
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate() // Updates game 50 times per second
    {
        Vector2 velocity = moveInput; // Direction * Speed = Velocity. 
        if (velocity.sqrMagnitude > 1f) velocity.Normalize(); // Prevents player from moving faster while moving diagonally

        rb.linearVelocity = velocity * moveSpeed; // Sets play movement velocity and direction, allowing for the player to move at a constant speed
    }

    public void Move(InputAction.CallbackContext context) // Function that handles player movement inputs
    {
        if (context.canceled) // If movement input key is released
        {
            moveInput = Vector2.zero; // Player stops moving
            animator.SetBool("isWalking", false); // Player is not walking
            SetIdleFacing(); // Plays idle animation in the direction the player was just walking in
            SetWalkingFacing(Vector2.zero);
            return; // End method
        }

        Vector2 newInput = context.ReadValue<Vector2>();
        animator.SetBool("isWalking", true); // Player is walking

        TrackPressOrder(newInput); // Update which key was pressed most recently
        moveInput = newInput;
        desiredFacing = ResolveFacingFromHeld();

        // Only change facing direction if not firing
        if (!facingLocked)
            facingDir = desiredFacing;

        SetWalkingFacing(facingDir);
    }

    public void SetFacingLocked(bool locked) // Called by Player_Shoot when player starts or stops firing
    {
        // If player is firing, end method early
        facingLocked = locked;
        if (locked) return;

        // Fire released: turn immediately to the direction currently being held
        desiredFacing = ResolveFacingFromHeld();
        facingDir = desiredFacing;

        SetIdleFacing();
        if (moveInput != Vector2.zero)
            SetWalkingFacing(facingDir);
    }

    private void TrackPressOrder(Vector2 newInput) // Function that handles which movement input was most recently pressed
    {
        // !Mathf.Approximately is safewast wat to compare float numbers
        bool hasX = !Mathf.Approximately(newInput.x, 0f);
        bool hasY = !Mathf.Approximately(newInput.y, 0f);
        bool hadX = !Mathf.Approximately(moveInput.x, 0f);
        bool hadY = !Mathf.Approximately(moveInput.y, 0f);

        // An axis counts as "pressed" when it starts or flips direction (left -> right)
        if (hasX && (!hadX || Mathf.Sign(newInput.x) != Mathf.Sign(moveInput.x)))
            lastXOrder = ++pressCounter;
        if (hasY && (!hadY || Mathf.Sign(newInput.y) != Mathf.Sign(moveInput.y)))
            lastYOrder = ++pressCounter;
    }

    private Vector2 ResolveFacingFromHeld() // Function that handles which direction the player faces based on movement input keys held
    {
        bool hasX = !Mathf.Approximately(moveInput.x, 0f);
        bool hasY = !Mathf.Approximately(moveInput.y, 0f);

        if (!hasX && !hasY) return desiredFacing; // No keys being held

        if (hasX && !hasY) return new Vector2(Mathf.Sign(moveInput.x), 0f); // x axis
        if (hasY && !hasX) return new Vector2(0f, Mathf.Sign(moveInput.y)); // y axis

        // Diagonal: face the axis pressed most recently
        return lastYOrder > lastXOrder
            ? new Vector2(0f, Mathf.Sign(moveInput.y))
            : new Vector2(Mathf.Sign(moveInput.x), 0f);
    }

    private void SetWalkingFacing(Vector2 dir) // Function that handles which animation plays while the player is walking
    {
        animator.SetFloat("InputX", dir.x);
        animator.SetFloat("InputY", dir.y);
    }

    private void SetIdleFacing() // Function that handles which animation plays while the player isn't walking
    {
        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }
}