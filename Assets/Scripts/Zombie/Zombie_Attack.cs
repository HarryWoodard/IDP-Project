using UnityEngine;

public class Zombie_Attack : MonoBehaviour
{
    [SerializeField] private int damageAmount; // Damage of Zombie's attack

    private void OnTriggerStay2D (Collider2D collision) // Called when Zombie collides with (and stays within) an object 
    {
        if (collision.gameObject.GetComponent<Player_Movement>()) // If object has Player_Movement script (player)
        { 
            var playerHealth = collision.gameObject.GetComponent<Player_Health>(); // playerHealth variable = Player_Health class in Player_Health script

            playerHealth.TakeDamage(damageAmount); // Damage is inflicted upon the player
        } 
    }
}
