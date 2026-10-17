using UnityEngine;

// Spawns a bonus cherry outside the maze and linearly moves it through the level.
public class CherryController : MonoBehaviour
{
    [SerializeField] Sprite cherrySprite;
    [SerializeField] Camera levelCamera;
    [SerializeField] Transform pacStudent;
    [SerializeField] InGameHUDController hud;
    [SerializeField] AudioClip collectClip;
    [SerializeField] float spawnDelay = 5f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Vector2 mapMin = new Vector2(-13.5f, -13.5f);
    [SerializeField] Vector2 mapMax = new Vector2(13.5f, 13.5f);

    GameObject activeCherry;
    AudioSource collectAudio;
    Vector3 startPoint;
    Vector3 endPoint;
    float progress;
    float nextSpawnTime;
    PacStudentGameController game;

    public bool IsCherryActive => activeCherry != null;
    public float TimeUntilNextSpawn => Mathf.Max(0f, nextSpawnTime - Time.time);
    public Vector3 CurrentCherryPosition => activeCherry == null ? Vector3.zero : activeCherry.transform.position;

    void Awake()
    {
        if (levelCamera == null) levelCamera = Camera.main;
        if (pacStudent == null)
        {
            var pac = GameObject.Find("PacStudent");
            if (pac != null) pacStudent = pac.transform;
        }
        if (hud == null) hud = FindFirstObjectByType<InGameHUDController>();
        game = FindFirstObjectByType<PacStudentGameController>();
        collectAudio = GetComponent<AudioSource>();
        if (collectAudio == null) collectAudio = gameObject.AddComponent<AudioSource>();
        collectAudio.playOnAwake = false;
        collectAudio.spatialBlend = 0f;
        nextSpawnTime = Time.time + spawnDelay;
    }

    void Update()
    {
        // Spawn delay and travel both pause during countdown/death/game over.
        if (game != null && !game.IsPlaying)
        {
            nextSpawnTime += Time.deltaTime;
            return;
        }
        if (activeCherry == null)
        {
            if (Time.time >= nextSpawnTime) SpawnCherry();
            return;
        }

        float duration = Mathf.Max(Vector3.Distance(startPoint, endPoint) / Mathf.Max(moveSpeed, .01f), .01f);
        progress = Mathf.Clamp01(progress + Time.deltaTime / duration);
        activeCherry.transform.position = Vector3.Lerp(startPoint, endPoint, progress);
        var renderer = activeCherry.GetComponent<SpriteRenderer>();
        renderer.enabled = IsInsideMap(activeCherry.transform.position);

        if (progress >= 1f) DestroyCherry();
    }

    void SpawnCherry()
    {
        bool fromLeft = Random.value < .5f;
        float laneOffset = Random.Range(mapMin.y - 1f, mapMax.y + 1f);
        startPoint = new Vector3(fromLeft ? mapMin.x - 1.5f : mapMax.x + 1.5f, laneOffset, -1f);
        // Symmetric endpoints make the straight-line path cross the centre of the level.
        endPoint = new Vector3(fromLeft ? mapMax.x + 1.5f : mapMin.x - 1.5f, -laneOffset, -1f);
        progress = 0f;
        activeCherry = new GameObject("BonusCherry");
        activeCherry.transform.position = startPoint;
        var renderer = activeCherry.AddComponent<SpriteRenderer>();
        renderer.sprite = cherrySprite;
        renderer.sortingOrder = 50;
        renderer.enabled = false;
        var collider = activeCherry.AddComponent<CircleCollider2D>();
        collider.radius = .3f;
        collider.isTrigger = true;
        var item = activeCherry.AddComponent<LevelCollectible>();
        item.kind = LevelCollectible.CollectibleKind.Cherry;
        item.cherryOwner = this;
    }

    public void OnCherryCollected(GameObject cherry)
    {
        if (cherry != activeCherry) return;
        activeCherry = null;
        nextSpawnTime = Time.time + spawnDelay;
    }

    void DestroyCherry()
    {
        if (activeCherry != null) Destroy(activeCherry);
        activeCherry = null;
        nextSpawnTime = Time.time + spawnDelay;
    }

    bool IsInsideMap(Vector3 position)
    {
        return position.x >= mapMin.x && position.x <= mapMax.x && position.y >= mapMin.y && position.y <= mapMax.y;
    }
}
