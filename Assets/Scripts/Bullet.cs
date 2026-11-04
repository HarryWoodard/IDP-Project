using UnityEngine;

public class Bullet :MonoBehaviour
{
    [SerializeField] private int damage = 1; // Damage of individual bullet

    private bool hasHit; // Has enemy been hit? Prevents bullet from hitting the enemy multiple times in one frame
    private void OnTriggerEnter2D(Collider2D collision) // Called when bullet enters or passes through an object
    {
        if (hasHit) return; // Return from method if bullet had already hit enemy

        Zombie_Health zombieHealth = collision.GetComponent<Zombie_Health>();

        if (zombieHealth) // If object has Zombie_Health script (zombie)
        {
            hasHit = true;
            zombieHealth.TakeDamage(damage);
            Destroy(gameObject); // Destroy Bullet
        }
    }
}
