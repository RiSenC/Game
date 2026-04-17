using UnityEngine;

/// <summary>
/// Abstract base for all locomotion types.
/// Override for wheels, legs, jets, etc.
/// </summary>
public abstract class LocomotionBase : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 10f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float rotationSpeed = 180f;  // degrees per second
    
    protected Vector2 currentVelocity = Vector2.zero;
    protected Rigidbody2D rb;
    public ChassisController chassis;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        chassis = GetComponentInParent<ChassisController>();
    }
    
    protected virtual void Update()
    {
        HandleInput();
    }
    
    protected virtual void FixedUpdate()
    {
        ApplyMovement();
    }
    
    /// <summary>Override to define input handling per locomotion type</summary>
    protected abstract void HandleInput();
    
    /// <summary>Override to apply movement physics per type</summary>
    protected abstract void ApplyMovement();
    
    /// <summary>Apply chassis modifiers to this locomotion</summary>
    public virtual void ApplyChassisModifiers(ChassisStats stats)
    {
        maxSpeed *= stats.speedMultiplier;
        acceleration *= stats.accelerationMultiplier;
        rotationSpeed *= stats.turningMultiplier;
    }
    
    /// <summary>Helper: accelerate toward target speed</summary>
    protected void AccelerateToward(float targetSpeed)
    {
        float accelRate = targetSpeed > 0 ? acceleration : deceleration;
        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetSpeed, accelRate * Time.deltaTime);
    }
    
    /// <summary>Helper: rotate toward target angle</summary>
    protected void RotateToward(float targetAngle)
    {
        float current = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(current, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }
}