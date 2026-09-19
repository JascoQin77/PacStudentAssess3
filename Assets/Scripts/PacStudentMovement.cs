using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    [SerializeField] float speed = 2.5f;
    [SerializeField] Vector3[] clockwiseRoute =
    {
        new Vector3(-11.5f, 12.5f, 0f),
        new Vector3(-8.5f, 12.5f, 0f),
        new Vector3(-8.5f, 10.5f, 0f),
        new Vector3(-11.5f, 10.5f, 0f)
    };

    Animator animator;
    AudioSource movementAudio;
    int routeIndex;
    float segmentElapsed;
    float segmentDuration;
    Vector3 segmentStart;

    void Awake()
    {
        animator = GetComponent<Animator>();
        movementAudio = GetComponent<AudioSource>();
        if (clockwiseRoute == null || clockwiseRoute.Length < 2)
            enabled = false;
    }

    void Start()
    {
        routeIndex = 0;
        segmentStart = clockwiseRoute[routeIndex];
        transform.position = segmentStart;
        segmentElapsed = 0f;
        segmentDuration = SegmentDuration();
        SetDirectionAnimation();
        if (movementAudio != null)
        {
            movementAudio.loop = true;
            movementAudio.playOnAwake = false;
            if (movementAudio.clip != null) movementAudio.Play();
        }
    }

    void Update()
    {
        segmentElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(segmentElapsed / segmentDuration);
        transform.position = Vector3.Lerp(segmentStart, TargetPoint(), t);
        if (t >= 1f)
        {
            routeIndex = (routeIndex + 1) % clockwiseRoute.Length;
            segmentStart = clockwiseRoute[routeIndex];
            segmentElapsed = 0f;
            segmentDuration = SegmentDuration();
            SetDirectionAnimation();
        }
    }

    Vector3 TargetPoint()
    {
        return clockwiseRoute[(routeIndex + 1) % clockwiseRoute.Length];
    }

    float SegmentDuration()
    {
        return Mathf.Max(Vector3.Distance(segmentStart, TargetPoint()) / Mathf.Max(speed, 0.01f), 0.01f);
    }

    void SetDirectionAnimation()
    {
        if (animator == null) return;
        Vector3 delta = TargetPoint() - segmentStart;
        string state = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? (delta.x > 0f ? "Walking_Right" : "Walking_Left")
            : (delta.y > 0f ? "Walking_Up" : "Walking_Down");
        animator.Play(state, 0, 0f);
    }
}
