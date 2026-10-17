using UnityEngine;

// Ghost AI is a later assessment section. This owns collision/animation state
// independently of movement so countdown and death also freeze future AI.
public class GhostStateController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }
    public GhostState State { get; private set; }
    public bool CanMove { get; private set; }
    public float RespawnRemaining { get; private set; }
    Vector3 initialPosition;
    Animator animator;

    void Awake()
    {
        initialPosition = transform.position;
        animator = GetComponent<Animator>();
        SetState(GhostState.Normal);
    }

    public void SetFrozen(bool frozen)
    {
        CanMove = !frozen;
        if (animator != null) animator.speed = frozen ? 0 : 1;
    }

    public void Tick(float delta, float scaredRemaining)
    {
        if (State == GhostState.Dead)
        {
            RespawnRemaining = Mathf.Max(0, RespawnRemaining - delta);
            if (RespawnRemaining > 0) return;
        }
        SetState(scaredRemaining <= 0 ? GhostState.Normal :
            scaredRemaining <= 3 ? GhostState.Recovering : GhostState.Scared);
    }

    public void Eat()
    {
        RespawnRemaining = 3f;
        SetState(GhostState.Dead);
    }

    public void ResetGhost()
    {
        transform.position = initialPosition;
        RespawnRemaining = 0;
        SetState(GhostState.Normal);
    }

    void SetState(GhostState state)
    {
        if (animator == null) animator = GetComponent<Animator>();
        bool changed = State != state;
        State = state;
        if (animator == null || (!changed && animator.GetCurrentAnimatorStateInfo(0).fullPathHash != 0)) return;
        string animationName = state == GhostState.Normal ? "Normal" : state.ToString();
        animator.Play(animationName, 0, 0);
    }
}
