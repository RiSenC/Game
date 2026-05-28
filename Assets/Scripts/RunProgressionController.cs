using System;
using System.Collections.Generic;
using UnityEngine;

public class RunProgressionController : MonoBehaviour
{
    [Header("Core")]
    public PlayerModuleController player;
    public RoomDatabase roomDatabase;
    public int totalFloors = 3;
    public int choicesPerFloor = 2;

    [Header("Map Loop")]
    public bool rollingThreeNodeMap = true;
    public bool requireCombatRewardBeforePath = true;

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

    [Header("Runtime Room Dressing")]
    public Color combatFloorColor = new Color(0.12f, 0.14f, 0.16f, 1f);
    public Color serviceFloorColor = new Color(0.13f, 0.17f, 0.18f, 1f);
    public Color wallColor = new Color(0.32f, 0.36f, 0.4f, 1f);
    public Color coverColor = new Color(0.25f, 0.29f, 0.33f, 1f);

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
    private bool waitingForMandatoryReward;
    private bool roomResolved;

    private void Start()
    {
        if (player == null)
            player = UnityEngine.Object.FindFirstObjectByType<PlayerModuleController>();

        if (player != null && player.GetComponent<PlayerRunStats>() == null)
            player.gameObject.AddComponent<PlayerRunStats>();

        runStats = player != null ? player.GetComponent<PlayerRunStats>() : null;
        DestroySceneEnemies();
        BuildMap();
        LoadFirstRoom();
    }

    private void Update()
    {
        CleanupDeadEnemies();
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
        ClearPortals();
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

        int floorCount = rollingThreeNodeMap ? 1 : Mathf.Max(1, totalFloors);
        for (int floor = 0; floor < floorCount; floor++)
            generatedFloors.Add(CreateFloorOptions(floor, GetOptionCountForFloor(floor)));
    }

    private List<RuntimeRoomOption> CreateFloorOptions(int floor, int optionCount)
    {
        List<RuntimeRoomOption> options = new List<RuntimeRoomOption>();
        HashSet<RunRoomType> usedTypes = new HashSet<RunRoomType>();

        for (int i = 0; i < optionCount; i++)
        {
            RuntimeRoomOption option = CreateRoomOption(floor, i, usedTypes);
            options.Add(option);
            usedTypes.Add(option.roomType);
        }

        return options;
    }

    private int GetOptionCountForFloor(int floor)
    {
        if (rollingThreeNodeMap && floor == 0)
            return 1;

        return Mathf.Max(1, choicesPerFloor);
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
        waitingForMandatoryReward = false;
        roomResolved = false;

        if (player != null)
        {
            Vector2 spawn = option.layout != null ? option.layout.playerSpawn : Vector2.zero;
            player.transform.position = spawn;
            player.RefreshRuntimeBonuses();
        }

        SpawnRoomDressing(option);

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
        HandleAllEnemiesCleared();
    }

