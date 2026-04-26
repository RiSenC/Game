using UnityEngine;

/// <summary>
/// Base for legged vehicles (mechs, spiders).
/// Always rotates toward cursor.
/// Moves on WASD relative to world (not body).
/// </summary>
public abstract class LeggedLocomotion : LocomotionBase
{
    protected Camera mainCam;
    protected Vector2 movementInput = Vector2.zero;
    
    protected override void Awake()
    {
        base.Awake();
        mainCam = Camera.main;
    }
    
    protected override void HandleInput()
    {
        // Gather movement input (relative to world)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        movementInput = new Vector2(h, v).normalized;
        
        // Rotate toward cursor
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        
        Vector2 toMouse = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
        
        RotateToward(targetAngle);
    }
    
    protected override void ApplyMovement()
    {
        // Accelerate in movement direction (world space)
        Vector2 targetVelocity = movementInput * maxSpeed;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        rb.linearVelocity = currentVelocity;
    }
}