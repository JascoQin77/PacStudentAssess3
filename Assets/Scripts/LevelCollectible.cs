using UnityEngine;

// Triggers report collection; no Rigidbody applies forces or moves the player.
public class LevelCollectible : MonoBehaviour
{
    public enum CollectibleKind { Pellet, PowerPill, Cherry }
    public CollectibleKind kind;
    public CherryController cherryOwner;
    bool collected;

    public bool TryCollect()
    {
        if (collected) return false;
        collected = true;
        gameObject.SetActive(false);
        if (cherryOwner != null) cherryOwner.OnCherryCollected(gameObject);
        Destroy(gameObject);
        return true;
    }
}
