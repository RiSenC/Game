using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MapChoicePortal : MonoBehaviour
{
    public RunProgressionController controller;
    public int floorIndex;
    public int optionIndex;
    public string choiceLabel = "Room";
    public Color portalColor = new Color(0.3f, 0.75f, 1f, 0.8f);
    public Vector3 visualScale = new Vector3(0.8f, 0.8f, 1f);

    private TextMesh label;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        spriteRenderer.color = portalColor;
        transform.localScale = visualScale;

        EnsureLabel();
    }

    public void Configure(RunProgressionController targetController, int targetFloorIndex, int targetOptionIndex, string labelText)
    {
        Configure(targetController, targetFloorIndex, targetOptionIndex, labelText, portalColor, visualScale);
    }

    public void Configure(RunProgressionController targetController, int targetFloorIndex, int targetOptionIndex, string labelText, Color color, Vector3 scale)
    {
        controller = targetController;
        floorIndex = targetFloorIndex;
        optionIndex = targetOptionIndex;
        choiceLabel = labelText;
        portalColor = color;
        visualScale = scale;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = portalColor;
        transform.localScale = visualScale;

        EnsureLabel();
        label.transform.localScale = new Vector3(1f / Mathf.Max(0.01f, visualScale.x), 1f / Mathf.Max(0.01f, visualScale.y), 1f);
        label.text = choiceLabel;
    }

    private void EnsureLabel()
    {
        if (label == null)
            label = GetComponentInChildren<TextMesh>();
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.characterSize = 0.15f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || controller == null)
            return;

        controller.SelectNextRoom(floorIndex, optionIndex);
    }
}
