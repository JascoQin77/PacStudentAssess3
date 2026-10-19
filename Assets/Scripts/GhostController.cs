using System.Collections.Generic;
using UnityEngine;

// Grid movement and per-ghost decision making for the 85% assessment section.
// Ghosts use the same half-cell coordinate system as PacStudent, but never use
// the tunnel teleporters or Unity pathfinding.
public class GhostController : MonoBehaviour
{
    [SerializeField] int ghostNumber = 1;
    [SerializeField] Transform mazeRoot;
    [SerializeField] PacStudentController player;
    [SerializeField] PacStudentGameController game;
    [SerializeField] GhostStateController state;
    [SerializeField] float tileSize = 1f;

    readonly HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> exitCells = new HashSet<Vector2Int>();
    readonly List<Vector2Int> candidates = new List<Vector2Int>(4);
    readonly List<Vector2Int> preferred = new List<Vector2Int>(4);

    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    Vector2Int currentCell;
    Vector2Int targetCell;
    Vector2Int previousCell;
    Vector2Int currentDirection;
    Vector3 initialPosition;
    Vector3 lerpStart;
    Vector3 lerpTarget;
    float lerpElapsed;
    bool isLerping;
    bool spawnExited;
    Vector2Int minCell;
    Vector2Int maxCell;
    bool boundsReady;

    const float SpawnMinX = -1.5f;
    const float SpawnMaxX = 1.5f;
    const float SpawnMinY = -1.5f;
    const float SpawnMaxY = 1.5f;

    public int GhostNumber => ghostNumber;
    public bool IsLerping => isLerping;
    public bool SpawnExited => spawnExited;
    public Vector2Int CurrentCell => currentCell;
    public Vector2Int CurrentDirection => currentDirection;

    public void ResetMovement()
    {
        initialPosition = transform.position;
        currentCell = WorldToCell(transform.position);
        previousCell = currentCell;
        currentDirection = ExitDirection();
        isLerping = false;
        lerpElapsed = 0;
        spawnExited = !IsSpawnCell(currentCell);
    }

    public void Configure(int number, Transform maze, PacStudentController target,
        PacStudentGameController controller, GhostStateController ghostState)
    {
        ghostNumber = Mathf.Clamp(number, 1, 4);
        mazeRoot = maze;
        player = target;
        game = controller;
        state = ghostState;
    }

    void Awake()
    {
        if (state == null) state = GetComponent<GhostStateController>();
        ResolveReferences();
        initialPosition = transform.position;
        BuildGrid();
    }

    void Start()
    {
        ResolveReferences();
        if (!boundsReady) BuildGrid();
        currentCell = WorldToCell(transform.position);
        transform.position = CellToWorld(currentCell);
        previousCell = currentCell;
        currentDirection = ExitDirection();
        spawnExited = !IsSpawnCell(currentCell);
    }

    void Update()
    {
        if (state == null || (game != null && !game.IsPlaying) || !state.CanMove) return;

        if (state.State == GhostStateController.GhostState.Dead)
        {
            isLerping = false;
            AdvanceDead(Time.deltaTime);
            return;
        }

        if (isLerping)
        {
            AdvanceLerp();
            return;
        }

        StartNextMove();
    }

    void ResolveReferences()
    {
        if (game == null) game = FindFirstObjectByType<PacStudentGameController>();
        if (player == null) player = FindFirstObjectByType<PacStudentController>();
        if (mazeRoot == null)
        {
            var level = GameObject.Find("Level01_Manual");
            if (level != null) mazeRoot = level.transform;
        }
        if (ghostNumber < 1 || ghostNumber > 4)
        {
            ghostNumber = NameToNumber(transform.name);
        }
    }

    static int NameToNumber(string name)
    {
        if (name == "GhostRed") return 1;
        if (name == "GhostPink") return 2;
        if (name == "GhostBlue") return 3;
        if (name == "GhostOrange") return 4;
        return 1;
    }

    void BuildGrid()
    {
        blockedCells.Clear();
        exitCells.Clear();
        boundsReady = false;
        if (mazeRoot == null) return;

        bool first = true;
        foreach (var renderer in mazeRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Vector2Int cell = WorldToCell(renderer.transform.position);
            if (first)
            {
                minCell = maxCell = cell;
                first = false;
            }
            else
            {
                minCell = Vector2Int.Min(minCell, cell);
                maxCell = Vector2Int.Max(maxCell, cell);
            }

            if (!renderer.name.StartsWith("Tile_")) continue;
            blockedCells.Add(cell);
            if (renderer.sprite != null && renderer.sprite.name == "Wall_GhostExit")
                exitCells.Add(cell);
        }
        boundsReady = !first;
    }

