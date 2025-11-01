using UnityEngine;
using UnityEngine.InputSystem; // ДОБАВИТЬ ЭТУ СТРОКУ!

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Mouse Offset")]
    public float mouseInfluence = 0.3f;
    public float maxMouseOffset = 2f;
    
    private Camera mainCamera;
    private Vector3 velocity = Vector3.zero;
    
    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Позиция игрока
        Vector3 targetPosition = target.position + offset;
        
        // Смещение к курсору
        Vector3 mouseOffset = GetMouseWorldOffset();
        targetPosition += mouseOffset * mouseInfluence;
        
        // Плавное следование
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
    }
    
    Vector3 GetMouseWorldOffset()
    {
        // Проверяем доступность мыши
        if (Mouse.current == null) return Vector3.zero;
        
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        Vector3 offset = mouseWorldPos - target.position;
        offset.z = 0;
        
        // Ограничиваем максимальное смещение
        if (offset.magnitude > maxMouseOffset)
        {
            offset = offset.normalized * maxMouseOffset;
        }
        
        return offset;
    }
}