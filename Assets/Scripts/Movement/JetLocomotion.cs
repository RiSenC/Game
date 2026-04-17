using UnityEngine;

/// <summary>
/// Jet/Hover propulsion.
/// Very low friction — barely slows down after thrusting.
/// WASD moves relative to body direction.
/// Q/E rotate the body (A/D no longer steer, instead Q/E do).
/// </summary>
public class JetLocomotion : LocomotionBase
{
    [Header("Jet Specific")]
    public float thrustForce = 20f;
    public float frictionDrag = 0.05f;  // very low friction
    public float bodyRotationSpeed = 250f;
    
    private Vector2 thrustInput = Vector2.zero;
    
    protected override void HandleInput()
    {
        // WASD for thrust (relative to body)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        thrustInput = new Vector2(h, v).normalized;
        
        // Q/E for body rotation
        float rotate = 0f;
        if (Input.GetKey(KeyCode.Q)) rotate = -1f;
        if (Input.GetKey(KeyCode.E)) rotate = 1f;
        
        if (rotate != 0f)
            RotateToward(transform.eulerAngles.z + rotate * bodyRotationSpeed * Time.deltaTime);
    }
    
    protected override void ApplyMovement()
    {
        // Apply thrust relative to body orientation
        Vector2 thrustDirection = (Vector2)transform.right * thrustInput.x + (Vector2)transform.up * thrustInput.y;
        currentVelocity += thrustDirection * thrustForce * Time.deltaTime;
        
        // Apply friction (very minimal)
        currentVelocity *= (1f - frictionDrag * Time.deltaTime);
        
        // Clamp to max speed
        if (currentVelocity.magnitude > maxSpeed)
            currentVelocity = currentVelocity.normalized * maxSpeed;
        
        rb.linearVelocity = currentVelocity;
    }
    
    public override void ApplyChassisModifiers(ChassisStats stats)
    {
        base.ApplyChassisModifiers(stats);
        thrustForce *= stats.accelerationMultiplier;
    }
}