    void StartNextMove()
    {
        if (!spawnExited && TryStartSpawnExit()) return;

        GatherCandidates();
        if (candidates.Count == 0)
        {
            // A valid maze always has an escape route, but retain movement if a
            // future level creates a one-cell cul-de-sac.
            if (IsWalkable(currentCell + currentDirection * 2, true))
                StartMove(currentDirection);
            return;
        }

        Vector2Int direction = state.State == GhostStateController.GhostState.Normal
            ? ChooseNormalDirection(candidates)
            : ChooseGhostOneDirection(candidates);
        if (ghostNumber == 4 && state.State == GhostStateController.GhostState.Normal)
            direction = ChooseClockwiseDirection(candidates);
        StartMove(direction);
    }

    bool TryStartSpawnExit()
    {
        float exitX = ghostNumber <= 2 ? -.5f : .5f;
        float worldX = currentCell.x * tileSize * .5f;
        if (Mathf.Abs(worldX - exitX) > .01f)
        {
            Vector2Int horizontal = worldX < exitX ? Vector2Int.right : Vector2Int.left;
            if (IsWalkable(currentCell + horizontal * 2, true))
            {
                StartMove(horizontal);
                return true;
            }
        }

        Vector2Int vertical = ExitDirection();
        if (IsWalkable(currentCell + vertical * 2, true))
        {
            StartMove(vertical);
            return true;
        }

        // If the exact exit line is temporarily occupied by another ghost,
        // choose a valid non-backtracked move rather than stopping.
        GatherCandidates();
        if (candidates.Count == 0) return false;
        StartMove(candidates[Random.Range(0, candidates.Count)]);
        return true;
    }

    Vector2Int ExitDirection() => ghostNumber == 1 || ghostNumber == 3 ? Vector2Int.up : Vector2Int.down;

    void GatherCandidates()
    {
        candidates.Clear();
        bool hasNonReverse = false;
        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int direction = Directions[i];
            if (direction == -currentDirection) continue;
            if (!IsWalkable(currentCell + direction * 2, true)) continue;
            candidates.Add(direction);
            hasNonReverse = true;
        }

