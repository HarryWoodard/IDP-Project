using UnityEngine;

// Sends the zombie back to where it spawned, with full health, when the player leaves its room.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Zombie_Health))]
[RequireComponent(typeof(Zombie_Movement))]
public class Zombie_Reset : MonoBehaviour
{
    private Vector3 spawnPosition;    // Zombie spawn position
    private Rigidbody2D rb;           // Reference to Rigidbody2D component
    private Zombie_Health health;     // Reference to Zombie_Health component
    private Zombie_Movement movement; // Reference to Zombie_Movement component

    // Awake function retrieves Rigidbody2D, Zombie_Health, and Zombie_Movement refrence
    private void Awake()
    {
        spawnPosition = transform.position;   // Zombie is at it's spawn point
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Zombie_Health>();
        movement = GetComponent<Zombie_Movement>();
    }

    private void OnEnable() { RoomTransition.RoomEntered += HandleRoomEntered; } // Register HandleRoomEntered to automatically execute when player enters room the zombie is in
    private void OnDisable() { RoomTransition.RoomEntered -= HandleRoomEntered; } // Unregister HandleRoomEntered to automatically execute when player exits room the zombie is in

    private void HandleRoomEntered(PolygonCollider2D newRoom)
    {
        if (newRoom.OverlapPoint(spawnPosition)) return; // If zombie's spawn point is in same room as player, return from method

        // The player has left the room the zombie is in: reset everything
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        health.ResetHealth();
        movement.ResetState();
    }
}