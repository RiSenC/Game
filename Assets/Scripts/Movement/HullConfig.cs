using UnityEngine;

[CreateAssetMenu(fileName = "New Hull Config", menuName = "Tank/Hull Config")]
public class HullConfig : ScriptableObject
{
    public enum HullType { Tank, Car }
    
    [Header("Hull Type")]
    public HullType hullType = HullType.Tank;
    
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 25f;
    public float rotationSpeed = 180f;
    
    [Header("Car-Specific")]
    public float steeringResponse = 150f;
    public float handbrakeSteeringBoost = 2f;
    
    [Header("Combat")]
    public float maxHealth = 100f;
    public float armor = 10f;
    
    [Header("Visual")]
    public Sprite hullSprite;
    public Vector3 hullScale = Vector3.one;
}