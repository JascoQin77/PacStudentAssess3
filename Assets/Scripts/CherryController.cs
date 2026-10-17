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
        collectAudio = GetComponent<AudioSource>();
        if (collectAudio == null) collectAudio = gameObject.AddComponent<AudioSource>();
        collectAudio.playOnAwake = false;
        collectAudio.spatialBlend = 0f;
        nextSpawnTime = Time.time + spawnDelay;
    }

    void Update()
    {
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

        if (pacStudent != null && Vector2.Distance(activeCherry.transform.position, pacStudent.position) < .55f)
        {
            CollectCherry();
            return;
        }
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
    }

    void CollectCherry()
    {
        if (hud != null) hud.AddScore(100);
        if (collectAudio != null && collectClip != null) collectAudio.PlayOneShot(collectClip);
        DestroyCherry();
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
