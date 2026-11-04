using Newtonsoft.Json;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Reads a level JSON file, checks it with LevelValidator, then builds it:
// paints tiles, spawns zombies, creates camera boundaries and room-transition doors,
// and moves the player to the start. Put this on an empty GameObject.
//
// Two ways to use it:
//  1. BAKE (for hand-editing): right-click the component header > "Build Level (Editor)".
//     Everything is saved into the scene, so you can paint over it. Untick "Build On Start".
//  2. RUNTIME: leave "Build On Start" ticked and the level is rebuilt every time you press Play.
public class LevelBuilder : MonoBehaviour
{
    [Header("Level file")]
    [SerializeField] private TextAsset levelJson;
    [SerializeField] private bool buildOnStart = false;   // tick for runtime generation; UNTICK after baking

    [Header("Tilemaps in the scene")]
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap wallMap;
    [Tooltip("Optional. Paint your own decoration here. The builder NEVER clears this map.")]
    [SerializeField] private Tilemap decorationMap;

    [Header("Tiles")]
    [SerializeField] private TileBase[] plainFloorTiles;
    [SerializeField] private TileBase[] detailFloorTiles;
    [SerializeField, Range(0f, 1f)] private float detailFloorChance = 0.15f;
    [SerializeField] private TileBase[] wallTiles;

    [Header("Prefabs")]
    [SerializeField] private GameObject zombiePrefab;

    [Header("Validation rules")]
    [SerializeField] private LevelValidationSettings rules = new LevelValidationSettings();

    private const string RootName = "GeneratedLevel";

    private void Start()
    {
        if (Application.isPlaying && buildOnStart) BuildLevel();
    }

    [ContextMenu("Validate Level Only")]
    public void ValidateOnly()
    {
        LevelData level = LoadLevel();
        if (level == null) return;
        Report(level, LevelValidator.Validate(level, rules));
    }

    // Works in the editor AND in Play mode.
    [ContextMenu("Build Level (Editor)")]
    public void BuildLevel()
    {
        if (!HasRequiredReferences()) return;

        LevelData level = LoadLevel();
        if (level == null) return;

        LevelValidationResult result = LevelValidator.Validate(level, rules);
        Report(level, result);
        if (!result.IsValid) return;   // never build a level that failed validation

        ClearLevel();
        GameObject levelRoot = new GameObject(RootName);
        EnsureWallPhysics();

        var rng = new System.Random(level.seed);
        var roomBounds = new PolygonCollider2D[level.rooms.Count];

        for (int ri = 0; ri < level.rooms.Count; ri++)
        {
            RoomData room = level.rooms[ri];
            int h = room.grid.Count;
            int w = room.grid[0].Length;

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    char ch = room.grid[row][col];
                    var cell = new Vector3Int(room.x + col, room.y + (h - 1 - row), 0);

                    if (ch == LevelLegend.Wall)
                    {
                        wallMap.SetTile(cell, Pick(wallTiles, rng));
                    }
                    else
                    {
                        groundMap.SetTile(cell, PickFloor(rng));
                        if (ch == LevelLegend.Zombie)
                            SpawnZombie(groundMap.GetCellCenterWorld(cell), levelRoot.transform);
                    }
                }
            }

            roomBounds[ri] = CreateRoomBounds(levelRoot.transform, room.id, room.x, room.y, w, h);
        }

        CreateDoors(levelRoot.transform, level, result, roomBounds);
        PlacePlayer(result, roomBounds);
        MarkSceneChanged();

        Debug.Log("LevelBuilder: built '" + level.name + "' (" + level.rooms.Count + " rooms, " + result.doors.Count + " doors, " + result.zombieCells.Count + " zombies).");
    }

    [ContextMenu("Clear Generated Level")]
    public void ClearGeneratedLevel()
    {
        if (!HasMaps()) return;
        ClearLevel();
        MarkSceneChanged();
    }

    private bool HasMaps()
    {
        if (groundMap != null && wallMap != null) return true;
        Debug.LogError("LevelBuilder: assign the Ground Map and Wall Map.");
        return false;
    }

    private bool HasRequiredReferences()
    {
        string missing = "";
        if (levelJson == null) missing += " Level Json,";
        if (groundMap == null) missing += " Ground Map,";
        if (wallMap == null) missing += " Wall Map,";
        if (plainFloorTiles == null || plainFloorTiles.Length == 0) missing += " Plain Floor Tiles,";
        if (wallTiles == null || wallTiles.Length == 0) missing += " Wall Tiles,";
        if (zombiePrefab == null) missing += " Zombie Prefab,";

        if (missing.Length == 0) return true;
        Debug.LogError("LevelBuilder is missing:" + missing.TrimEnd(',') + ".");
        return false;
    }

    private LevelData LoadLevel()
    {
        if (levelJson == null)
        {
            Debug.LogError("LevelBuilder: assign a Level Json file.");
            return null;
        }
        try
        {
            return JsonConvert.DeserializeObject<LevelData>(levelJson.text);
        }
        catch (JsonException e)
        {
            string message = "Level JSON could not be read: " + e.Message;
            GUIUtility.systemCopyBuffer = message;
            Debug.LogError(message + "\n(Copied to your clipboard so you can paste it back to Claude.)");
            return null;
        }
    }

    private void Report(LevelData level, LevelValidationResult result)
    {
        string report = result.BuildReport(level.name);
        if (!result.IsValid)
        {
            GUIUtility.systemCopyBuffer = report;
            Debug.LogError(report + "\n(Copied to your clipboard so you can paste it back to Claude.)");
        }
        else if (result.warnings.Count > 0) Debug.LogWarning(report);
        else Debug.Log(report);
    }

    // Removes the previous build. The decoration map is deliberately left alone.
    private void ClearLevel()
    {
        // Looked up by name (not stored) so it also works after a script reload or scene save.
        GameObject old;
        while ((old = GameObject.Find(RootName)) != null)
        {
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);
            if (Application.isPlaying) break;   // Destroy is delayed, so the loop would never end
        }
        groundMap.ClearAllTiles();
        wallMap.ClearAllTiles();
    }

    private void SpawnZombie(Vector3 position, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // PrefabUtility keeps the prefab link, so editing the prefab later updates baked zombies too.
            var z = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab, parent);
            z.transform.position = position;
            return;
        }
