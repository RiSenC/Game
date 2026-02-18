using UnityEngine;
using UnityEngine.InputSystem;

public class SmoothCameraFollowWithCursor : MonoBehaviour
{
    public Transform playerTarget;
    public float smoothTime = 0.125f;
    public Vector3 cameraOffset = new Vector3(0, 0, -10);
    
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
        if (playerTarget == null || mainCamera == null) return;
        
        // Target position at player + offset
        Vector3 targetPosition = playerTarget.position + cameraOffset;
        
        // Get mouse offset relative to player
        Vector3 mouseOffset = GetMouseWorldOffset();
        targetPosition += mouseOffset * mouseInfluence;
        
        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
    
    Vector3 GetMouseWorldOffset()
    {
        if (Mouse.current == null) return Vector3.zero;
        
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        Vector3 offset = mouseWorldPos - playerTarget.position;
        offset.z = 0;
        
        // Clamp maximum offset
        if (offset.magnitude > maxMouseOffset)
        {
            offset = offset.normalized * maxMouseOffset;
        }
        
        return offset;
    }
}