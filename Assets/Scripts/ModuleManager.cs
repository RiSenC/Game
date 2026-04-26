using System;
using System.Collections.Generic;

public class ModuleManager
{
    // Dictionary to hold available modules
    private Dictionary<string, Module> availableModules;

    // Equipment slots for Chassis, Weapons, and Locomotion
    private Module chassis;
    private Module weapon;
    private Module locomotion;

    public ModuleManager()
    {
        availableModules = new Dictionary<string, Module>();
        // Initialize equipment slots
        chassis = null;
        weapon = null;
        locomotion = null;
    }

    // Method to register a new module
    public void RegisterModule(string id, Module module)
    {
        if (!availableModules.ContainsKey(id))
        {
            availableModules[id] = module;
        }
    }

    // Method to swap chassis
    public void SwapChassis(string id)
    {
        if (availableModules.ContainsKey(id))
        {
            chassis = availableModules[id];
            RecalculateStats(); // Recalculate stats after swap
        }
    }

    // Method to swap weapon
    public void SwapWeapon(string id)
    {
        if (availableModules.ContainsKey(id))
        {
            weapon = availableModules[id];
            RecalculateStats(); // Recalculate stats after swap
        }
    }

    // Method to swap locomotion system
    public void SwapLocomotion(string id)
    {
        if (availableModules.ContainsKey(id))
        {
            locomotion = availableModules[id];
            RecalculateStats(); // Recalculate stats after swap
        }
    }

    // Method to recalculate stats based on current modules
    private void RecalculateStats()
    {
        // Example logic for stat recalculation
        // Update stats based on currently equipped modules
    }

    // Query methods
    public List<string> GetAvailableModules()
    {
        return new List<string>(availableModules.Keys);
    }
}