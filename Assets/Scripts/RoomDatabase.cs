using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomDatabase", menuName = "Roguelike/Room Database")]
public class RoomDatabase : ScriptableObject
{
    public List<RoomLayoutDefinition> rooms = new List<RoomLayoutDefinition>();
}
