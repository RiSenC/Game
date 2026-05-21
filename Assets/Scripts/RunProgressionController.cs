using System;
using System.Collections.Generic;
using UnityEngine;

public class RunProgressionController : MonoBehaviour
{
    [Header("Core")]
    public PlayerModuleController player;
    public RoomDatabase roomDatabase;
    public int totalFloors = 5;
    public int choicesPerFloor = 2;

    [Header("Fallback Enemies")]
    public GameObject basicEnemyPrefab;
    public GameObject heavyEnemyPrefab;
    public GameObject shooterEnemyPrefab;

    [Header("Rewards")]
    public HullConfig[] hullRewardPool;
    public TurretConfig[] turretRewardPool;
    public LocomotionBase[] locomotionRewardPool;
    public WeaponBase[] weaponRewardPool;
    public Vector2 rewardOffset = new Vector2(0f, 1.5f);

    [Header("Rarity Odds")]
    public RarityWeightTable firstFloorDropOdds = new RarityWeightTable(50f, 40f, 9f, 1f);
    public float commonDecayPerFloor = 6f;
    public float rareGrowthPerFloor = 3.5f;
    public float epicGrowthPerFloor = 1.8f;
    public float legendaryGrowthPerFloor = 0.7f;
    public float eliteRarityBonus = 12f;
    public float shopRarityBonus = 6f;

    [Header("Encounter Scaling")]
    public float healthScalePerFloor = 0.22f;
    public float damageScalePerFloor = 0.14f;
    public float speedScalePerFloor = 0.05f;
    public int baseWaveCount = 1;
    public int extraWaveEveryFloors = 2;

    [Header("Special Rooms")]
    public int shopOfferCount = 3;
    public int engineerChoices = 2;
    public int eventScrapReward = 6;
    public float eventHealAmount = 25f;

    [Header("Map Choice Portals")]
    public Vector2 portalCenter = new Vector2(0f, 3.5f);
    public float portalSpacing = 2.5f;

    private readonly List<List<RuntimeRoomOption>> generatedFloors = new List<List<RuntimeRoomOption>>();
    private readonly List<Enemy> aliveEnemies = new List<Enemy>();
    private readonly List<GameObject> spawnedRuntimeObjects = new List<GameObject>();
    private readonly List<GameObject> activePortals = new List<GameObject>();
    private readonly List<RuntimeWave> currentWaves = new List<RuntimeWave>();

    private int currentFloor = -1;
    private int currentWaveIndex = -1;
    private Vector2 currentRewardSpawn;
    private bool useLayoutRewardSpawn;
    private RuntimeRoomOption currentRoom;
    private PlayerRunStats runStats;
    private bool eventAmbushActive;

    private void Start()
    {
        if (player == null)
            player = Object.FindFirstObjectByType<PlayerModuleController>();

        if (player != null && player.GetComponent<PlayerRunStats>() == null)
            player.gameObject.AddComponent<PlayerRunStats>();

        runStats = player != null ? player.GetComponent<PlayerRunStats>() : null;
        DestroySceneEnemies();
        BuildMap();
        LoadFirstRoom();
    }

    public void SelectNextRoom(int floorIndex, int optionIndex)
    {
        if (floorIndex < 0 || floorIndex >= generatedFloors.Count)
            return;
        if (optionIndex < 0 || optionIndex >= generatedFloors[floorIndex].Count)
            return;

        ClearPortals();
        ClearRuntimeObjects();

        currentFloor = floorIndex;
        currentRoom = generatedFloors[floorIndex][optionIndex];
        SpawnRoom(currentRoom);
    }

    public void TriggerEventAmbush()
    {
        if (currentRoom == null || currentRoom.roomType != RunRoomType.Event || eventAmbushActive)
            return;

        eventAmbushActive = true;
        currentWaves.Clear();
        currentWaveIndex = -1;
        currentWaves.Add(BuildFallbackWave(currentFloor, true));
        StartNextWave();
    }

