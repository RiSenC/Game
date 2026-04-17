using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles DoT (damage over time) effects on a unit.
/// </summary>
public class DamageOverTimeComponent : MonoBehaviour
{
    private class DotEffect
    {
        public float damagePerSecond;
        public float timeRemaining;
    }
    
    private HealthSystem healthSystem;
    private List<DotEffect> activeEffects = new List<DotEffect>();
    
    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }
    
    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].timeRemaining -= Time.deltaTime;
            
            if (activeEffects[i].timeRemaining <= 0)
            {
                activeEffects.RemoveAt(i);
            }
            else
            {
                healthSystem.TakeDamage(activeEffects[i].damagePerSecond * Time.deltaTime);
            }
        }
    }
    
    public void ApplyDamage(float damagePerSecond, float duration)
    {
        activeEffects.Add(new DotEffect { damagePerSecond = damagePerSecond, timeRemaining = duration });
    }
}