using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    private const float DefaultHitscanRange = 40f;
    private const float TracerLifetime = 0.05f;

    public Transform barrelPoint;
    public GameObject bulletPrefab;
    public TurretConfig turretConfig;
    
    private Camera mainCamera;
    private float lastFireTime = 0f;
    private PlayerInput playerInput;
    private InputAction fireAction;
    private Vector3 mouseWorldPos;
    private Rigidbody2D vehicleRigidbody;
    
    private int bulletsFiredInBurst = 0;
    private float timeSinceLastShot = 0f;
    private float currentAccuracy = 0f;
    
    [Header("Accuracy Settings")]
    public int accuracyThreshold = 10;
    public float accuracyIncreasePerShot = 1.5f;
    public float standingAccuracyRecoveryRate = 3f;
    public float movingAccuracyDecayDelay = 0.5f;
    public float movingAccuracyDecayRate = 8f;

    [Header("Hitscan")]
    public LayerMask hitLayers = Physics2D.DefaultRaycastLayers;
    public float tracerWidth = 0.06f;
    public Color tracerColor = new Color(1f, 0.9f, 0.4f, 0.9f);
    
    void Start()
    {
        mainCamera = Camera.main;
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            fireAction = playerInput.actions["Fire"];

        vehicleRigidbody = GetComponentInParent<Rigidbody2D>();
        currentAccuracy = 0f;
    }
    
    void Update()
    {
        if (barrelPoint == null || turretConfig == null || mainCamera == null)
            return;

        UpdateMousePosition();
        AimAtMouse();
        UpdateAccuracy();
        
        if (fireAction != null && fireAction.IsPressed())
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
        
        bool isMoving = vehicleRigidbody != null && vehicleRigidbody.linearVelocity.sqrMagnitude > 0.01f;
        if (isMoving)
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
        float currentSpread = turretConfig.baseSpreadAngle + GetCurrentSpread();
        
        for (int i = 0; i < turretConfig.bulletsPerShot; i++)
        {
            float spreadOffset = Random.Range(-currentSpread, currentSpread);
            Vector2 bulletDir = Quaternion.Euler(0, 0, spreadOffset) * fireDirection;
            if (turretConfig.weaponType == TurretConfig.WeaponType.Bullet)
                FireHitscan(bulletDir);
            else
                SpawnProjectile(bulletDir);
        }
        
        if (bulletsFiredInBurst >= accuracyThreshold)
            currentAccuracy += accuracyIncreasePerShot;
        
        bulletsFiredInBurst++;
    }
    
    void FireHitscan(Vector2 direction)
    {
        Vector2 origin = barrelPoint.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, DefaultHitscanRange, hitLayers);
        RaycastHit2D hit = default;
        bool foundHit = false;

        foreach (RaycastHit2D candidate in hits)
        {
            if (candidate.collider == null)
                continue;

            if (candidate.collider.transform.root == transform.root)
                continue;

            hit = candidate;
            foundHit = true;
            break;
        }

        Vector2 endPoint = foundHit ? hit.point : origin + direction * DefaultHitscanRange;

        if (foundHit && hit.collider.TryGetComponent<HealthSystem>(out var health))
            health.TakeDamage(turretConfig.bulletDamage, -direction);

        SpawnTracer(origin, endPoint);
    }

    void SpawnProjectile(Vector2 direction)
    {
        if (bulletPrefab == null)
            return;

        GameObject bulletObj = Instantiate(bulletPrefab, barrelPoint.position, Quaternion.identity);
        ProjectileBase projectile = bulletObj.GetComponent<ProjectileBase>();

        if (projectile != null)
        {
            Vector2 inheritedVelocity = vehicleRigidbody != null ? vehicleRigidbody.linearVelocity : Vector2.zero;
            Vector2 bulletVelocity = direction * turretConfig.bulletSpeed + inheritedVelocity;
            projectile.Init(turretConfig.bulletDamage, bulletVelocity.magnitude);
            Rigidbody2D bulletRb = bulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
                bulletRb.linearVelocity = bulletVelocity;
        }
    }

    void SpawnTracer(Vector2 start, Vector2 end)
    {
        GameObject tracer = new GameObject("Tracer");
        tracer.transform.position = start;

        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = tracerWidth;
        line.endWidth = tracerWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = tracerColor;
        line.endColor = tracerColor;
        line.sortingOrder = 10;

        Destroy(tracer, TracerLifetime);
    }
}
