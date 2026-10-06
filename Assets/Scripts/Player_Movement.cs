using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;

    // Facing is always cardinal. desiredFacing is where the player wants to face;
    // facingDir is where they actually face (frozen while firing).
    private Vector2 facingDir = Vector2.down;
    private Vector2 desiredFacing = Vector2.down;
    private bool facingLocked;

    // Press order of each axis, used to decide facing on diagonals
    private int pressCounter;
    private int lastXOrder;
    private int lastYOrder;

    public Vector2 FacingDir => facingDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = moveInput;
        if (velocity.sqrMagnitude > 1f) velocity.Normalize();

        rb.linearVelocity = velocity * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            moveInput = Vector2.zero;
            animator.SetBool("isWalking", false);
            SetIdleFacing();
            SetWalkingFacing(Vector2.zero);
            return;
        }

        Vector2 newInput = context.ReadValue<Vector2>();
        animator.SetBool("isWalking", true);

        TrackPressOrder(newInput);
        moveInput = newInput;
        desiredFacing = ResolveFacingFromHeld();

        if (!facingLocked)
            facingDir = desiredFacing;

        SetWalkingFacing(facingDir);
    }

    public void SetFacingLocked(bool locked)
    {
        facingLocked = locked;
        if (locked) return;

        // Fire released: turn immediately to the direction currently held
        desiredFacing = ResolveFacingFromHeld();
        facingDir = desiredFacing;

        SetIdleFacing();
        if (moveInput != Vector2.zero)
            SetWalkingFacing(facingDir);
    }

    private void TrackPressOrder(Vector2 newInput)
    {
        bool hasX = !Mathf.Approximately(newInput.x, 0f);
        bool hasY = !Mathf.Approximately(newInput.y, 0f);
        bool hadX = !Mathf.Approximately(moveInput.x, 0f);
        bool hadY = !Mathf.Approximately(moveInput.y, 0f);

        // An axis counts as "pressed" when it starts or reverses direction
        if (hasX && (!hadX || Mathf.Sign(newInput.x) != Mathf.Sign(moveInput.x)))
            lastXOrder = ++pressCounter;
        if (hasY && (!hadY || Mathf.Sign(newInput.y) != Mathf.Sign(moveInput.y)))
            lastYOrder = ++pressCounter;
    }

    private Vector2 ResolveFacingFromHeld()
    {
        bool hasX = !Mathf.Approximately(moveInput.x, 0f);
        bool hasY = !Mathf.Approximately(moveInput.y, 0f);

        if (!hasX && !hasY) return desiredFacing;
        if (hasX && !hasY) return new Vector2(Mathf.Sign(moveInput.x), 0f);
        if (hasY && !hasX) return new Vector2(0f, Mathf.Sign(moveInput.y));

        // Diagonal: face the axis pressed most recently
        return lastYOrder > lastXOrder
            ? new Vector2(0f, Mathf.Sign(moveInput.y))
            : new Vector2(Mathf.Sign(moveInput.x), 0f);
    }

    private void SetWalkingFacing(Vector2 dir)
    {
        animator.SetFloat("InputX", dir.x);
        animator.SetFloat("InputY", dir.y);
    }

    private void SetIdleFacing()
    {
        animator.SetFloat("LastInputX", facingDir.x);
        animator.SetFloat("LastInputY", facingDir.y);
    }
}