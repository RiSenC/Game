using UnityEngine;
using UnityEngine.InputSystem;

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
    protected PlayerInput playerInput;
    protected InputAction moveAction;
    protected Transform movementRoot;
    protected float baseMaxSpeed;
    protected float baseAcceleration;
    protected float baseDeceleration;
    protected float baseRotationSpeed;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();

        chassis = GetComponentInParent<ChassisController>();
        playerInput = GetComponentInParent<PlayerInput>();
        moveAction = playerInput != null ? playerInput.actions["Move"] : null;
        movementRoot = chassis != null ? chassis.transform : transform;
        baseMaxSpeed = maxSpeed;
        baseAcceleration = acceleration;
        baseDeceleration = deceleration;
        baseRotationSpeed = rotationSpeed;
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
        maxSpeed = baseMaxSpeed * stats.speedMultiplier;
        acceleration = baseAcceleration * stats.accelerationMultiplier;
        deceleration = baseDeceleration * stats.accelerationMultiplier;
        rotationSpeed = baseRotationSpeed * stats.turningMultiplier;
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
        float current = movementRoot.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(current, targetAngle, rotationSpeed * Time.deltaTime);
        movementRoot.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    protected Vector2 ReadMoveInput()
    {
        if (moveAction != null)
        {
            Vector2 value = moveAction.ReadValue<Vector2>();
            if (value.sqrMagnitude > 0f)
                return value;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed) y += 1f;
        return new Vector2(x, y).normalized;
    }

    protected Vector3 ReadMouseWorldPosition(Camera camera)
    {
        if (camera == null || Mouse.current == null)
            return movementRoot.position;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = Mathf.Abs(camera.transform.position.z);
        return camera.ScreenToWorldPoint(mouseScreen);
    }

    protected float ReadDigitalAxis(Key negativeKey, Key positiveKey)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return 0f;

        float value = 0f;
        if (keyboard[negativeKey].isPressed)
            value -= 1f;
        if (keyboard[positiveKey].isPressed)
            value += 1f;
        return value;
    }
}