    public void SpawnEventRewardModule()
    {
        ModulePickup pickup = SpawnModuleReward(GetScaledRarityOdds(currentFloor, false, true), false);
        if (pickup != null)
            pickup.pickupDelay = 0f;
    }

    private void BuildMap()
    {
        generatedFloors.Clear();

        for (int floor = 0; floor < totalFloors; floor++)
        {
            List<RuntimeRoomOption> options = new List<RuntimeRoomOption>();
            HashSet<RunRoomType> usedTypes = new HashSet<RunRoomType>();
            int optionCount = Mathf.Max(1, choicesPerFloor);

            for (int i = 0; i < optionCount; i++)
            {
                RuntimeRoomOption option = CreateRoomOption(floor, i, usedTypes);
                options.Add(option);
                usedTypes.Add(option.roomType);
            }

            generatedFloors.Add(options);
        }
    }

    private RuntimeRoomOption CreateRoomOption(int floorIndex, int optionIndex, HashSet<RunRoomType> usedTypes)
    {
        RunRoomType roomType = ChooseRoomType(floorIndex, usedTypes);
        RoomLayoutDefinition layout = PickLayout(roomType, floorIndex);
        string displayName = layout != null ? layout.displayName : $"{roomType} {floorIndex + 1}-{optionIndex + 1}";

        return new RuntimeRoomOption
        {
            displayName = displayName,
            layout = layout,
            floorIndex = floorIndex,
            optionIndex = optionIndex,
            roomType = roomType
        };
    }

    private RunRoomType ChooseRoomType(int floorIndex, HashSet<RunRoomType> usedTypes)
    {
        if (floorIndex == 0)
            return usedTypes.Contains(RunRoomType.Combat) ? RunRoomType.Event : RunRoomType.Combat;

        List<RoomTypeWeight> weights = new List<RoomTypeWeight>
        {
            new RoomTypeWeight(RunRoomType.Combat, 42f),
            new RoomTypeWeight(RunRoomType.Shop, 16f),
            new RoomTypeWeight(RunRoomType.Engineer, 14f),
            new RoomTypeWeight(RunRoomType.Event, 14f),
            new RoomTypeWeight(RunRoomType.Elite, 14f),
        };

        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (usedTypes.Contains(weights[i].roomType))
                continue;
            if (weights[i].roomType == RunRoomType.Elite && floorIndex < 1)
                continue;

            total += weights[i].weight;
        }

        if (total <= 0f)
            return RunRoomType.Combat;

        float roll = UnityEngine.Random.value * total;
        float cursor = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (usedTypes.Contains(weights[i].roomType))
                continue;
            if (weights[i].roomType == RunRoomType.Elite && floorIndex < 1)
                continue;

