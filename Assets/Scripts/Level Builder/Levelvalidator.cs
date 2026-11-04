using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Rules the level must obey. Editable in the LevelBuilder Inspector.
[System.Serializable]
public class LevelValidationSettings
{
    public float startSafeRadius = 7f;     // Zombie cannot be within 7 cells of player spawn
    public float landingSafeRadius = 4f;   // Zombie cannot be within 4 cells of player position after entering a room
    public int maxZombiesPerRoom = 8;      // Maximum number of zombies per room
    public int maxZombiesTotal = 40;       // Maximum number of zombie per level
    public int minRoomWidth = 12;          // smaller rooms only give a warning
    public int minRoomHeight = 8;
}

// One doorway between two rooms.
public class DoorInfo
{
    public RectInt cells;        // bounding box of the 'D' cells, in world cells
    public bool horizontal;      // true = rooms are left/right of each other, false = below/above
    public int negativeRoom;     // room index on the left (horizontal) or below (vertical)
    public int positiveRoom;     // room index on the right (horizontal) or above (vertical)
}

public class LevelValidationResult
{
    public readonly List<string> errors = new List<string>();
    public readonly List<string> warnings = new List<string>();
    public readonly List<DoorInfo> doors = new List<DoorInfo>();
    public readonly List<Vector2Int> zombieCells = new List<Vector2Int>();
    public Vector2Int playerCell;
    public int playerRoom;

    public bool IsValid => errors.Count == 0;

    public string BuildReport(string levelName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LEVEL VALIDATION REPORT: " + levelName);
        sb.AppendLine("Errors: " + errors.Count + ", Warnings: " + warnings.Count);
        foreach (string e in errors) sb.AppendLine("ERROR: " + e);
        foreach (string w in warnings) sb.AppendLine("WARNING: " + w);
        return sb.ToString();
    }
}

// Checks a level against the rules and collects what the builder needs (doors, player, zombies).
public static class LevelValidator
{
    private struct Cell { public int room; public char ch; }

    private static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public static LevelValidationResult Validate(LevelData level, LevelValidationSettings s)
    {
        var result = new LevelValidationResult();
        if (level == null || level.rooms == null || level.rooms.Count == 0)
        {
            result.errors.Add("The level has no rooms.");
            return result;
        }

        var cells = new Dictionary<Vector2Int, Cell>();   // every cell in the world: which room, which character
        if (!CheckRooms(level, s, result, cells)) return result;   // later checks need clean rooms

        CheckDoors(cells, result, out List<Vector2Int> landingCells);
        CheckReachability(level, cells, result);
        CheckFairness(s, result, landingCells);
        return result;
    }

    // Room shapes, characters, enclosure, overlaps, player count, zombie counts.
    private static bool CheckRooms(LevelData level, LevelValidationSettings s, LevelValidationResult result, Dictionary<Vector2Int, Cell> cells)
    {
        var ids = new HashSet<string>();
        int playerCount = 0;
        int totalZombies = 0;

        for (int ri = 0; ri < level.rooms.Count; ri++)
        {
            RoomData room = level.rooms[ri];
            string label = string.IsNullOrEmpty(room.id) ? "Room #" + ri : "Room '" + room.id + "'";

            if (string.IsNullOrEmpty(room.id)) result.errors.Add(label + " has no id.");
            else if (!ids.Add(room.id)) result.errors.Add("Duplicate room id '" + room.id + "'.");

            if (room.grid == null || room.grid.Count == 0)
            {
                result.errors.Add(label + ": grid is empty.");
                continue;
            }

            int h = room.grid.Count;
            int w = room.grid[0].Length;
            bool rectangular = true;
            foreach (string line in room.grid)
                if (line.Length != w) rectangular = false;
            if (!rectangular)
            {
                result.errors.Add(label + ": rows are not all the same length.");
                continue;
            }

            if (w < s.minRoomWidth || h < s.minRoomHeight)
                result.warnings.Add(label + " is " + w + "x" + h + ", smaller than " + s.minRoomWidth + "x" + s.minRoomHeight + " (may be smaller than the camera view).");

            int zombiesInRoom = 0;
            bool overlapReported = false;

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    char ch = room.grid[row][col];
                    if (!LevelLegend.IsAllowed(ch))
                    {
                        result.errors.Add(label + ": unknown character '" + ch + "' at row " + row + ", column " + col + ".");
                        continue;
                    }

                    // Row 0 is the TOP row, so world y counts up from the bottom.
                    var pos = new Vector2Int(room.x + col, room.y + (h - 1 - row));

                    bool onEdgeRow = row == 0 || row == h - 1;
                    bool onEdgeCol = col == 0 || col == w - 1;
                    bool border = onEdgeRow || onEdgeCol;
                    bool corner = onEdgeRow && onEdgeCol;

                    if (border && ch != LevelLegend.Wall && ch != LevelLegend.Door)
                        result.errors.Add(label + ": border cell at row " + row + ", column " + col + " is '" + ch + "' but must be '#' or 'D' (rooms must be enclosed).");
                    if (ch == LevelLegend.Door && (!border || corner))
                        result.errors.Add(label + ": 'D' at row " + row + ", column " + col + " must be on the border and not in a corner.");

                    if (cells.ContainsKey(pos))
                    {
                        if (!overlapReported)
                            result.errors.Add(label + " overlaps another room at world cell (" + pos.x + "," + pos.y + ").");
                        overlapReported = true;
                        continue;
                    }
                    cells.Add(pos, new Cell { room = ri, ch = ch });

                    if (ch == LevelLegend.Player)
                    {
                        playerCount++;
                        result.playerCell = pos;
                        result.playerRoom = ri;
                    }
                    if (ch == LevelLegend.Zombie)
                    {
                        zombiesInRoom++;
                        result.zombieCells.Add(pos);
                    }
                }
            }

