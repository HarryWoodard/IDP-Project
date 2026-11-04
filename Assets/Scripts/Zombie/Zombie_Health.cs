using UnityEngine;
using System.Collections;

public class Zombie_Health : MonoBehaviour
{
    [SerializeField] private int maxHealth; // Maximum health of zombie
    [SerializeField] private float flashDuration; // How long the zombie "flashes" for upon being hit
    [SerializeField] private Material flashMaterial; // Solid-colour material of the "flash"

    private int currentHealth; // Current health of zombie
    private SpriteRenderer spriteRenderer; // Reference to SpriteRenderer component, renders the zombie sprite
    private Material originalMaterial; // The Zombie's original/normal material (not flash material)
    private Coroutine flashRoutine;

    private void Awake() // Awake function retrieves SpriteRenderer reference (and the sprite's colour)
    {
        currentHealth = maxHealth;   // Zombie starts at full health

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMaterial = spriteRenderer.sharedMaterial;
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;   // Return from method if zombie's health is zero or less than zero (i.e. zombie is dying)

        currentHealth -= damageAmount; // Subtract currentHealth by the amount of damage the zombie took

        if (currentHealth <= 0)
        {
            Destroy(gameObject);          // Zombie is destroyed
            return;
        }

        // In case another hit lands mid-flash, restart the flash coroutine
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine() // Coroutine that handles when the zombie "flashes"
    {
        spriteRenderer.sharedMaterial = flashMaterial;      // Flash on

        yield return new WaitForSeconds(flashDuration); // Pause for the number of seconds equal to flashDuration, then continue

        spriteRenderer.sharedMaterial = originalMaterial;   // Flash off
        flashRoutine = null;
    }

    public void ResetHealth() // Function that resets zombie health
    {
        currentHealth = maxHealth; // Fully restores zombie's health

        // Cancel flash, don't restart and revert back to original look
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = null;
        spriteRenderer.sharedMaterial = originalMaterial;
    }
}
