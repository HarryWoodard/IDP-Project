using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour
{
    private enum Axis { Horizontal, Vertical }

    [SerializeField] private Axis axis = Axis.Horizontal;
    [SerializeField] private PolygonCollider2D roomNegative; // left / below the trigger
    [SerializeField] private PolygonCollider2D roomPositive; // right / above the trigger
    [SerializeField] private float exitOffset = 1f;

    private CinemachineConfiner2D confiner;
    private CinemachineCamera vcam;
    private Collider2D triggerCollider;

    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        vcam = confiner.GetComponent<CinemachineCamera>();
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Vector3 playerPos = collision.transform.position;
        Vector3 center = triggerCollider.bounds.center;

        // The player is heading to the opposite side from where they entered
        bool goingPositive = axis == Axis.Horizontal
            ? playerPos.x < center.x
            : playerPos.y < center.y;

        confiner.BoundingShape2D = goingPositive ? roomPositive : roomNegative;
        confiner.InvalidateBoundingShapeCache();

        Vector3 delta = MovePlayerPastTrigger(collision, goingPositive);

        // Snap the camera instead of letting it glide
        vcam.OnTargetObjectWarped(collision.transform, delta);
        vcam.PreviousStateIsValid = false;
    }

    private Vector3 MovePlayerPastTrigger(Collider2D player, bool goingPositive)
    {
        Bounds b = triggerCollider.bounds;
        Vector3 oldPos = player.transform.position;
        Vector3 newPos = oldPos;

        if (axis == Axis.Horizontal)
            newPos.x = goingPositive ? b.max.x + exitOffset : b.min.x - exitOffset;
        else
            newPos.y = goingPositive ? b.max.y + exitOffset : b.min.y - exitOffset;

        if (player.attachedRigidbody != null)
            player.attachedRigidbody.position = newPos;
        player.transform.position = newPos;

        return newPos - oldPos;
    }
}
