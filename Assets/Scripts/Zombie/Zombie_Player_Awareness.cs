using UnityEngine;
using UnityEngine.Serialization; // needed for FormerlySerializedAs

public class Zombie_Player_Awareness : MonoBehaviour // Zombie awareness of player, does it "see" the player
{
    public bool AwareOfPlayer { get; private set; } // Is Zombie aware of player
    public Vector2 DirectionToPlayer { get; private set; } // Which direction is player in

    // Renamed from _playerAwarenessDistance to match variable naming convention
    // FormelySerializedAs allows the variable to be reset without automtically setting it to 0
    [FormerlySerializedAs("_playerAwarenessDistance")]
    [SerializeField] private float playerAwarenessDistance;

    private Transform player;

    // Awake function searches scene for object that has Player_Movement (i.e. the player character)
    void Awake()
    {
        player = FindAnyObjectByType<Player_Movement>().transform;
    }

    void Update() // Updates game every frame
    {
        // if player is destroyed or disabled (i.e. if the player died), zombie is no longer aware of player
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            AwareOfPlayer = false;
            DirectionToPlayer = Vector2.zero;
            return;
        }

        Vector2 zombieToPlayer = player.position - transform.position; // player.position - transform.position = arrow pointing from Zombie to player
    
        DirectionToPlayer = zombieToPlayer.normalized; // .normalized = same direction. Move in 1 direction towards player

        AwareOfPlayer = zombieToPlayer.magnitude <= playerAwarenessDistance; //.magnitude == the arrow's length (distance between Zombie and player)
    }

    //Draws red circle in scene view when Zombie is selected. Represents Zombie's player awareness radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerAwarenessDistance);

    }
}
