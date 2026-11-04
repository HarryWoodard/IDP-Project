using System.Collections.Generic;

// Plain data classes. Newtonsoft turns the JSON text into these.
// The field names here are the key names you use in the JSON file.

[System.Serializable]
public class RoomData
{
    public string id;          // unique name, e.g. "entry"
    public int x;              // world cell of the room's BOTTOM-LEFT corner
    public int y;
    public List<string> grid;  // rows of characters, TOP row first
}

[System.Serializable]
public class LevelData
{
    public string name;
    public int seed;           // seeds the random tile variation, so a level always looks the same
    public List<RoomData> rooms;
}

// The characters allowed in a room grid.
public static class LevelLegend
{
    public const char Wall = '#';    // # = Wall
    public const char Floor = '.';   // . = Floor
    public const char Door = 'D';    // D = Door
    public const char Zombie = 'Z';  // Z = Zombie
    public const char Player = 'P';  // P = Player Spawn

    public static bool IsAllowed(char c)
    {
        return c == Wall || c == Floor || c == Door || c == Zombie || c == Player;
    }
}
