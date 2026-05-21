using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScrapPickup : MonoBehaviour
{
    public int scrapAmount = 1;
    public Color pickupColor = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = pickupColor;
        transform.localScale = new Vector3(0.25f, 0.25f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRunStats stats = other.GetComponentInParent<PlayerRunStats>();
        if (stats == null)
            return;

        stats.AddScrap(scrapAmount);
        Destroy(gameObject);
    }
}
