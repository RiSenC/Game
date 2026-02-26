using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    public float pushbackForce = 10f;
    
    void Start()
    {
        // Make sure obstacle has a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Obstacle should NOT be a trigger
        GetComponent<Collider2D>().isTrigger = false;
        
        // Add rigidbody to block movement
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // Static, doesn't move
        }
    }
}