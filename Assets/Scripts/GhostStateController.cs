using UnityEngine;

// Owns collision/animation state independently of GhostController movement so
// countdown and death can freeze both systems together.
public class GhostStateController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }
    public GhostState State { get; private set; }
    public bool CanMove { get; private set; }
    Vector3 initialPosition;
    Animator animator;

    void Awake()
    {
        initialPosition = transform.position;
        animator = GetComponent<Animator>();
        SetState(GhostState.Normal);
        if (GetComponent<GhostController>() == null) gameObject.AddComponent<GhostController>();
    }

    public void SetFrozen(bool frozen)
    {
        CanMove = !frozen;
        if (animator != null) animator.speed = frozen ? 0 : 1;
    }

    public void Tick(float delta, float scaredRemaining)
    {
        if (State == GhostState.Dead) return;
        SetState(scaredRemaining <= 0 ? GhostState.Normal :
            scaredRemaining <= 3 ? GhostState.Recovering : GhostState.Scared);
    }

    public void Eat()
    {
        SetState(GhostState.Dead);
    }

    public void Revive(GhostState state)
    {
        if (State != GhostState.Dead) return;
        SetState(state);
    }

    public void ResetGhost()
    {
        transform.position = initialPosition;
        var controller = GetComponent<GhostController>();
        if (controller != null) controller.ResetMovement();
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
