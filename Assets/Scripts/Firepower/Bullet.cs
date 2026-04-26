using UnityEngine;

/// <summary>
/// Updated Bullet to work with new ProjectileBase system.
/// Also maintains backward compatibility with old hitscan system.
/// </summary>
public class Bullet : ProjectileBase
{
    private void FixedUpdate()
    {
        // Keep bullets rotated to face direction of movement
        if (rb != null && rb.linearVelocity.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }
}