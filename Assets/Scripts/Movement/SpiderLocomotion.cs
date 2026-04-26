using UnityEngine;

/// <summary>
/// Spider locomotion (6-8 legs).
/// Faster and more maneuverable than mechs.
/// </summary>
public class SpiderLocomotion : LeggedLocomotion
{
    private void Awake()
    {
        maxSpeed = 14f;
        acceleration = 6f;
        rotationSpeed = 220f;
    }
}