using UnityEngine;

[CreateAssetMenu(fileName = "New Turret Config", menuName = "Tank/Turret Config")]
public class TurretConfig : ScriptableObject
{
    [Header("Turret Stats")]
    public float turretRotationSpeed = 45f;
    public float bulletSpeed = 20f;
    public float fireRate = 0.1f;
    public float bulletDamage = 10f;
}