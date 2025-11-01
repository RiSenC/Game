using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private PlayerInput playerInput;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
    }
    
    // Этот метод вызывается автоматически новой системой ввода
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    void FixedUpdate()
    {
        // Движение вперед/назад (ось Y)
        if (moveInput.y != 0)
        {
            rb.AddForce(transform.up * moveInput.y * moveSpeed);
        }
        
        // Поворот (ось X)
        if (moveInput.x != 0)
        {
            float rotation = -moveInput.x * rotationSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, 0, rotation);
        }
        
        // Добавляем сопротивление для плавного торможения
        if (moveInput.magnitude < 0.1f)
        {
            rb.linearVelocity *= 0.95f;
            rb.angularVelocity *= 0.95f;
        }
    }
    
    // Для отладки - видим ввод в консоли
    void Update()
    {
        if (moveInput != Vector2.zero)
        {
            Debug.Log($"Move Input: {moveInput}");
        }
    }
}