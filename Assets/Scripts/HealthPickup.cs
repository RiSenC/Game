using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    public float healAmount = 20f;
    public Color pickupColor = new Color(0.35f, 1f, 0.45f, 1f);

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = pickupColor;
        transform.localScale = new Vector3(0.35f, 0.35f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HealthSystem health = other.GetComponentInParent<HealthSystem>();
        if (health == null)
            return;

        health.Heal(healAmount);
        Destroy(gameObject);
    }
}
