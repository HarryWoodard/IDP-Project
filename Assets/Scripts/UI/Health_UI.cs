using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health_UI : MonoBehaviour
{
    [SerializeField] private Image[] hearts; // 1 image per heart
    [SerializeField] private Sprite fullHeart; // Full heart sprite
    [SerializeField] private Sprite halfHeart; // Half heart sprite
    [SerializeField] private Sprite emptyHeart; // Empty heart sprite

    [SerializeField] private int blinkCount = 3; // How many "blinks" during health UI change
    [SerializeField] private float blinkInterval = 0.1f; // How long until the health UI "blinks" on/off

    private Player_Health playerHealth; // Reference to Player_Health component
    private CanvasGroup canvasGroup; // Reference to CanvasGroup component, allows for control over multiple UI elements (i.e. individual hearts are controlled as one)
    private Coroutine blinkRoutine;

    // Awake function retrieves CanvasGroup reference
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start() // Called after Awake function is executed
    {
        playerHealth = FindAnyObjectByType<Player_Health>(); // Searches scene for object that has Player_Health (i.e. the player character)

        SetHearts(playerHealth.CurrentHealth); // Sets health UI to correct amount of hearts immediately
        playerHealth.OnHealthChanged += HandleHealthChanged; // Register HandleHealthChanged to automatically execute when player health changes

    }

    private void OnDestroy()
    {
        if (playerHealth != null) // If player health is something
            playerHealth.OnHealthChanged -= HandleHealthChanged; // Unregister HandleHealthChanged to automatically execute when player health changes
    }

    private void HandleHealthChanged(int current, int max) // Runs every time Player_Health fires OnHealthChanged event
    {
        // In case another hit lands mid-blink, restart the blink coroutine
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(BlinkAndUpdate(current));
    }

    private IEnumerator BlinkAndUpdate(int health) // Coroutine that handles updating the UI
    {
        for (int i = 0; i < blinkCount; i++) // Repeat loop until the number of iterations is not less than the blinkCount
        {
            canvasGroup.alpha = 0f;                   // Health UI = invisible

            yield return new WaitForSeconds(blinkInterval); // Pause for the number of seconds equal to blinkInterval, then continue

            SetHearts(health);                        // update while UI is invisible, so the change appears on reappear

            canvasGroup.alpha = 1f;                   // Health UI = visible

            yield return new WaitForSeconds(blinkInterval); // Pause for the number of seconds equal to blinkInterval, then continue
        }
    }

    private void SetHearts(int health) // Sets if each heart is empty, half or full
    {
        for (int i = 0; i < hearts.Length; i++) // Repeat loop until the number of iterations is not less than the number of hearts
        {
            int points = Mathf.Clamp(health - i * 2, 0, 2); // Each heart 2 points, calculate how much belong to a specific heart

            if (points == 2) hearts[i].sprite = fullHeart; // If heart points = 2, full heart
            else if (points == 1) hearts[i].sprite = halfHeart; // If heart points = 1, half heart
            else hearts[i].sprite = emptyHeart; // If heart points = 2, empty heart
        }
    }
}
