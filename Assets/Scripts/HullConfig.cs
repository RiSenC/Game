using UnityEngine;

[CreateAssetMenu(fileName = "New Hull Config", menuName = "Tank/Hull Config")]
public class HullConfig : ScriptableObject
{
    [Header("Hull Stats")]
    public float maxSpeed = 8f;
    public float acceleration = 25f;
    public float deceleration = 30f;
    public float rotationSpeed = 180f;
}