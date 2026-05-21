using System;
using UnityEngine;

[Serializable]
public enum RunRoomType
{
    Combat,
    Shop,
    Engineer,
    Event,
    Elite
}

[CreateAssetMenu(fileName = "RoomLayout", menuName = "Roguelike/Room Layout")]
public class RoomLayoutDefinition : ScriptableObject
{
    public string displayName = "Room";
    public RunRoomType roomType = RunRoomType.Combat;
    public int minFloor;
    public int maxFloor = 99;
    public float selectionWeight = 1f;
    public int waveCountOverride;
    public Vector2 playerSpawn = Vector2.zero;
    public Vector2 rewardSpawn = new Vector2(0f, 0.5f);
    public Vector2[] enemySpawnPositions;
    public RoomEnemySpawnEntry[] enemySpawns;
    public RoomWaveDefinition[] waves;
}

[Serializable]
public class RoomEnemySpawnEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
}

[Serializable]
public class RoomWaveDefinition
{
    public string waveName = "Wave";
    public Vector2[] enemySpawnPositions;
    public RoomEnemySpawnEntry[] enemySpawns;
}