            totalZombies += zombiesInRoom;
            if (zombiesInRoom > s.maxZombiesPerRoom)
                result.errors.Add(label + " has " + zombiesInRoom + " zombies; the limit is " + s.maxZombiesPerRoom + ".");
        }

        if (playerCount != 1) result.errors.Add("The level must contain exactly one 'P' (found " + playerCount + ").");
        if (totalZombies > s.maxZombiesTotal)
            result.errors.Add("The level has " + totalZombies + " zombies; the limit is " + s.maxZombiesTotal + ".");

        return result.errors.Count == 0;
    }

    // Groups 'D' cells into doorways, checks each is built correctly, records landing cells.
    private static void CheckDoors(Dictionary<Vector2Int, Cell> cells, LevelValidationResult result, out List<Vector2Int> landingCells)
    {
        landingCells = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        foreach (var pair in cells)
        {
            if (pair.Value.ch != LevelLegend.Door || visited.Contains(pair.Key)) continue;

            // Flood fill: collect every touching 'D' cell into one cluster.
            var cluster = new List<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(pair.Key);
            visited.Add(pair.Key);
            while (stack.Count > 0)
            {
                Vector2Int p = stack.Pop();
                cluster.Add(p);
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int n = p + d;
                    if (visited.Contains(n)) continue;
                    if (cells.TryGetValue(n, out Cell nc) && nc.ch == LevelLegend.Door)
                    {
                        visited.Add(n);
                        stack.Push(n);
                    }
                }
            }

            AnalyseDoor(cells, cluster, result, landingCells);
        }
    }

    private static void AnalyseDoor(Dictionary<Vector2Int, Cell> cells, List<Vector2Int> cluster, LevelValidationResult result, List<Vector2Int> landingCells)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (Vector2Int p in cluster)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        string where = "Door at world cells (" + minX + "," + minY + ") to (" + maxX + "," + maxY + ")";

        if (cluster.Count != w * h)
        {
            result.errors.Add(where + ": the 'D' cells must form a solid rectangle.");
            return;
        }

        // A door is 2 cells thick across the wall: one 'D' column/row in each of two touching rooms.
        bool horizontal = false, vertical = false;
        int hNeg = -1, hPos = -1, vNeg = -1, vPos = -1;

        if (w == 2)
        {
            int left = cells[new Vector2Int(minX, minY)].room;
            int right = cells[new Vector2Int(maxX, minY)].room;
            bool ok = left != right;
            for (int y = minY; y <= maxY && ok; y++)
                if (cells[new Vector2Int(minX, y)].room != left || cells[new Vector2Int(maxX, y)].room != right) ok = false;
            if (ok) { horizontal = true; hNeg = left; hPos = right; }
        }
        if (h == 2)
        {
            int low = cells[new Vector2Int(minX, minY)].room;
            int high = cells[new Vector2Int(minX, maxY)].room;
            bool ok = low != high;
            for (int x = minX; x <= maxX && ok; x++)
                if (cells[new Vector2Int(x, minY)].room != low || cells[new Vector2Int(x, maxY)].room != high) ok = false;
            if (ok) { vertical = true; vNeg = low; vPos = high; }
        }

        if (horizontal && vertical)
        {
            result.errors.Add(where + ": ambiguous door shape.");
            return;
        }
        if (!horizontal && !vertical)
        {
            result.errors.Add(where + ": must be exactly 2 cells thick, one line of 'D' in each of two touching rooms, side by side.");
            return;
        }

        result.doors.Add(new DoorInfo
        {
            cells = new RectInt(minX, minY, w, h),
            horizontal = horizontal,
            negativeRoom = horizontal ? hNeg : vNeg,
            positiveRoom = horizontal ? hPos : vPos
        });

        int length = horizontal ? h : w;
        if (length < 2)
            result.warnings.Add(where + " is only 1 cell wide; the player collider may not fit.");

        // The player lands 2 cells past the door, so those cells must be open floor on both sides.
        if (horizontal)
        {
            for (int y = minY; y <= maxY; y++)
            {
                RequireOpen(cells, new Vector2Int(minX - 1, y), where, result);
                RequireOpen(cells, new Vector2Int(minX - 2, y), where, result);
                RequireOpen(cells, new Vector2Int(maxX + 1, y), where, result);
                RequireOpen(cells, new Vector2Int(maxX + 2, y), where, result);
                landingCells.Add(new Vector2Int(minX - 1, y));
                landingCells.Add(new Vector2Int(maxX + 1, y));
            }
        }
        else
        {
            for (int x = minX; x <= maxX; x++)
            {
                RequireOpen(cells, new Vector2Int(x, minY - 1), where, result);
                RequireOpen(cells, new Vector2Int(x, minY - 2), where, result);
                RequireOpen(cells, new Vector2Int(x, maxY + 1), where, result);
                RequireOpen(cells, new Vector2Int(x, maxY + 2), where, result);
                landingCells.Add(new Vector2Int(x, minY - 1));
                landingCells.Add(new Vector2Int(x, maxY + 1));
            }
        }
    }

    private static void RequireOpen(Dictionary<Vector2Int, Cell> cells, Vector2Int p, string where, LevelValidationResult result)
    {
        if (!cells.TryGetValue(p, out Cell c) || c.ch == LevelLegend.Wall)
            result.errors.Add(where + ": cell (" + p.x + "," + p.y + ") beside the door must be open floor so the player can land safely.");
    }

    // Flood fill from the player: every room must be reachable.
    private static void CheckReachability(LevelData level, Dictionary<Vector2Int, Cell> cells, LevelValidationResult result)
    {
        var reached = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(result.playerCell);
        reached.Add(result.playerCell);

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = p + d;
                if (reached.Contains(n)) continue;
                if (!cells.TryGetValue(n, out Cell c) || c.ch == LevelLegend.Wall) continue;
                reached.Add(n);
                queue.Enqueue(n);
            }
        }

        int[] openPerRoom = new int[level.rooms.Count];
        int[] reachedPerRoom = new int[level.rooms.Count];
        foreach (var pair in cells)
        {
            if (pair.Value.ch == LevelLegend.Wall) continue;
            openPerRoom[pair.Value.room]++;
            if (reached.Contains(pair.Key)) reachedPerRoom[pair.Value.room]++;
        }

        for (int ri = 0; ri < level.rooms.Count; ri++)
        {
            string id = level.rooms[ri].id;
            if (reachedPerRoom[ri] == 0)
                result.errors.Add("Room '" + id + "' cannot be reached from the player start.");
            else if (reachedPerRoom[ri] < openPerRoom[ri])
                result.warnings.Add("Room '" + id + "' has " + (openPerRoom[ri] - reachedPerRoom[ri]) + " open cells the player cannot reach (walled-off pockets).");
        }
    }

    // No zombies right next to the player start or the spot the player lands after a door.
    private static void CheckFairness(LevelValidationSettings s, LevelValidationResult result, List<Vector2Int> landingCells)
    {
        foreach (Vector2Int z in result.zombieCells)
        {
            if (Vector2Int.Distance(z, result.playerCell) < s.startSafeRadius)
                result.errors.Add("Zombie at (" + z.x + "," + z.y + ") is within " + s.startSafeRadius + " cells of the player start.");

            foreach (Vector2Int l in landingCells)
            {
                if (Vector2Int.Distance(z, l) < s.landingSafeRadius)
                {
                    result.errors.Add("Zombie at (" + z.x + "," + z.y + ") is within " + s.landingSafeRadius + " cells of a door landing spot.");
                    break;
                }
            }
        }
    }
}