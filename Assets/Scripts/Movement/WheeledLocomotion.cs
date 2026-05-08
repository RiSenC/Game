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
    protected float currentThrottleInput = 0f;
    
    protected override void HandleInput()
    {
        // Forward/Backward
        Vector2 moveInput = ReadMoveInput();
        currentThrottleInput = moveInput.y;
        float targetSpeed = currentThrottleInput * maxSpeed;
        AccelerateToward(targetSpeed);
        currentSpeed = currentVelocity.x;
        
        // Steering becomes available as soon as the player is trying to drive.
        if (Mathf.Abs(currentSpeed) > 0.05f || Mathf.Abs(currentThrottleInput) > 0.1f)
        {
            float horizontalInput = moveInput.x;
            desiredSteeringAngle = -horizontalInput * steeringResponseSpeed;
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
        RotateToward(movementRoot.eulerAngles.z + steerAmount);
        
        // Player art faces up, so local up is the forward direction.
        Vector2 forward = (Vector2)movementRoot.up * currentSpeed;
        rb.linearVelocity = forward;
    }
}
