using System.Collections.Generic;
using UnityEngine;

// Grid based PacStudent movement for the Assessment 4 manual level.
public class PacStudentController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Transform mazeRoot;
    [SerializeField] Animator animator;
    [SerializeField] ParticleSystem dustParticles;
    [SerializeField] AudioSource movementAudio;
    [SerializeField] AudioClip walkingClip;
    [SerializeField] AudioClip pelletApproachClip;
    [SerializeField] float tileSize = 1f;

    // These names and values are intentionally kept as state: the latest input is
    // retained while the player crosses the current grid cell.
    public Vector2Int lastInput;
    public Vector2Int currentInput;

    readonly HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
    readonly List<SpriteRenderer> pelletRenderers = new List<SpriteRenderer>();
    Vector2Int currentCell;
    Vector2Int targetCell;
    Vector3 lerpStart;
    Vector3 lerpTarget;
    float lerpElapsed;
    bool isLerping;
    bool hasInput;
    Vector2Int minCell;
    Vector2Int maxCell;
    bool boundsReady;
    bool controlEnabled;
    bool bumpReported;
    Vector3 startPosition;
    PacStudentGameController game;

    public bool IsLerping => isLerping;
    public float MoveSpeed => moveSpeed;
    public Vector2Int CurrentCell => currentCell;
    public Vector2Int TargetCell => targetCell;

    public void SetInput(Vector2Int input)
    {
        if (!controlEnabled || input == Vector2Int.zero) return;
        lastInput = new Vector2Int(Mathf.Clamp(input.x, -1, 1), Mathf.Clamp(input.y, -1, 1));
        hasInput = true;
        bumpReported = false;
    }

    void Awake()
    {
        if (mazeRoot == null)
        {
            var level = GameObject.Find("Level01_Manual");
            if (level != null) mazeRoot = level.transform;
        }
        if (animator == null) animator = GetComponent<Animator>();
        if (movementAudio == null) movementAudio = GetComponent<AudioSource>();
        game = FindFirstObjectByType<PacStudentGameController>();
        startPosition = transform.position;
        BuildGrid();
    }

    void Start()
    {
        currentCell = WorldToCell(transform.position);
        transform.position = CellToWorld(currentCell);
        lastInput = Vector2Int.right;
        currentInput = Vector2Int.zero;
        hasInput = false;
        SetAnimation(Vector2Int.right, false);
        StopMovementEffects();
    }

    void Update()
    {
        if (!controlEnabled) return;
        ReadInput();
        if (isLerping)
        {
            AdvanceLerp();
            return;
        }

        bool started = hasInput && TryStartMove(lastInput);
        if (!started && currentInput != Vector2Int.zero)
            started = hasInput && TryStartMove(currentInput);
        if (!started) ReportBlockedMove();
    }

    void ReadInput()
    {
        Vector2Int input = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W)) input = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.A)) input = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.S)) input = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.D)) input = Vector2Int.right;
        if (input == Vector2Int.zero) return;
        SetInput(input);
        SetAnimation(lastInput, isLerping);
    }

    bool TryStartMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero) return false;
        Vector2Int candidate = currentCell + direction * 2;
        if (!IsWalkable(candidate)) return false;

        currentInput = direction;
        targetCell = candidate;
        lerpStart = transform.position;
        lerpTarget = CellToWorld(targetCell);
        lerpElapsed = 0f;
        isLerping = true;
        bumpReported = false;
        SetAnimation(direction, true);
        StartMovementEffects(IsPelletAt(candidate));
        return true;
    }

    void AdvanceLerp()
    {
        lerpElapsed += Time.deltaTime;
        float duration = Mathf.Max(tileSize / Mathf.Max(moveSpeed, 0.01f), 0.01f);
        float t = Mathf.Clamp01(lerpElapsed / duration);
        transform.position = Vector3.Lerp(lerpStart, lerpTarget, t);
        if (t < 1f) return;

        transform.position = lerpTarget;
        currentCell = targetCell;
        isLerping = false;
        bool started = hasInput && TryStartMove(lastInput);
        if (!started && currentInput != Vector2Int.zero)
            started = hasInput && TryStartMove(currentInput);
        if (!started) ReportBlockedMove();
    }

    void BuildGrid()
    {
        blockedCells.Clear();
        pelletRenderers.Clear();
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
            if (renderer.name.StartsWith("Tile_")) blockedCells.Add(cell);
            if (renderer.name == "StandardPellet" || renderer.name == "PowerPellet") pelletRenderers.Add(renderer);
        }
        boundsReady = !first;
    }

    bool IsWalkable(Vector2Int cell)
    {
        // Only the two empty tunnel lanes can extend beyond the side bounds.
        if (boundsReady && Mathf.Abs(cell.y) == 1 &&
            cell.x >= minCell.x - 2 && cell.x <= maxCell.x + 2) return !blockedCells.Contains(cell);
        if (!boundsReady || cell.x < minCell.x || cell.x > maxCell.x || cell.y < minCell.y || cell.y > maxCell.y)
            return false;
        return !blockedCells.Contains(cell);
    }

    void ReportBlockedMove()
    {
        StopMovementEffects();
        if (!hasInput || bumpReported) return;
        bumpReported = true;
        if (game != null) game.BumpWall(transform.position + (Vector3)(Vector2)lastInput * .5f);
    }

    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        var trigger = GetComponent<Collider2D>();
        if (trigger != null) trigger.enabled = value;
        if (!value) StopMovementEffects();
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        currentCell = targetCell = WorldToCell(startPosition);
        isLerping = hasInput = bumpReported = false;
        currentInput = Vector2Int.zero;
        lastInput = Vector2Int.right;
        StopMovementEffects();
    }

    public void PlayDeath(float animationSpeed)
    {
        isLerping = false;
        if (animator == null) return;
        animator.speed = animationSpeed;
        animator.Play("Dead", 0, 0);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!controlEnabled || game == null || !game.IsPlaying) return;
        var tunnel = other.GetComponent<TunnelTeleporter>();
        if (tunnel != null)
        {
            transform.position = new Vector3(tunnel.destinationX, transform.position.y, transform.position.z);
            currentCell = WorldToCell(transform.position);
            isLerping = false;
            currentInput = lastInput = tunnel.inwardDirection;
            hasInput = true;
            TryStartMove(currentInput);
            return;
        }
        if (other.name.StartsWith("Tile_"))
        {
            // Grid tests normally reject walls before Lerp. This also handles
            // an unexpected physical overlap without a Rigidbody response.
            transform.position = isLerping ? lerpStart : CellToWorld(currentCell);
            isLerping = false;
            ReportBlockedMove();
            return;
        }
        HandleContact(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (controlEnabled && game != null && game.IsPlaying) HandleContact(other);
    }

    void HandleContact(Collider2D other)
    {
        var collectible = other.GetComponent<LevelCollectible>();
        if (collectible != null) game.Collect(collectible);
        var ghost = other.GetComponent<GhostStateController>();
        if (ghost != null) game.TouchGhost(ghost);
    }

    bool IsPelletAt(Vector2Int cell)
    {
        foreach (var pellet in pelletRenderers)
            if (pellet != null && pellet.gameObject.activeInHierarchy && WorldToCell(pellet.transform.position) == cell)
                return true;
        return false;
    }

    Vector2Int WorldToCell(Vector3 position)
    {
        // The manual scene uses half-unit world coordinates for each grid cell.
        return new Vector2Int(Mathf.RoundToInt(position.x / (tileSize * .5f)), Mathf.RoundToInt(position.y / (tileSize * .5f)));
    }

    Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * tileSize * .5f, cell.y * tileSize * .5f, transform.position.z);
    }

    void SetAnimation(Vector2Int direction, bool moving)
    {
        if (animator == null) return;
        string state = direction == Vector2Int.up ? "Walking_Up" :
            direction == Vector2Int.down ? "Walking_Down" :
            direction == Vector2Int.left ? "Walking_Left" : "Walking_Right";
        animator.Play(state, 0, 0f);
        animator.speed = moving ? 1f : 0f;
    }

    void StartMovementEffects(bool approachingPellet)
    {
        if (dustParticles != null && !dustParticles.isPlaying) dustParticles.Play();
        if (movementAudio == null) return;
        AudioClip clip = approachingPellet ? pelletApproachClip : walkingClip;
        if (clip == null) return;
        if (movementAudio.clip != clip) movementAudio.clip = clip;
        movementAudio.loop = true;
        movementAudio.playOnAwake = false;
        if (!movementAudio.isPlaying) movementAudio.Play();
    }

    void StopMovementEffects()
    {
        if (dustParticles != null && dustParticles.isPlaying) dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (movementAudio != null && movementAudio.isPlaying) movementAudio.Stop();
        if (!isLerping) SetAnimation(currentInput == Vector2Int.zero ? Vector2Int.right : currentInput, false);
    }
}
