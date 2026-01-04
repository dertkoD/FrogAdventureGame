using UnityEngine;

public class PullableItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody itemRb;
    [SerializeField] private Transform pullAnchor; // optional (center point). if null uses rb.position

    [Header("Rules")]
    [SerializeField] private bool canBePulled = true;

    public bool CanBePulled => canBePulled && itemRb != null;
    public Rigidbody ItemRigidbody => itemRb;

    public Vector3 PullAnchorPosition
    {
        get
        {
            if (pullAnchor != null) return pullAnchor.position;
            if (itemRb != null) return itemRb.position;
            return transform.position;
        }
    }

    // Called when pulling starts (optional hook)
    public virtual void OnPullStart(PlayerItemPull puller) { }

    // Called when pulling stops (cancel or arrive)
    public virtual void OnPullStop(PlayerItemPull puller, bool arrived) { }

    // Called when it successfully reaches player
    // Default: just destroy (you can override or add pickup logic)
    
    void Reset()
    {
        if (itemRb == null) itemRb = GetComponent<Rigidbody>();
    }
}
