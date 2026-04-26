using UnityEngine;

/// <summary>
/// Base for wheeled vehicles (wheels, tracks).
/// Requires manual steering input — steer while moving.
/// </summary>
public abstract class WheeledLocomotion : LocomotionBase
{
    [Header("Wheeled Specific")]
    public float steeringResponseSpeed = 200f;  // degrees per second
    
    protected float currentSpeed = 0f;
    protected float desiredSteeringAngle = 0f;
    
    protected override void HandleInput()
    {
        // Forward/Backward
        float verticalInput = Input.GetAxis("Vertical");
        float targetSpeed = verticalInput * maxSpeed;
        AccelerateToward(targetSpeed);
        currentSpeed = currentVelocity.x;
        
        // Steering (only when moving)
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            desiredSteeringAngle = horizontalInput * steeringResponseSpeed;
        }
        else
        {
            desiredSteeringAngle = 0f;
        }
    }
    
    protected override void ApplyMovement()
    {
        // Apply steering
        float steerAmount = desiredSteeringAngle * Time.deltaTime;
        RotateToward(transform.eulerAngles.z + steerAmount);
        
        // Apply forward movement
        Vector2 forward = (Vector2)transform.right * currentSpeed;
        rb.linearVelocity = forward;
    }
}