    private void CleanupDeadEnemies()
    {
        if (aliveEnemies.Count == 0)
            return;

        bool removedAny = false;
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy != null && !enemy.IsDeadOrDying)
                continue;

            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDeath;
                enemy.Kill();
            }

            aliveEnemies.RemoveAt(i);
            removedAny = true;
        }

        if (removedAny)
            HandleAllEnemiesCleared();
    }

    private void HandleAllEnemiesCleared()
    {
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
        if (currentRoom == null || roomResolved)
            return;

        roomResolved = true;

        switch (currentRoom.roomType)
        {
            case RunRoomType.Combat:
                SpawnMandatoryRoomReward(false);
                break;
            case RunRoomType.Elite:
                SpawnMandatoryRoomReward(true);
                break;
        }

        if (!waitingForMandatoryReward)
            OpenPathForward();
    }

    private void SpawnMandatoryRoomReward(bool elite)
    {
        ModulePickup reward = SpawnModuleReward(GetScaledRarityOdds(currentFloor, elite, false), false);
        if (reward == null || !requireCombatRewardBeforePath)
            return;

        waitingForMandatoryReward = true;
        reward.PickedUp += HandleMandatoryRewardPickedUp;
    }

    private void HandleMandatoryRewardPickedUp(ModulePickup pickup)
    {
        if (pickup != null)
            pickup.PickedUp -= HandleMandatoryRewardPickedUp;

        if (!waitingForMandatoryReward)
            return;

        waitingForMandatoryReward = false;
        OpenPathForward();
    }

    private void OpenPathForward()
    {
        int nextFloor = currentFloor + 1;

        if (rollingThreeNodeMap && nextFloor >= generatedFloors.Count && (totalFloors <= 0 || nextFloor < totalFloors))
            generatedFloors.Add(CreateFloorOptions(nextFloor, GetOptionCountForFloor(nextFloor)));

        if (nextFloor >= generatedFloors.Count)
        {
            SpawnRunCompleteMarker();
            return;
        }

        SpawnChoicePortals(nextFloor);
    }

    private void SpawnChoicePortals(int floorIndex)
    {
        ClearPortals();

        List<RuntimeRoomOption> options = generatedFloors[floorIndex];
        float spacing = Mathf.Max(portalSpacing, GetCurrentArenaSize().x * 0.32f);
        float totalWidth = (options.Count - 1) * spacing;
        Vector3 center = GetDoorCenterPosition();

        for (int i = 0; i < options.Count; i++)
        {
            Vector3 position = center + Vector3.right * (-totalWidth * 0.5f + i * spacing);
            GameObject portalObject = new GameObject($"{GetDoorDirectionName(i)} Door {floorIndex + 1}-{i + 1}");
            portalObject.transform.position = position;
            portalObject.AddComponent<BoxCollider2D>().size = Vector2.one;

            MapChoicePortal portal = portalObject.AddComponent<MapChoicePortal>();
            portal.Configure(this, floorIndex, i, FormatDoorLabel(options[i], i), GetDoorColor(i), new Vector3(1.25f, 2.1f, 1f));

            activePortals.Add(portalObject);
        }
    }

    private Vector3 GetDoorCenterPosition()
    {
        Vector2 arenaSize = GetCurrentArenaSize();
        if (arenaSize.x <= 0.1f || arenaSize.y <= 0.1f)
            return player != null ? player.transform.position + (Vector3)portalCenter : (Vector3)portalCenter;

        return new Vector3(0f, arenaSize.y * 0.5f - 1.25f, 0f);
    }

    private Vector2 GetCurrentArenaSize()
    {
        if (currentRoom != null && currentRoom.layout != null && currentRoom.layout.arenaSize.x > 0.1f && currentRoom.layout.arenaSize.y > 0.1f)
            return currentRoom.layout.arenaSize;

        return new Vector2(11f, 9f);
    }

    private string FormatDoorLabel(RuntimeRoomOption option, int optionIndex)
    {
        return $"{GetDoorDirectionName(optionIndex).ToUpperInvariant()}\n{option.roomType}\n{option.displayName}";
    }

    private string GetDoorDirectionName(int optionIndex)
    {
        if (optionIndex == 0)
            return "Left";
        if (optionIndex == 1)
            return "Right";
        return $"Path {optionIndex + 1}";
    }

    private Color GetDoorColor(int optionIndex)
    {
        if (optionIndex == 0)
            return new Color(0.2f, 0.65f, 1f, 0.88f);
        if (optionIndex == 1)
            return new Color(1f, 0.58f, 0.18f, 0.88f);
        return new Color(0.55f, 0.85f, 0.55f, 0.88f);
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
            pickup.Configure(upgradeType);
            spawnedRuntimeObjects.Add(station);
        }
    }

    private void SpawnEventNode()
    {
        GameObject eventObject = new GameObject("Random Event");
        eventObject.transform.position = GetRoomCenter() + new Vector3(0f, 0.5f, 0f);
        eventObject.AddComponent<CircleCollider2D>().radius = 0.6f;

        RandomEventPickup pickup = eventObject.AddComponent<RandomEventPickup>();
        pickup.Configure(this, RollEventOutcome(), eventScrapReward + currentFloor * 2, eventHealAmount);

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

        if (!isShop)
            pickup.scrapCost = 0;

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

    private void SpawnRoomDressing(RuntimeRoomOption option)
    {
        Vector2 arenaSize = option.layout != null ? option.layout.arenaSize : new Vector2(11f, 9f);
        if (arenaSize.x <= 0.1f || arenaSize.y <= 0.1f)
            arenaSize = new Vector2(11f, 9f);

        Color floorColor = option.roomType == RunRoomType.Combat || option.roomType == RunRoomType.Elite
            ? combatFloorColor
            : serviceFloorColor;

        GameObject floor = CreateRuntimeVisual("Room Floor", Vector3.zero, arenaSize, floorColor, -30);
        spawnedRuntimeObjects.Add(floor);

        float halfWidth = arenaSize.x * 0.5f;
        float halfHeight = arenaSize.y * 0.5f;
        float wallThickness = 0.35f;

        spawnedRuntimeObjects.Add(CreateRuntimeBlock("North Wall", new Vector2(0f, halfHeight + wallThickness * 0.5f), new Vector2(arenaSize.x + wallThickness * 2f, wallThickness), wallColor));
        spawnedRuntimeObjects.Add(CreateRuntimeBlock("South Wall", new Vector2(0f, -halfHeight - wallThickness * 0.5f), new Vector2(arenaSize.x + wallThickness * 2f, wallThickness), wallColor));
        spawnedRuntimeObjects.Add(CreateRuntimeBlock("West Wall", new Vector2(-halfWidth - wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaSize.y), wallColor));
        spawnedRuntimeObjects.Add(CreateRuntimeBlock("East Wall", new Vector2(halfWidth + wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaSize.y), wallColor));

        Vector2[] obstaclePositions = option.layout != null ? option.layout.obstaclePositions : null;
        if (obstaclePositions == null || obstaclePositions.Length == 0)
            obstaclePositions = GetFallbackObstaclePositions(option.roomType);

        Vector2 obstacleSize = option.layout != null ? option.layout.obstacleSize : new Vector2(1.15f, 1.15f);
        if (obstacleSize.x <= 0.1f || obstacleSize.y <= 0.1f)
            obstacleSize = new Vector2(1.15f, 1.15f);

        for (int i = 0; i < obstaclePositions.Length; i++)
            spawnedRuntimeObjects.Add(CreateRuntimeBlock($"Cover {i + 1}", obstaclePositions[i], obstacleSize, coverColor));

        SpawnRoomLabel(option);
    }

    private Vector2[] GetFallbackObstaclePositions(RunRoomType roomType)
    {
        return roomType switch
        {
            RunRoomType.Combat => new[] { new Vector2(-2.4f, -0.15f), new Vector2(2.4f, -0.15f) },
            RunRoomType.Elite => new[] { new Vector2(-3.1f, 0.2f), new Vector2(3.1f, 0.2f), new Vector2(0f, 2.2f) },
            RunRoomType.Shop => new[] { new Vector2(-4.2f, 1.7f), new Vector2(4.2f, 1.7f) },
            RunRoomType.Engineer => new[] { new Vector2(-4f, 1.6f), new Vector2(4f, 1.6f) },
            RunRoomType.Event => new[] { new Vector2(-3.2f, 1.8f), new Vector2(3.2f, 1.8f) },
            _ => Array.Empty<Vector2>()
        };
    }

    private GameObject CreateRuntimeVisual(string objectName, Vector2 position, Vector2 scale, Color color, int sortingOrder)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.position = position;
        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return visual;
    }

    private GameObject CreateRuntimeBlock(string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject block = CreateRuntimeVisual(objectName, position, size, color, -10);
        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        block.AddComponent<Obstacle>();
        return block;
    }

    private void SpawnRoomLabel(RuntimeRoomOption option)
    {
        GameObject labelObject = new GameObject("Room Label");
        labelObject.transform.position = new Vector3(0f, -4.9f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = $"Floor {currentFloor + 1}: {option.displayName}";
        label.characterSize = 0.18f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        spawnedRuntimeObjects.Add(labelObject);
    }

    private void SpawnRunCompleteMarker()
    {
        GameObject marker = new GameObject("Run Complete");
        marker.transform.position = GetRoomCenter() + new Vector3(0f, 2.4f, 0f);
        TextMesh label = marker.AddComponent<TextMesh>();
        label.text = "Sector clear";
        label.characterSize = 0.24f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(0.45f, 1f, 0.75f, 1f);
        spawnedRuntimeObjects.Add(marker);
    }

    private void DestroySceneEnemies()
    {
        Enemy[] sceneEnemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
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
        waitingForMandatoryReward = false;
        ClearLoosePickups();
    }

    private void ClearLoosePickups()
    {
        DestroyLooseObjects(UnityEngine.Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None));
        DestroyLooseObjects(UnityEngine.Object.FindObjectsByType<ScrapPickup>(FindObjectsSortMode.None));
        DestroyLooseObjects(UnityEngine.Object.FindObjectsByType<ModulePickup>(FindObjectsSortMode.None));
        DestroyLooseObjects(UnityEngine.Object.FindObjectsByType<EngineerUpgradePickup>(FindObjectsSortMode.None));
        DestroyLooseObjects(UnityEngine.Object.FindObjectsByType<RandomEventPickup>(FindObjectsSortMode.None));
    }

    private void DestroyLooseObjects<T>(T[] components) where T : Component
    {
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                Destroy(components[i].gameObject);
        }
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
