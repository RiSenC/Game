using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    public Transform barrelPoint;
    public GameObject bulletPrefab;
    public TurretConfig turretConfig;
    
    private Camera mainCamera;
    private float lastFireTime = 0f;
    private PlayerInput playerInput;
    private InputAction fireAction;
    private Vector3 mouseWorldPos;
    
    void Start()
    {
        mainCamera = Camera.main;
        playerInput = GetComponentInParent<PlayerInput>();
        fireAction = playerInput.actions["Fire"];
    }
    
    void Update()
    {
        UpdateMousePosition();
        AimAtMouse();
        
        if (fireAction.IsPressed())
        {
            TryFire();
        }
    }
    
    void UpdateMousePosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
    
    void AimAtMouse()
    {
        Vector2 direction = (mouseWorldPos - barrelPoint.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        float currentAngle = transform.eulerAngles.z;
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
        float newAngle = currentAngle + Mathf.Clamp(angleDiff, -turretConfig.turretRotationSpeed * Time.deltaTime, turretConfig.turretRotationSpeed * Time.deltaTime);
        
        transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);
    }
    
    void TryFire()
    {
        if (Time.time - lastFireTime < turretConfig.fireRate)
            return;
        
        lastFireTime = Time.time;
        Fire();
    }
    
    void Fire()
    {
        GameObject bulletObj = Instantiate(bulletPrefab, barrelPoint.position, Quaternion.identity);
        
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 fireDirection = barrelPoint.up;
            bullet.Initialize(fireDirection, turretConfig.bulletSpeed, turretConfig.bulletDamage);
        }
    }
}