            cursor += weights[i].weight;
            if (roll <= cursor)
                return weights[i].roomType;
        }

        return RunRoomType.Combat;
    }

    private RoomLayoutDefinition PickLayout(RunRoomType roomType, int floorIndex)
    {
        if (roomDatabase == null || roomDatabase.rooms == null || roomDatabase.rooms.Count == 0)
            return null;

        List<RoomLayoutDefinition> candidates = new List<RoomLayoutDefinition>();
        float totalWeight = 0f;

        for (int i = 0; i < roomDatabase.rooms.Count; i++)
        {
            RoomLayoutDefinition room = roomDatabase.rooms[i];
            if (room == null)
                continue;
            if (room.roomType != roomType)
                continue;
            if (floorIndex < room.minFloor || floorIndex > room.maxFloor)
                continue;

            candidates.Add(room);
            totalWeight += Mathf.Max(0.01f, room.selectionWeight);
        }

        if (candidates.Count == 0)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;
        float cursor = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cursor += Mathf.Max(0.01f, candidates[i].selectionWeight);
            if (roll <= cursor)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private void LoadFirstRoom()
    {
        if (generatedFloors.Count == 0 || generatedFloors[0].Count == 0)
            return;

        SelectNextRoom(0, UnityEngine.Random.Range(0, generatedFloors[0].Count));
    }

    private void SpawnRoom(RuntimeRoomOption option)
    {
        aliveEnemies.Clear();
        currentWaves.Clear();
        currentWaveIndex = -1;
        currentRewardSpawn = option.layout != null ? option.layout.rewardSpawn : rewardOffset;
        useLayoutRewardSpawn = option.layout != null;
        eventAmbushActive = false;

        if (player != null)
        {
            Vector2 spawn = option.layout != null ? option.layout.playerSpawn : Vector2.zero;
            player.transform.position = spawn;
            player.RefreshRuntimeBonuses();
        }

        switch (option.roomType)
        {
            case RunRoomType.Combat:
            case RunRoomType.Elite:
                BuildEncounterWaves(option);
                StartNextWave();
                break;
            case RunRoomType.Shop:
                SpawnShopOffers();
                OpenPathForward();
                break;
            case RunRoomType.Engineer:
                SpawnEngineerChoices();
                OpenPathForward();
                break;
            case RunRoomType.Event:
                SpawnEventNode();
                OpenPathForward();
                break;
        }
    }

    private void BuildEncounterWaves(RuntimeRoomOption option)
    {
        RoomLayoutDefinition layout = option.layout;
        if (layout != null && layout.waves != null && layout.waves.Length > 0)
        {
            for (int i = 0; i < layout.waves.Length; i++)
            {
                RoomWaveDefinition wave = layout.waves[i];
                if (wave == null)
                    continue;

                currentWaves.Add(new RuntimeWave
                {
                    spawnEntries = wave.enemySpawns,
                    spawnPositions = wave.enemySpawnPositions != null && wave.enemySpawnPositions.Length > 0
                        ? wave.enemySpawnPositions
                        : layout.enemySpawnPositions
                });
            }
        }

        if (currentWaves.Count == 0 && layout != null && layout.enemySpawns != null && layout.enemySpawns.Length > 0)
        {
            int waveCount = layout.waveCountOverride > 0 ? layout.waveCountOverride : GetFallbackWaveCount(option.floorIndex, option.roomType);
            for (int i = 0; i < waveCount; i++)
            {
                currentWaves.Add(new RuntimeWave
                {
                    spawnEntries = layout.enemySpawns,
                    spawnPositions = layout.enemySpawnPositions
                });
            }
        }

        if (currentWaves.Count == 0)
        {
            int waveCount = GetFallbackWaveCount(option.floorIndex, option.roomType);
            for (int i = 0; i < waveCount; i++)
                currentWaves.Add(BuildFallbackWave(option.floorIndex, option.roomType == RunRoomType.Elite));
        }
    }

    private RuntimeWave BuildFallbackWave(int floorIndex, bool elite)
    {
        List<RoomEnemySpawnEntry> entries = new List<RoomEnemySpawnEntry>();
        entries.Add(new RoomEnemySpawnEntry { enemyPrefab = basicEnemyPrefab, count = Mathf.Clamp(2 + floorIndex, 2, 5) });

        if (floorIndex >= 1 && heavyEnemyPrefab != null)
            entries.Add(new RoomEnemySpawnEntry { enemyPrefab = heavyEnemyPrefab, count = elite ? 2 : 1 });
        if (floorIndex >= 2 && shooterEnemyPrefab != null)
            entries.Add(new RoomEnemySpawnEntry { enemyPrefab = shooterEnemyPrefab, count = elite ? 2 : 1 });

        Vector2[] positions =
        {
            new Vector2(-4f, 2f),
            new Vector2(0f, 4.25f),
            new Vector2(4f, 2f),
            new Vector2(-2f, 0.5f),
            new Vector2(2f, 0.5f)
        };

        return new RuntimeWave
        {
            spawnEntries = entries.ToArray(),
            spawnPositions = positions
        };
    }

    private int GetFallbackWaveCount(int floorIndex, RunRoomType roomType)
    {
        int waves = baseWaveCount + floorIndex / Mathf.Max(1, extraWaveEveryFloors);
        if (roomType == RunRoomType.Elite)
            waves += 1;
        return Mathf.Clamp(waves, 1, 4);
    }

    private void StartNextWave()
    {
        currentWaveIndex++;
        if (currentWaveIndex >= currentWaves.Count)
        {
            HandleRoomResolved();
            return;
        }

        RuntimeWave wave = currentWaves[currentWaveIndex];
        SpawnWave(wave, currentFloor, currentRoom != null && currentRoom.roomType == RunRoomType.Elite);

        if (aliveEnemies.Count == 0)
            StartNextWave();
    }

    private void SpawnWave(RuntimeWave wave, int floorIndex, bool elite)
    {
        aliveEnemies.Clear();
        if (wave == null || wave.spawnEntries == null || wave.spawnEntries.Length == 0)
            return;

        Vector2[] positions = wave.spawnPositions != null && wave.spawnPositions.Length > 0
            ? wave.spawnPositions
            : new[] { new Vector2(-3f, 2f), new Vector2(0f, 4f), new Vector2(3f, 2f) };

        int spawnCursor = 0;
        for (int i = 0; i < wave.spawnEntries.Length; i++)
        {
            RoomEnemySpawnEntry entry = wave.spawnEntries[i];
            if (entry == null || entry.enemyPrefab == null)
                continue;

            for (int count = 0; count < Mathf.Max(1, entry.count); count++)
            {
                Vector2 position = positions[spawnCursor % positions.Length];
                spawnCursor++;
                SpawnEnemy(entry.enemyPrefab, position, floorIndex, elite, currentWaveIndex, spawnCursor);
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab, Vector2 position, int floorIndex, bool elite, int waveIndex, int tacticalSeed)
    {
        if (enemyPrefab == null)
            return;

        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);
        spawnedRuntimeObjects.Add(enemyObject);

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ConfigureEncounter(GetHealthScale(floorIndex, waveIndex, elite), GetSpeedScale(floorIndex, elite), GetDamageScale(floorIndex, elite), elite, tacticalSeed);
            aliveEnemies.Add(enemy);
            enemy.Died += HandleEnemyDeath;
        }
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        if (enemy != null)
            enemy.Died -= HandleEnemyDeath;

        aliveEnemies.Remove(enemy);
        if (aliveEnemies.Count > 0)
            return;

        if (eventAmbushActive)
        {
            eventAmbushActive = false;
            OpenPathForward();
            return;
        }

        if (currentRoom != null && (currentRoom.roomType == RunRoomType.Combat || currentRoom.roomType == RunRoomType.Elite) && currentWaveIndex + 1 < currentWaves.Count)
        {
            StartNextWave();
            return;
        }

        HandleRoomResolved();
    }

    private void HandleRoomResolved()
    {
        if (currentRoom == null)
            return;

        switch (currentRoom.roomType)
        {
            case RunRoomType.Combat:
                SpawnModuleReward(GetScaledRarityOdds(currentFloor, false, false), false);
                break;
            case RunRoomType.Elite:
                SpawnModuleReward(GetScaledRarityOdds(currentFloor, true, false), false);
                break;
        }

        OpenPathForward();
    }

    private void OpenPathForward()
    {
        int nextFloor = currentFloor + 1;
        if (nextFloor >= generatedFloors.Count)
        {
            Debug.Log("Run complete!");
            return;
        }

        SpawnChoicePortals(nextFloor);
    }

    private void SpawnChoicePortals(int floorIndex)
    {
        ClearPortals();

        List<RuntimeRoomOption> options = generatedFloors[floorIndex];
        float totalWidth = (options.Count - 1) * portalSpacing;
        Vector3 center = player != null ? player.transform.position : Vector3.zero;
        center += (Vector3)portalCenter;

        for (int i = 0; i < options.Count; i++)
        {
            Vector3 position = center + Vector3.right * (-totalWidth * 0.5f + i * portalSpacing);
            GameObject portalObject = new GameObject($"Choice Portal {floorIndex + 1}-{i + 1}");
            portalObject.transform.position = position;
            portalObject.AddComponent<CircleCollider2D>().radius = 0.6f;

            MapChoicePortal portal = portalObject.AddComponent<MapChoicePortal>();
            portal.controller = this;
            portal.floorIndex = floorIndex;
            portal.optionIndex = i;
            portal.choiceLabel = $"{options[i].roomType}: {options[i].displayName}";

            activePortals.Add(portalObject);
        }
    }

    private void SpawnShopOffers()
    {
        for (int i = 0; i < shopOfferCount; i++)
        {
            ModulePickup pickup = SpawnModuleReward(GetScaledRarityOdds(currentFloor, false, true), true, (i - (shopOfferCount - 1) * 0.5f) * 1.8f);
            if (pickup == null)
                continue;

            pickup.scrapCost = GetShopCost(pickup.rarity);
            pickup.RefreshVisuals();
        }
    }

    private void SpawnEngineerChoices()
    {
        List<EngineerUpgradePickup.UpgradeType> pool = new List<EngineerUpgradePickup.UpgradeType>
        {
            EngineerUpgradePickup.UpgradeType.Hull,
            EngineerUpgradePickup.UpgradeType.Turret,
            EngineerUpgradePickup.UpgradeType.Weapon,
            EngineerUpgradePickup.UpgradeType.Mobility
        };

        int choiceCount = Mathf.Clamp(engineerChoices, 1, pool.Count);
        for (int i = 0; i < choiceCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            EngineerUpgradePickup.UpgradeType upgradeType = pool[randomIndex];
            pool.RemoveAt(randomIndex);

            GameObject station = new GameObject($"Engineer {upgradeType}");
            station.transform.position = GetRoomCenter() + new Vector3((i - (choiceCount - 1) * 0.5f) * 1.8f, 0.5f, 0f);
            station.AddComponent<CircleCollider2D>().radius = 0.55f;
            EngineerUpgradePickup pickup = station.AddComponent<EngineerUpgradePickup>();
            pickup.upgradeType = upgradeType;
            spawnedRuntimeObjects.Add(station);
        }
    }

    private void SpawnEventNode()
    {
        GameObject eventObject = new GameObject("Random Event");
        eventObject.transform.position = GetRoomCenter() + new Vector3(0f, 0.5f, 0f);
        eventObject.AddComponent<CircleCollider2D>().radius = 0.6f;

        RandomEventPickup pickup = eventObject.AddComponent<RandomEventPickup>();
        pickup.controller = this;
        pickup.scrapReward = eventScrapReward + currentFloor * 2;
        pickup.healAmount = eventHealAmount;
        pickup.outcome = RollEventOutcome();

        spawnedRuntimeObjects.Add(eventObject);
    }

    private RandomEventPickup.EventOutcome RollEventOutcome()
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.35f)
            return RandomEventPickup.EventOutcome.ScrapCache;
        if (roll < 0.65f)
            return RandomEventPickup.EventOutcome.FieldRepair;
        if (roll < 0.9f)
            return RandomEventPickup.EventOutcome.FreeModule;
        return RandomEventPickup.EventOutcome.Ambush;
    }

    private ModulePickup SpawnModuleReward(RarityWeightTable rarityTable, bool isShop, float horizontalOffset = 0f)
    {
        if (player == null)
            return null;

        List<ModulePickup.ModuleType> availableRewards = GetAvailableRewardTypes();
        if (availableRewards.Count == 0)
            return null;

        ModuleRarity rarity = RollRarity(rarityTable);
        int safety = 0;
        while (!HasAnyRewardForRarity(availableRewards, rarity) && safety < 4)
        {
            rarity = DowngradeRarity(rarity);
            safety++;
        }

        List<ModulePickup.ModuleType> eligibleTypes = GetEligibleTypesForRarity(availableRewards, rarity);
        if (eligibleTypes.Count == 0)
            return null;

        ModulePickup.ModuleType rewardType = eligibleTypes[UnityEngine.Random.Range(0, eligibleTypes.Count)];
        Vector3 spawnPosition = useLayoutRewardSpawn
            ? (Vector3)currentRewardSpawn + new Vector3(horizontalOffset, 0f, 0f)
            : player.transform.position + (Vector3)currentRewardSpawn + new Vector3(horizontalOffset, 0f, 0f);

        GameObject reward = new GameObject(isShop ? "Shop Offer" : "Module Reward");
        reward.transform.position = spawnPosition;
        reward.AddComponent<CircleCollider2D>().radius = 0.5f;

        ModulePickup pickup = reward.AddComponent<ModulePickup>();
        pickup.destroyAfterPickup = true;
        pickup.dropPreviousModule = true;
        pickup.pickupDelay = 0.15f;
        pickup.rarity = rarity;

        if (!AssignReward(pickup, rewardType, rarity))
        {
            Destroy(reward);
            return null;
        }

        pickup.RefreshVisuals();
        reward.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        spawnedRuntimeObjects.Add(reward);
        return pickup;
    }

    private bool AssignReward(ModulePickup pickup, ModulePickup.ModuleType rewardType, ModuleRarity rarity)
    {
        pickup.moduleType = rewardType;

        switch (rewardType)
        {
            case ModulePickup.ModuleType.Hull:
            {
                HullConfig config = PickHullByRarity(rarity);
                if (config == null)
                    return false;
                pickup.hullModule = config;
                pickup.scrapCost = config.scrapCost;
                pickup.rarity = config.rarity;
                return true;
            }
            case ModulePickup.ModuleType.Turret:
            {
                TurretConfig config = PickTurretByRarity(rarity);
                if (config == null)
                    return false;
                pickup.turretModule = config;
                pickup.scrapCost = config.scrapCost;
                pickup.rarity = config.rarity;
                return true;
            }
            case ModulePickup.ModuleType.Locomotion:
            {
                LocomotionBase config = PickLocomotionByRarity(rarity);
                if (config == null)
                    return false;
                pickup.locomotionModule = config;
                pickup.scrapCost = config.scrapCost;
                pickup.rarity = config.rarity;
                return true;
            }
            case ModulePickup.ModuleType.Weapon:
            {
                WeaponBase config = PickWeaponByRarity(rarity);
                if (config == null)
                    return false;
                pickup.weaponModule = config;
                pickup.scrapCost = config.scrapCost;
                pickup.rarity = config.rarity;
                return true;
            }
        }

        return false;
    }

    private List<ModulePickup.ModuleType> GetAvailableRewardTypes()
    {
        List<ModulePickup.ModuleType> availableRewards = new List<ModulePickup.ModuleType>();
        if (hullRewardPool != null && hullRewardPool.Length > 0)
            availableRewards.Add(ModulePickup.ModuleType.Hull);
        if (turretRewardPool != null && turretRewardPool.Length > 0)
            availableRewards.Add(ModulePickup.ModuleType.Turret);
        if (locomotionRewardPool != null && locomotionRewardPool.Length > 0)
            availableRewards.Add(ModulePickup.ModuleType.Locomotion);
        if (weaponRewardPool != null && weaponRewardPool.Length > 0)
            availableRewards.Add(ModulePickup.ModuleType.Weapon);
        return availableRewards;
    }

    private bool HasAnyRewardForRarity(List<ModulePickup.ModuleType> availableRewards, ModuleRarity rarity)
    {
        for (int i = 0; i < availableRewards.Count; i++)
        {
            if (HasRewardForTypeAndRarity(availableRewards[i], rarity))
                return true;
        }

        return false;
    }

    private List<ModulePickup.ModuleType> GetEligibleTypesForRarity(List<ModulePickup.ModuleType> availableRewards, ModuleRarity rarity)
    {
        List<ModulePickup.ModuleType> eligible = new List<ModulePickup.ModuleType>();
        for (int i = 0; i < availableRewards.Count; i++)
        {
            if (HasRewardForTypeAndRarity(availableRewards[i], rarity))
                eligible.Add(availableRewards[i]);
        }

        return eligible;
    }

    private bool HasRewardForTypeAndRarity(ModulePickup.ModuleType type, ModuleRarity rarity)
    {
        switch (type)
        {
            case ModulePickup.ModuleType.Hull:
                return PickHullByRarity(rarity) != null;
            case ModulePickup.ModuleType.Turret:
                return PickTurretByRarity(rarity) != null;
            case ModulePickup.ModuleType.Locomotion:
                return PickLocomotionByRarity(rarity) != null;
            case ModulePickup.ModuleType.Weapon:
                return PickWeaponByRarity(rarity) != null;
            default:
                return false;
        }
    }

    private HullConfig PickHullByRarity(ModuleRarity rarity)
    {
        return PickAssetByRarity(hullRewardPool, rarity, hull => hull != null ? hull.rarity : ModuleRarity.Common);
    }

    private TurretConfig PickTurretByRarity(ModuleRarity rarity)
    {
        return PickAssetByRarity(turretRewardPool, rarity, turret => turret != null ? turret.rarity : ModuleRarity.Common);
    }

    private LocomotionBase PickLocomotionByRarity(ModuleRarity rarity)
    {
        return PickAssetByRarity(locomotionRewardPool, rarity, locomotion => locomotion != null ? locomotion.rarity : ModuleRarity.Common);
    }

    private WeaponBase PickWeaponByRarity(ModuleRarity rarity)
    {
        return PickAssetByRarity(weaponRewardPool, rarity, weapon => weapon != null ? weapon.rarity : ModuleRarity.Common);
    }

    private T PickAssetByRarity<T>(T[] pool, ModuleRarity rarity, Func<T, ModuleRarity> rarityGetter) where T : UnityEngine.Object
    {
        if (pool == null || pool.Length == 0)
            return null;

        List<T> candidates = new List<T>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == null)
                continue;
            if (rarityGetter(pool[i]) == rarity)
                candidates.Add(pool[i]);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private ModuleRarity RollRarity(RarityWeightTable weights)
    {
        float total = Mathf.Max(0f, weights.common) + Mathf.Max(0f, weights.rare) + Mathf.Max(0f, weights.epic) + Mathf.Max(0f, weights.legendary);
        if (total <= 0f)
            return ModuleRarity.Common;

        float roll = UnityEngine.Random.value * total;
        if (roll < weights.common)
            return ModuleRarity.Common;
        roll -= weights.common;
        if (roll < weights.rare)
            return ModuleRarity.Rare;
        roll -= weights.rare;
        if (roll < weights.epic)
            return ModuleRarity.Epic;
        return ModuleRarity.Legendary;
    }

    private ModuleRarity DowngradeRarity(ModuleRarity rarity)
    {
        return rarity switch
        {
            ModuleRarity.Legendary => ModuleRarity.Epic,
            ModuleRarity.Epic => ModuleRarity.Rare,
            ModuleRarity.Rare => ModuleRarity.Common,
            _ => ModuleRarity.Common
        };
    }

    private RarityWeightTable GetScaledRarityOdds(int floorIndex, bool elite, bool shop)
    {
        RarityWeightTable weights = new RarityWeightTable(
            firstFloorDropOdds.common - floorIndex * commonDecayPerFloor,
            firstFloorDropOdds.rare + floorIndex * rareGrowthPerFloor,
            firstFloorDropOdds.epic + floorIndex * epicGrowthPerFloor,
            firstFloorDropOdds.legendary + floorIndex * legendaryGrowthPerFloor);

        if (elite)
        {
            weights.common -= eliteRarityBonus;
            weights.rare += eliteRarityBonus * 0.55f;
            weights.epic += eliteRarityBonus * 0.3f;
            weights.legendary += eliteRarityBonus * 0.15f;
        }

        if (shop)
        {
            weights.common -= shopRarityBonus;
            weights.rare += shopRarityBonus * 0.55f;
            weights.epic += shopRarityBonus * 0.3f;
            weights.legendary += shopRarityBonus * 0.15f;
        }

        weights.ClampToPositive();
        return weights;
    }

    private int GetShopCost(ModuleRarity rarity)
    {
        return rarity switch
        {
            ModuleRarity.Common => 8 + currentFloor * 2,
            ModuleRarity.Rare => 14 + currentFloor * 3,
            ModuleRarity.Epic => 24 + currentFloor * 4,
            ModuleRarity.Legendary => 40 + currentFloor * 5,
            _ => 10
        };
    }

    private float GetHealthScale(int floorIndex, int waveIndex, bool elite)
    {
        float scale = 1f + floorIndex * healthScalePerFloor + waveIndex * 0.12f;
        if (elite)
            scale *= 1.55f;
        return scale;
    }

    private float GetDamageScale(int floorIndex, bool elite)
    {
        float scale = 1f + floorIndex * damageScalePerFloor;
        if (elite)
            scale *= 1.3f;
        return scale;
    }

    private float GetSpeedScale(int floorIndex, bool elite)
    {
        float scale = 1f + floorIndex * speedScalePerFloor;
        if (elite)
            scale *= 1.08f;
        return scale;
    }

    private Vector3 GetRoomCenter()
    {
        return useLayoutRewardSpawn ? (Vector3)currentRewardSpawn : player != null ? player.transform.position : Vector3.zero;
    }

    private void DestroySceneEnemies()
    {
        Enemy[] sceneEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneEnemies.Length; i++)
            Destroy(sceneEnemies[i].gameObject);
    }

    private void ClearRuntimeObjects()
    {
        for (int i = 0; i < spawnedRuntimeObjects.Count; i++)
        {
            if (spawnedRuntimeObjects[i] != null)
                Destroy(spawnedRuntimeObjects[i]);
        }

        spawnedRuntimeObjects.Clear();
        aliveEnemies.Clear();
        currentWaves.Clear();
        eventAmbushActive = false;
    }

    private void ClearPortals()
    {
        for (int i = 0; i < activePortals.Count; i++)
        {
            if (activePortals[i] != null)
                Destroy(activePortals[i]);
        }

        activePortals.Clear();
    }

    [Serializable]
    public class RarityWeightTable
    {
        public float common = 50f;
        public float rare = 40f;
        public float epic = 9f;
        public float legendary = 1f;

        public RarityWeightTable()
        {
        }

        public RarityWeightTable(float common, float rare, float epic, float legendary)
        {
            this.common = common;
            this.rare = rare;
            this.epic = epic;
            this.legendary = legendary;
        }

        public void ClampToPositive()
        {
            common = Mathf.Max(1f, common);
            rare = Mathf.Max(0f, rare);
            epic = Mathf.Max(0f, epic);
            legendary = Mathf.Max(0f, legendary);
        }
    }

    private class RuntimeRoomOption
    {
        public string displayName;
        public RoomLayoutDefinition layout;
        public int floorIndex;
        public int optionIndex;
        public RunRoomType roomType;
    }

    private class RuntimeWave
    {
        public Vector2[] spawnPositions;
        public RoomEnemySpawnEntry[] spawnEntries;
    }

    private readonly struct RoomTypeWeight
    {
        public readonly RunRoomType roomType;
        public readonly float weight;

        public RoomTypeWeight(RunRoomType roomType, float weight)
        {
            this.roomType = roomType;
            this.weight = weight;
        }
    }
}
