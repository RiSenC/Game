using UnityEngine;

/// <summary>
/// Laser weapon — continuous beam, drains energy per second.
/// </summary>
public class LaserWeaponImproved : WeaponBase
{
    [Header("Laser")]
    public float energyDrainPerSecond = 3f;
    public float maxRange = 25f;
    public LayerMask hitLayers;
    
    private LineRenderer beamRenderer;
    private bool isFiring = false;
    private void Awake()
    {
        weaponName = "Лазер";
        damage = 20f;           // per second
        fireRate = 999f;        // n/a for laser
        baseSpread = 0f;
        turretRotationSpeed = 350f;
        energyCost = 0;         // handled via drain
    }
    
    private void Start()
    {
        beamRenderer = GetComponentInChildren<LineRenderer>();
        if (beamRenderer != null)
            beamRenderer.enabled = false;
    }
    
    protected override bool CanFire(ChassisController chassis)
    {
        return true;
    }
    
    protected override void Fire(Transform firePoint, float spreadMultiplier)
    {
        isFiring = true;
    }
    
    private void Update()
    {
        base.Update();
        
        if (isFiring)
            FireContinuous();
    }
    
    private void FireContinuous()
    {
        Vector2 origin = (Vector2)transform.position;
        Vector2 direction = (Vector2)transform.right;
        
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxRange, hitLayers);
        Vector3 endPoint = hit ? (Vector3)hit.point : origin + direction * maxRange;
        
        if (beamRenderer != null)
        {
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, endPoint);
        }
        
        if (hit && hit.collider.TryGetComponent<HealthSystem>(out var health))
        {
            health.TakeDamage(damage * Time.deltaTime, -direction);
        }
    }
    
    public override void OnFireReleased()
    {
        isFiring = false;
        if (beamRenderer != null)
            beamRenderer.enabled = false;
    }
    
    public override void OnUnequipped()
    {
        isFiring = false;
        if (beamRenderer != null)
            beamRenderer.enabled = false;
    }
}
