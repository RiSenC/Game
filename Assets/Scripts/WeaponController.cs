using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform barrelPoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.2f;
    public float bulletSpeed = 15f;
    public float weaponRotationSpeed = 360f;
    
    private float nextFireTime;
    private InputAction fireAction;
    private Camera mainCamera;
    
    void Start()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        fireAction = playerInput.actions["Fire"];
        mainCamera = Camera.main;
        
        // Создаем точку выстрела если не назначена
        if (barrelPoint == null)
        {
            GameObject point = new GameObject("BarrelPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = new Vector3(0, 0.5f, 0);
            barrelPoint = point.transform;
        }
    }
    
    void Update()
    {
        RotateWeaponTowardsMouse();
        HandleShooting();
    }
    
    void RotateWeaponTowardsMouse()
    {
        // Получаем позицию мыши в мировых координатах
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePosition.z = 0;
        
        // Вычисляем направление к мыши
        Vector2 direction = (mousePosition - transform.position).normalized;
        
        // Плавный поворот башни
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, weaponRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }
    
    void HandleShooting()
    {
        if (fireAction.IsPressed() && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet prefab not assigned!");
            return;
        }
        
        GameObject bullet = Instantiate(bulletPrefab, barrelPoint.position, barrelPoint.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = barrelPoint.up * bulletSpeed;
        }
        
        // Автоуничтожение пули через 3 секунды
        Destroy(bullet, 3f);
    }
}