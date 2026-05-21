using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MapChoicePortal : MonoBehaviour
{
    public RunProgressionController controller;
    public int floorIndex;
    public int optionIndex;
    public string choiceLabel = "Room";
    public Color portalColor = new Color(0.3f, 0.75f, 1f, 0.8f);

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = portalColor;
        transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        TextMesh label = GetComponentInChildren<TextMesh>();
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

        label.text = choiceLabel;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || controller == null)
            return;

        controller.SelectNextRoom(floorIndex, optionIndex);
    }
}
