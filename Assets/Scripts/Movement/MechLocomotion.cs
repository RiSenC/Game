using UnityEngine;

/// <summary>
/// Mech locomotion (2-4 legs).
/// Slower and less maneuverable than spiders.
/// </summary>
public class MechLocomotion : LeggedLocomotion
{
    private void Awake()
    {
        maxSpeed = 8f;
        acceleration = 4f;
        rotationSpeed = 150f;
    }
    
    public override void ApplyChassisModifiers(ChassisStats stats)
    {
        base.ApplyChassisModifiers(stats);
        // Mechs are already slower, so apply mods more conservatively
    }
}