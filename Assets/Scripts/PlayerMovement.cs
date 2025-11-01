using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public float rotationSpeed = 180f;
    public float driftCompensation = 2f;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private InputAction moveAction;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        
        // Настройка физики для плавного движения
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2f;
    }
    
    void FixedUpdate()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        HandleMovement();
        HandleRotation();
        HandleDriftCompensation();
    }
    
    void HandleMovement()
    {
        Vector2 currentVelocity = rb.linearVelocity;
        Vector2 forwardDirection = transform.up;
        
        // Только прямое движение вперед/назад (без бокового)
        float currentForwardSpeed = Vector2.Dot(currentVelocity, forwardDirection);
        float targetSpeed = moveInput.y * maxSpeed;
        
        // Плавное ускорение и торможение
        float speedDiff = targetSpeed - currentForwardSpeed;
        float accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float force = speedDiff * accelerationRate;
        
        rb.AddForce(forwardDirection * force);
        
        // Ограничение максимальной скорости
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    
    void HandleRotation()
    {
        if (moveInput.x != 0)
        {
            float rotation = -moveInput.x * rotationSpeed * Time.fixedDeltaTime;
            
            // Меньше вращения на высоких скоростях (реалистичнее)
            float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            rotation *= Mathf.Lerp(1f, 0.6f, speedFactor);
            
            rb.MoveRotation(rb.rotation + rotation);
        }
    }
    
    void HandleDriftCompensation()
    {
        // Уменьшает боковой дрифт (как в гоночных играх)
        Vector2 forwardVelocity = transform.up * Vector2.Dot(rb.linearVelocity, transform.up);
        Vector2 rightVelocity = transform.right * Vector2.Dot(rb.linearVelocity, transform.right);
        
        rb.linearVelocity = forwardVelocity + rightVelocity * 0.3f;
    }
}