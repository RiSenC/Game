using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    [Header("Current Configuration")]
    public HullConfig currentHull;
    public TurretConfig currentTurret;
    
    private PlayerMovement playerMovement;
    private TurretController turretController;
    private SpriteRenderer hullRenderer;
    
    void Awake()
    {
        // Get components in Awake to ensure they're available for Start
        playerMovement = GetComponent<PlayerMovement>();
        turretController = GetComponentInChildren<TurretController>();
        hullRenderer = GetComponent<SpriteRenderer>();
    }
    
    void Start()
    {
        // Apply configs if they exist
        if (currentHull != null)
        {
            ApplyHullConfig(currentHull);
        }
        
        if (currentTurret != null)
        {
            ApplyTurretConfig(currentTurret);
        }
    }
    
    public void ChangeHull(HullConfig newHull)
    {
        if (newHull == null) return;
        
        currentHull = newHull;
        ApplyHullConfig(newHull);
    }
    
    public void ChangeTurret(TurretConfig newTurret)
    {
        if (newTurret == null) return;
        
        currentTurret = newTurret;
        ApplyTurretConfig(newTurret);
    }
    
    void ApplyHullConfig(HullConfig hull)
    {
        // Update PlayerMovement config
        if (playerMovement != null)
        {
            playerMovement.SetHullConfig(hull);
        }
        
        // Update visuals
        if (hullRenderer != null)
        {
            if (hull.hullSprite != null)
                hullRenderer.sprite = hull.hullSprite;
            
            // Apply scale
            transform.localScale = hull.hullScale;
        }
    }
    
    void ApplyTurretConfig(TurretConfig turret)
    {
        if (turretController != null)
        {
            // Direct assignment instead of calling a non-existent method
            turretController.turretConfig = turret;
            
            // Optional: Update turret visuals here
            SpriteRenderer turretRenderer = turretController.GetComponent<SpriteRenderer>();
            if (turretRenderer != null && turret.turretSprite != null)
            {
                turretRenderer.sprite = turret.turretSprite;
                turretRenderer.color = turret.turretColor;
            }
            
            // Update turret scale
            turretController.transform.localScale = turret.turretScale;
        }
    }
    
    // Optional: Method to sync config at runtime if needed
    void OnValidate()
    {
        // Editor-only validation to help catch issues
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (hullRenderer == null)
            hullRenderer = GetComponent<SpriteRenderer>();
        if (turretController == null)
            turretController = GetComponentInChildren<TurretController>();
    }
}