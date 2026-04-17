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
    private PlayerMovement playerMovement;
    
    private int bulletsFiredInBurst = 0;
    private float timeSinceLastShot = 0f;
    private float currentAccuracy = 0f;
    
    [Header("Accuracy Settings")]
    public int accuracyThreshold = 10;
    public float accuracyIncreasePerShot = 1.5f;
    public float standingAccuracyRecoveryRate = 3f;
    public float movingAccuracyDecayDelay = 0.5f;
    public float movingAccuracyDecayRate = 8f;
    
    void Start()
    {
        mainCamera = Camera.main;
        playerInput = GetComponentInParent<PlayerInput>();
        fireAction = playerInput.actions["Fire"];
        playerMovement = GetComponentInParent<PlayerMovement>();
        currentAccuracy = 0f;
    }
    
    void Update()
    {
        UpdateMousePosition();
        AimAtMouse();
        UpdateAccuracy();
        
        if (fireAction.IsPressed())
        {
            TryFire();
        }
    }

    public void SetTurretConfig(TurretConfig newConfig)
    {
        turretConfig = newConfig;
        
        // Update visual components if needed
        SpriteRenderer turretRenderer = GetComponent<SpriteRenderer>();
        if (turretRenderer != null && newConfig.turretSprite != null)
        {
            turretRenderer.sprite = newConfig.turretSprite;
            turretRenderer.color = newConfig.turretColor;
        }
        
        transform.localScale = newConfig.turretScale;
    }
    
    void UpdateMousePosition()
    {
        if (Mouse.current != null)
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z);
            mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }
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
    
    void UpdateAccuracy()
    {
        timeSinceLastShot += Time.deltaTime;
        
        if (playerMovement.IsMoving())
        {
            if (timeSinceLastShot > movingAccuracyDecayDelay)
            {
                currentAccuracy += movingAccuracyDecayRate * Time.deltaTime;
            }
        }
        else
        {
            currentAccuracy -= standingAccuracyRecoveryRate * Time.deltaTime;
        }
        
        currentAccuracy = Mathf.Clamp(currentAccuracy, 0f, turretConfig.maxSpreadAngle);
    }
    
    float GetCurrentSpread()
    {
        if (bulletsFiredInBurst < accuracyThreshold)
            return 0f;
        return currentAccuracy;
    }
    
    void TryFire()
    {
        if (Time.time - lastFireTime < turretConfig.fireRate)
            return;
        
        lastFireTime = Time.time;
        timeSinceLastShot = 0f;
        Fire();
    }
    
    void Fire()
    {
        Vector2 fireDirection = barrelPoint.up;
        float currentSpread = GetCurrentSpread();
        
        for (int i = 0; i < turretConfig.bulletsPerShot; i++)
        {
            float spreadOffset = Random.Range(-currentSpread, currentSpread);
            Vector2 bulletDir = Quaternion.Euler(0, 0, spreadOffset) * fireDirection;
            SpawnBullet(bulletDir);
        }
        
        if (bulletsFiredInBurst >= accuracyThreshold)
            currentAccuracy += accuracyIncreasePerShot;
        
        bulletsFiredInBurst++;
    }
    
    void SpawnBullet(Vector2 direction)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, barrelPoint.position, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        
        if (bullet != null)
        {
            // Bullet velocity = direction * speed + vehicle velocity (inertia)
            Vector2 bulletVelocity = direction * turretConfig.bulletSpeed + playerMovement.GetCurrentVelocity();
            bullet.Initialize(bulletVelocity, turretConfig.bulletDamage);
        }
    }
}