using UnityEngine;

[CreateAssetMenu(fileName = "ChassisStats", menuName = "ScriptableObjects/ChassisStats")]
public class ChassisStats : ScriptableObject
{
    public ChassisType type;
    
    [Header("Health & Armor")]
    public float maxHealthModifier = 1f;      // Light: 0.7, Medium: 1.0, Heavy: 1.3
    public float armorFlatModifier = 1f;
    public float armorPercentModifier = 1f;
    
    [Header("Locomotion")]
    public float speedMultiplier = 1f;        // Light: 1.3, Medium: 1.0, Heavy: 0.7
    public float accelerationMultiplier = 1f;
    public float turningMultiplier = 1f;
    
}
