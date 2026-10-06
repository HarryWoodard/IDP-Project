using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player_Movement))]
public class Player_Shoot : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float fireRate = 4f;       // shots per second
    [SerializeField] private float bulletOffset = 0.5f; // spawn distance in front of the player
    [SerializeField] private float bulletLifetime = 3f;

    private Player_Movement movement;
    private bool fireHeld;
    private float nextFireTime;

    private void Awake()
    {
        movement = GetComponent<Player_Movement>();
    }

    private void Update()
    {
        if (fireHeld && Time.time >= nextFireTime)
        {
            FireBullet();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            fireHeld = true;
            movement.SetFacingLocked(true);
        }
        else if (context.canceled)
        {
            fireHeld = false;
            movement.SetFacingLocked(false);
        }
    }

    private void FireBullet()
    {
        Vector2 dir = movement.FacingDir;
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);

        // Assumes the bullet sprite points up
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * bulletSpeed;
        Destroy(bullet, bulletLifetime);
    }
}