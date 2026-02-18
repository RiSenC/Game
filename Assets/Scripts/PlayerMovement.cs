using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public HullConfig hullConfig;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private InputAction moveAction;
    private float currentForwardSpeed = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    void FixedUpdate()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        HandleMovement();
        HandleRotation();
    }
    
    void HandleMovement()
    {
        Vector2 forwardDirection = transform.up;
        float targetSpeed = moveInput.y * hullConfig.maxSpeed;
        
        float accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) 
            ? hullConfig.acceleration 
            : hullConfig.deceleration;
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, accelerationRate * Time.fixedDeltaTime);
        
        Vector2 targetVelocity = forwardDirection * currentForwardSpeed;
        if (Vector2.Distance(rb.linearVelocity, targetVelocity) > 0.01f)
        {
            rb.linearVelocity = targetVelocity;
        }
    }
    
    void HandleRotation()
    {
        float rotationInput = moveInput.x;
        
        if (moveInput.y < -0.1f)
        {
            rotationInput = -rotationInput;
        }
        
        if (rotationInput != 0)
        {
            float rotation = -rotationInput * hullConfig.rotationSpeed * Time.fixedDeltaTime;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / hullConfig.maxSpeed);
            rotation *= Mathf.Lerp(1f, 0.5f, speedFactor);
            
            rb.MoveRotation(rb.rotation + rotation);
        }
    }
}