#endif
        Instantiate(zombiePrefab, position, Quaternion.identity, parent);
    }

    private void MarkSceneChanged()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void EnsureWallPhysics()
    {
        var body = wallMap.GetComponent<Rigidbody2D>();
        if (body == null) body = wallMap.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        var tileCollider = wallMap.GetComponent<TilemapCollider2D>();
        if (tileCollider == null) tileCollider = wallMap.gameObject.AddComponent<TilemapCollider2D>();
        tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        if (wallMap.GetComponent<CompositeCollider2D>() == null)
            wallMap.gameObject.AddComponent<CompositeCollider2D>();
    }

    private PolygonCollider2D CreateRoomBounds(Transform parent, string id, int x, int y, int w, int h)
    {
        var go = new GameObject("RoomBounds_" + id);
        go.transform.SetParent(parent, false);

        Vector2 min = groundMap.CellToWorld(new Vector3Int(x, y, 0));
        Vector2 max = groundMap.CellToWorld(new Vector3Int(x + w, y + h, 0));

        var poly = go.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.SetPath(0, new[] { min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y) });
        return poly;
    }

    private void CreateDoors(Transform parent, LevelData level, LevelValidationResult result, PolygonCollider2D[] roomBounds)
    {
        foreach (DoorInfo d in result.doors)
        {
            Vector3 min = groundMap.CellToWorld(new Vector3Int(d.cells.xMin, d.cells.yMin, 0));
            Vector3 max = groundMap.CellToWorld(new Vector3Int(d.cells.xMax, d.cells.yMax, 0));

            var go = new GameObject("Door_" + level.rooms[d.negativeRoom].id + "_" + level.rooms[d.positiveRoom].id);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) / 2f;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(max.x - min.x, max.y - min.y);

            var transition = go.AddComponent<RoomTransition>();
            transition.Configure(d.horizontal, roomBounds[d.negativeRoom], roomBounds[d.positiveRoom]);
        }
    }

    private void PlacePlayer(LevelValidationResult result, PolygonCollider2D[] roomBounds)
    {
        Player_Movement player = FindAnyObjectByType<Player_Movement>();
        if (player == null)
        {
            Debug.LogWarning("LevelBuilder: no player found in the scene.");
            return;
        }

        Vector3 target = groundMap.GetCellCenterWorld(new Vector3Int(result.playerCell.x, result.playerCell.y, 0));
        target.z = player.transform.position.z;
        Vector3 delta = target - player.transform.position;

        var body = player.GetComponent<Rigidbody2D>();
        if (body != null && Application.isPlaying)
        {
            body.position = target;
            body.linearVelocity = Vector2.zero;
        }
        player.transform.position = target;

        var confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        if (confiner == null) return;

        confiner.BoundingShape2D = roomBounds[result.playerRoom];

        if (Application.isPlaying)
        {
            confiner.InvalidateBoundingShapeCache();
            var vcam = confiner.GetComponent<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.OnTargetObjectWarped(player.transform, delta);
                vcam.PreviousStateIsValid = false;
            }
        }
    }

    private TileBase PickFloor(System.Random rng)
    {
        bool useDetail = detailFloorTiles != null && detailFloorTiles.Length > 0 && rng.NextDouble() < detailFloorChance;
        return Pick(useDetail ? detailFloorTiles : plainFloorTiles, rng);
    }

    private static TileBase Pick(TileBase[] tiles, System.Random rng)
    {
        return tiles[rng.Next(tiles.Length)];
    }
}