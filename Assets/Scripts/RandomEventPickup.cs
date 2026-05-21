using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RandomEventPickup : MonoBehaviour
{
    public enum EventOutcome
    {
        ScrapCache,
        FieldRepair,
        FreeModule,
        Ambush
    }

    public EventOutcome outcome;
    public int scrapReward = 6;
    public float healAmount = 25f;
    public RunProgressionController controller;
    public float pickupDelay = 0.2f;

    private float spawnedAt;

    private void Awake()
    {
        spawnedAt = Time.time;

        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = new Color(0.85f, 0.35f, 1f, 1f);
        transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        EnsureLabel();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < spawnedAt + pickupDelay)
            return;

        PlayerRunStats stats = other.GetComponentInParent<PlayerRunStats>();
        PlayerModuleController player = other.GetComponentInParent<PlayerModuleController>();
        if (stats == null || player == null)
            return;

        switch (outcome)
        {
            case EventOutcome.ScrapCache:
                stats.AddScrap(scrapReward);
                break;
            case EventOutcome.FieldRepair:
                if (player.healthSystem != null)
                    player.healthSystem.Heal(healAmount);
                break;
            case EventOutcome.FreeModule:
                if (controller != null)
                    controller.SpawnEventRewardModule();
                break;
            case EventOutcome.Ambush:
                if (controller != null)
                    controller.TriggerEventAmbush();
                break;
        }

        Destroy(gameObject);
    }

    private void EnsureLabel()
    {
        TextMesh label = GetComponentInChildren<TextMesh>();
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.characterSize = 0.12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
        }

        label.text = outcome switch
        {
            EventOutcome.ScrapCache => "Scrap Cache",
            EventOutcome.FieldRepair => "Field Repair",
            EventOutcome.FreeModule => "Prototype Drop",
            EventOutcome.Ambush => "Unstable Signal",
            _ => "Event"
        };
    }
}
