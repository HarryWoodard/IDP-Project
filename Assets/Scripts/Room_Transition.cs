using Unity.Cinemachine;
using UnityEngine;

// Put on an invisible trigger in a doorway between two rooms.
// When the player enters: switch the camera boundary, move the player to the far side, snap the camera.
// LevelBuilder creates these automatically and calls Configure(); you can still set them by hand in the Inspector.
[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour // Handles what happens when player exits a room and triggers a room transition
{
    private enum Axis { Horizontal, Vertical } // Will player be leaving room horizontally or vertically?

    [SerializeField] private Axis axis = Axis.Horizontal; 
    [SerializeField] private PolygonCollider2D roomNegative; // Left/Below the trigger
    [SerializeField] private PolygonCollider2D roomPositive; // Right/Above the trigger
    [SerializeField] private float exitOffset = 1f; // How far past the trigger to push the player

    private CinemachineConfiner2D confiner; // Keeps camera held within a boundary shape
    private CinemachineCamera vcam;
    private Collider2D triggerCollider;

    // Called by LevelBuilder right after it adds this component.
    public void Configure(bool horizontal, PolygonCollider2D negativeRoom, PolygonCollider2D positiveRoom)
    {
        axis = horizontal ? Axis.Horizontal : Axis.Vertical;
        roomNegative = negativeRoom;
        roomPositive = positiveRoom;
    }

    public static event System.Action<PolygonCollider2D> RoomEntered; // Event fired when player passes through a door.

    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        vcam = confiner.GetComponent<CinemachineCamera>();
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision) // Called when player collides with room transition trigger
    {
        if (!collision.CompareTag("Player")) return; // Ignore everything apart from the player

        Vector3 playerPos = collision.transform.position;
        Vector3 center = triggerCollider.bounds.center;

        // The player is heading to the opposite side from where they entered
        bool goingPositive = axis == Axis.Horizontal
            ? playerPos.x < center.x
            : playerPos.y < center.y;

        // When player enters new room, camera recalculates and adheres to the new rooms camera boundary
        confiner.BoundingShape2D = goingPositive ? roomPositive : roomNegative;
        confiner.InvalidateBoundingShapeCache();

        RoomEntered?.Invoke(goingPositive ? roomPositive : roomNegative);

        Vector3 delta = MovePlayerPastTrigger(collision, goingPositive);

        // Snap the camera instead of letting it glide
        vcam.OnTargetObjectWarped(collision.transform, delta);
        vcam.PreviousStateIsValid = false;
    }

    private Vector3 MovePlayerPastTrigger(Collider2D player, bool goingPositive) // Moves player ahead of room transition trigger so they don't trigger it again
    {
        Bounds b = triggerCollider.bounds; //b.min = bottom-left corner, b.max = top-right
        Vector3 oldPos = player.transform.position;
        Vector3 newPos = oldPos;

        // Change axis room transition trigger is on, other axis doesn't move
        if (axis == Axis.Horizontal)
            newPos.x = goingPositive ? b.max.x + exitOffset : b.min.x - exitOffset;
        else
            newPos.y = goingPositive ? b.max.y + exitOffset : b.min.y - exitOffset;

        if (player.attachedRigidbody != null)
            player.attachedRigidbody.position = newPos;
        player.transform.position = newPos;

        return newPos - oldPos; // newPos - oldPos = distance moved
    }
}
