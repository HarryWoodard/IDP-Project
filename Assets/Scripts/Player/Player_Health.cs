using System.Collections;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    [SerializeField] private int currentHealth; // Current health of player
    [SerializeField] private int maxHealth; // Maximum health of player
    [SerializeField] private float invincibilityDuration; // How long the player is invincible for after taking damage
    [SerializeField] private float blinkInterval; // How long until the player sprite "blinks" on/off

    public event System.Action<int, int> OnHealthChanged; // Event that fires when health changes

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public bool IsInvincible {get; private set;} // Is player invincible after taking damage

    private SpriteRenderer spriteRenderer; // Reference to SpriteRenderer component, renders the player sprite

    // Awake function retrieves SpriteRenderer reference
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void TakeDamage(int damageAmount) // Function called when player takes damage
    {
        if (currentHealth == 0) {return;} // Return from method if currentHealth is already 0

        if (IsInvincible) {return;} // Return from method if player is invincible

        currentHealth -= damageAmount; // Subtract currentHealth by the amount of damage the player took

        if (currentHealth < 0) {currentHealth = 0;} // Set currentHealth to 0 if currentHealth is in the negatives

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0) {gameObject.SetActive(false);} // Disable Player object when player's health reaches 0

        else
        {
            StartCoroutine(InvincibilityCoroutine()); // Become invincible and blink
        }
    }

    public void AddHealth (int amountToAdd) // Function called when player restores health
    {
        if (currentHealth == maxHealth) {return;} // Return from method if current player health is already maxed out

        currentHealth += amountToAdd; // Add the amount of health the player restored to currentHealth

        if (currentHealth > maxHealth) {currentHealth = maxHealth;} // Set currentHealth to maxHealth if currentHealth exceeds the maximum player health

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator InvincibilityCoroutine() // Coroutine = code that can pause and continue across multiple frames
    {
        IsInvincible = true; // Player is invincible

        float timeLeft = invincibilityDuration;

        while (timeLeft > 0f) // while invicibilityDuration is bigger than 0
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // flip from visible to invisible or vice-versa

            yield return new WaitForSeconds(blinkInterval); // Pause for the number of seconds equal to blinkInterval, then continue

            timeLeft -= blinkInterval; // Subtract blinkInterval from invicibilityDuration
        }

        IsInvincible = false; // Player is not invincible after coroutine has waited for the allotted seconds
        spriteRenderer.enabled = true; // Ensures player is visible after invincibilty ends
    }

    private void OnDisable() // Called when player object is switched off (esnures player is not permanently invicible or invisible after respawn)
    {
        IsInvincible = false; 
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}