        // At a dead end, reversing is the only legal choice. Keep the
        // no-backtracking rule deterministic even when the reverse direction
        // is the first entry in Directions.
        if (!hasNonReverse)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                if (IsWalkable(currentCell + Directions[i] * 2, true))
                    candidates.Add(Directions[i]);
            }
        }
    }

    Vector2Int ChooseNormalDirection(List<Vector2Int> valid)
    {
        if (ghostNumber == 3) return valid[Random.Range(0, valid.Count)];

        Vector3 playerPosition = player == null ? transform.position : player.transform.position;
        float currentDistance = Vector3.Distance(transform.position, playerPosition);
        preferred.Clear();
        for (int i = 0; i < valid.Count; i++)
        {
            Vector3 target = CellToWorld(currentCell + valid[i] * 2);
            float distance = Vector3.Distance(target, playerPosition);
            bool accepted = ghostNumber == 1 ? distance >= currentDistance : distance <= currentDistance;
            if (accepted) preferred.Add(valid[i]);
        }
        return (preferred.Count > 0 ? preferred : valid)[Random.Range(0, (preferred.Count > 0 ? preferred : valid).Count)];
    }

    Vector2Int ChooseGhostOneDirection(List<Vector2Int> valid)
    {
        Vector3 playerPosition = player == null ? transform.position : player.transform.position;
        float currentDistance = Vector3.Distance(transform.position, playerPosition);
        preferred.Clear();
        for (int i = 0; i < valid.Count; i++)
        {
            Vector3 target = CellToWorld(currentCell + valid[i] * 2);
            if (Vector3.Distance(target, playerPosition) >= currentDistance) preferred.Add(valid[i]);
        }
        return (preferred.Count > 0 ? preferred : valid)[Random.Range(0, (preferred.Count > 0 ? preferred : valid).Count)];
    }

    Vector2Int ChooseClockwiseDirection(List<Vector2Int> valid)
    {
        if (!NearOuterBoundary())
        {
            Vector2Int towardBoundary = DirectionTowardNearestBoundary(valid);
            if (towardBoundary != Vector2Int.zero) return towardBoundary;
        }

        // In screen/world coordinates, (y, -x) is a clockwise quarter turn.
        // Prefer the right-hand turn, then straight, then the left turn.
        Vector2Int clockwise = new Vector2Int(currentDirection.y, -currentDirection.x);
        Vector2Int counterClockwise = -clockwise;
        Vector2Int[] order = { clockwise, currentDirection, counterClockwise, -currentDirection };
        for (int i = 0; i < order.Length; i++)
            if (valid.Contains(order[i])) return order[i];
        return valid[0];
    }

    Vector2Int DirectionTowardNearestBoundary(List<Vector2Int> valid)
    {
        float x = transform.position.x;
        float y = transform.position.y;
        float left = Mathf.Abs(x - minCell.x * tileSize * .5f);
        float right = Mathf.Abs(maxCell.x * tileSize * .5f - x);
        float down = Mathf.Abs(y - minCell.y * tileSize * .5f);
        float up = Mathf.Abs(maxCell.y * tileSize * .5f - y);
        float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
        Vector2Int direction = nearest == left ? Vector2Int.left :
            nearest == right ? Vector2Int.right :
            nearest == down ? Vector2Int.down : Vector2Int.up;
        return valid.Contains(direction) ? direction : Vector2Int.zero;
    }

    bool NearOuterBoundary()
    {
        float x = transform.position.x;
        float y = transform.position.y;
        float left = Mathf.Abs(x - minCell.x * tileSize * .5f);
        float right = Mathf.Abs(maxCell.x * tileSize * .5f - x);
        float down = Mathf.Abs(y - minCell.y * tileSize * .5f);
        float up = Mathf.Abs(maxCell.y * tileSize * .5f - y);
        return Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up)) <= 2f;
    }

    void StartMove(Vector2Int direction)
    {
        previousCell = currentCell;
        currentDirection = direction;
        targetCell = currentCell + direction * 2;
        lerpStart = transform.position;
        lerpTarget = CellToWorld(targetCell);
        lerpElapsed = 0;
        isLerping = true;
    }

    void AdvanceLerp()
    {
        if (state.State == GhostStateController.GhostState.Dead)
        {
            isLerping = false;
            return;
        }
        float duration = 1f / Mathf.Max(CurrentSpeed(), .01f);
        lerpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(lerpElapsed / duration);
        transform.position = Vector3.Lerp(lerpStart, lerpTarget, t);
        if (t < 1f) return;

        transform.position = lerpTarget;
        currentCell = targetCell;
        isLerping = false;
        if (!IsSpawnCell(currentCell)) spawnExited = true;
    }

    void AdvanceDead(float delta)
    {
        Vector3 target = new Vector3(initialPosition.x, initialPosition.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed() * delta);
        if ((transform.position - target).sqrMagnitude > .0001f) return;

        transform.position = target;
        currentCell = WorldToCell(transform.position);
        previousCell = currentCell;
        isLerping = false;
        spawnExited = false;
        state.Revive(GlobalState());
    }

    GhostStateController.GhostState GlobalState()
    {
        float remaining = game == null ? 0 : game.ScaredRemaining;
        return remaining <= 0 ? GhostStateController.GhostState.Normal :
            remaining <= 3 ? GhostStateController.GhostState.Recovering :
            GhostStateController.GhostState.Scared;
    }

    float CurrentSpeed()
    {
        float playerSpeed = player == null ? 5f : player.MoveSpeed;
        return state.State == GhostStateController.GhostState.Normal ? playerSpeed * .9f : playerSpeed * .45f;
    }

    bool IsWalkable(Vector2Int cell, bool allowBacktrack)
    {
        if (!boundsReady || cell.x < minCell.x || cell.x > maxCell.x || cell.y < minCell.y || cell.y > maxCell.y)
            return false;
        if (spawnExited && IsSpawnCell(cell)) return false;
        if (blockedCells.Contains(cell) && !exitCells.Contains(cell) && !IsDesignatedExitCell(cell)) return false;
        if (!allowBacktrack && cell == previousCell) return false;
        return true;
    }

    bool IsDesignatedExitCell(Vector2Int cell)
    {
        // The recreated level uses the same open gap geometry as the supplied
        // PacStudent maze. Keep the coordinates explicit so the behaviour is
        // still correct when the exit artwork is imported under a different
        // sprite name or GUID.
        int exitX = ghostNumber <= 2 ? -1 : 1;
        int exitY = ghostNumber == 1 || ghostNumber == 3 ? 5 : -5;
        return cell == new Vector2Int(exitX, exitY);
    }

    bool IsSpawnCell(Vector2Int cell)
    {
        float x = cell.x * tileSize * .5f;
        float y = cell.y * tileSize * .5f;
        return x >= SpawnMinX && x <= SpawnMaxX && y >= SpawnMinY && y <= SpawnMaxY;
    }

    Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x / (tileSize * .5f)),
            Mathf.RoundToInt(position.y / (tileSize * .5f)));
    }

    Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * tileSize * .5f, cell.y * tileSize * .5f, transform.position.z);
    }

    void OnValidate()
    {
        ghostNumber = Mathf.Clamp(ghostNumber, 1, 4);
    }
}
