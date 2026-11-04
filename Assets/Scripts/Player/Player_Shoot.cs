using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player_Movement))]
public class Player_Shoot : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab; // Bullet template
    [SerializeField] private float bulletSpeed = 10f; // How fast the bullet moves
    [SerializeField] private float fireRate = 4f;       // Shots per second
    [SerializeField] private float bulletOffset = 0.5f; // Spawn distance in front of the player
    [SerializeField] private float bulletLifetime = 3f; // Seconds before a bullet is deleted/disappears

    private Player_Movement movement; // Reference to Player_Movement component
    private bool fireHeld; // Is fire button being held down
    private float nextFireTime; // Earliest time the next shot can be fired

    //Awake function retrieves Player_Movement refrence
    private void Awake()
    {
        movement = GetComponent<Player_Movement>();
    }

    private void Update() // Updates game every frame
    {
        if (fireHeld && Time.time >= nextFireTime) // Time.time = Seconds since game started
        {
            FireBullet();
            nextFireTime = Time.time + 1f / fireRate; // 4 shots per second. 0.25 second gap
        }
    }

    public void Fire(InputAction.CallbackContext context) // Called if fire button is pressed or released
    {
        if (context.started) // If fire button is pressed
        {
            fireHeld = true; // Fire button is being held
            movement.SetFacingLocked(true); // Lock player facing direction
        }
        else if (context.canceled) // If fire button is released
        {
            fireHeld = false; // Fire button is not being held
            movement.SetFacingLocked(false); // Don't lock player facing direction
        }
    }

    private void FireBullet()
    {
        Vector2 dir = movement.FacingDir;

        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset); // Bullet spawns slightly in front of player

        // Assumes the bullet sprite points up
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        // Instantiate = duplicate bullet prefab, Quaternion.Euler(0,0,angle) = rotate around z-axis
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;
        Destroy(bullet, bulletLifetime); // Delete bullet when the bullet's lifetime is up 
    }
}