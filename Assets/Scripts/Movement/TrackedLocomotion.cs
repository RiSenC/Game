using UnityEngine;

/// <summary>
/// Tracked vehicle (tanks with treads/gushenicy).
/// Steers only while moving. A/D always steer in their respective direction.
/// </summary>
public class TrackedLocomotion : WheeledLocomotion
{
    // Inherits steering behavior from WheeledLocomotion
    // Could add tread animation or dust effects here
}