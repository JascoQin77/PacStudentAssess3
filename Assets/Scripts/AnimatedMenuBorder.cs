using UnityEngine;

// All packets are RectTransforms inside the Canvas, so the border follows its aspect ratio.
public class AnimatedMenuBorder : MonoBehaviour
{
    [SerializeField] RectTransform[] packets;
    [SerializeField] float unitsPerSecond = 150f;
    RectTransform frame;

    public void Configure(RectTransform[] borderPackets) => packets = borderPackets;
    void Awake() => frame = (RectTransform)transform;
    void LateUpdate() => Animate(Time.unscaledTime);

    public void Animate(float elapsed)
    {
        if (frame == null) frame = (RectTransform)transform;
        if (packets == null || packets.Length == 0) return;
        float width = frame.rect.width, height = frame.rect.height;
        float perimeter = 2 * (width + height);
        if (perimeter <= 0) return;
        Vector2 origin = frame.rect.min;
        for (int i = 0; i < packets.Length; i++)
        {
            float distance = Mathf.Repeat(elapsed * unitsPerSecond + i * perimeter / packets.Length, perimeter);
            Vector2 position;
            if (distance < width) position = new Vector2(distance, height);
            else if ((distance -= width) < height) position = new Vector2(width, height - distance);
            else if ((distance -= height) < width) position = new Vector2(width - distance, 0);
            else position = new Vector2(0, distance - width);
            packets[i].anchoredPosition = origin + position;
        }
    }
}
