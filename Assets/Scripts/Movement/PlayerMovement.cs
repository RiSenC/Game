using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public HullConfig hullConfig;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private InputAction moveAction;
    private InputAction handbrakeAction;
    private bool isHandbraking = false;
    
    // Physics-based movement
    private Vector2 currentVelocity = Vector2.zero;
    private float currentRotation = 0f;
    
    [Header("Physics Settings")]
    public float friction = 0.95f;
    public float handbrakeFriction = 0.85f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Get or add PlayerInput
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            handbrakeAction = playerInput.actions["Handbrake"];
        }
        
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        currentRotation = transform.eulerAngles.z;
    }
    
    void FixedUpdate()
    {
        if (hullConfig == null) return;
        
        // Read input if available
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
            isHandbraking = handbrakeAction != null && handbrakeAction.IsPressed();
        }
        
        if (hullConfig.hullType == HullConfig.HullType.Tank)
        {
            HandleTankPhysics();
        }
        else if (hullConfig.hullType == HullConfig.HullType.Car)
        {
            HandleCarPhysics();
        }
    }
    
    // ===== TANK PHYSICS =====
    void HandleTankPhysics()
    {
        Vector2 forwardDirection = transform.up;
        
        // Apply acceleration in forward/backward direction
        float forwardInput = moveInput.y;
        currentVelocity += forwardDirection * forwardInput * hullConfig.acceleration * Time.fixedDeltaTime;
        
        // Tank rotates based on input (invert if moving backward)
        float rotationInput = moveInput.x;
        if (moveInput.y < -0.1f)
            rotationInput = -rotationInput;
        
        // Fixed: Proper rotation amount (negative for correct direction)
        float rotationAmount = rotationInput * hullConfig.rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation + rotationAmount);
        
        // Apply friction
        float currentFriction = isHandbraking ? handbrakeFriction : friction;
        currentVelocity *= currentFriction;
        
        // Limit speed
        if (currentVelocity.magnitude > hullConfig.maxSpeed)
        {
            currentVelocity = currentVelocity.normalized * hullConfig.maxSpeed;
        }
        
        // Apply to rigidbody
        rb.linearVelocity = currentVelocity;
    }
    
    // ===== CAR PHYSICS =====
    void HandleCarPhysics()
    {
        // Get input
        float forwardInput = moveInput.y;
        float steeringInput = moveInput.x;
        
        // Car can only rotate if moving forward/backward OR handbraking
        float speed = currentVelocity.magnitude;
        bool hasForwardInput = Mathf.Abs(forwardInput) > 0.1f;
        bool canSteer = (hasForwardInput && speed > 0.1f) || isHandbraking;
        
        if (canSteer)
        {
            // Fixed: Proper speed factor - less steering at low speed, more at high speed
            float speedFactor = Mathf.Clamp01(speed / hullConfig.maxSpeed);
            
            // Base steering response is lower at low speeds, increases with speed
            float effectiveSteeringSpeed = hullConfig.steeringResponse * (0.3f + speedFactor * 0.7f);
            
            if (isHandbraking)
            {
                effectiveSteeringSpeed = hullConfig.steeringResponse * hullConfig.handbrakeSteeringBoost;
            }
            
            // Update rotation based on steering input
            // Fixed: Proper steering direction (negative for correct orientation)
            currentRotation += -steeringInput * effectiveSteeringSpeed * Time.fixedDeltaTime;
        }
        
        // Fixed: Correct forward direction calculation (using proper orientation)
        // Unity uses Z rotation where 0 = up, 90 = right, -90 = left
        float radians = currentRotation * Mathf.Deg2Rad;
        Vector2 forwardDirection = new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
        
        // Apply acceleration in forward direction based on forward/backward input
        if (Mathf.Abs(forwardInput) > 0.1f)
        {
            currentVelocity += forwardDirection * forwardInput * hullConfig.acceleration * Time.fixedDeltaTime;
        }
        
        // Apply friction
        float currentFriction = isHandbraking ? handbrakeFriction : friction;
        currentVelocity *= currentFriction;
        
        // Limit speed
        if (currentVelocity.magnitude > hullConfig.maxSpeed)
        {
            currentVelocity = currentVelocity.normalized * hullConfig.maxSpeed;
        }
        
        // Apply velocity and rotation to rigidbody
        rb.linearVelocity = currentVelocity;
        rb.MoveRotation(currentRotation);
    }
    
    // Public methods to set config at runtime
    public void SetHullConfig(HullConfig config)
    {
        hullConfig = config;
    }
    
    public Vector2 GetCurrentVelocity() => currentVelocity;
    public float GetCurrentSpeed() => currentVelocity.magnitude;
    public bool IsMoving() => currentVelocity.magnitude > 0.1f;
}