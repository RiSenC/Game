using UnityEngine;

/// <summary>
/// Enemy with lightweight tactical movement and encounter scaling.
/// </summary>
public class Enemy : MonoBehaviour
{
    public enum EnemyRole
    {
        Charger,
        Tank,
        Shooter
    }

    [Header("Enemy Config")]
    public EnemyRole role = EnemyRole.Charger;
    public float maxHealth = 30f;
    public float speed = 4f;
    public float rotationSpeed = 120f;
    public float stopDistance = 2f;

    [Header("Ranged Attack")]
    public float attackRange = 7.5f;
    public float preferredDistance = 5f;
    public float fireCooldown = 1.25f;
    public float attackDamage = 8f;
    public Color shotTracerColor = new Color(1f, 0.35f, 0.25f, 0.9f);

    [Header("Tactics")]
    public float strafeStrength = 1.2f;
    public float weaveStrength = 0.9f;
    public float decisionInterval = 1f;
    public float avoidanceDistance = 1.1f;
    public LayerMask avoidanceMask = Physics2D.DefaultRaycastLayers;

    [Header("Drops")]
    [Range(0f, 1f)] public float healthDropChance = 0.2f;
    [Range(0f, 1f)] public float scrapDropChance = 0.7f;
    public int scrapAmount = 1;

    [Header("Visual")]
    public Sprite enemySprite;
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    public Color eliteTint = new Color(1f, 0.75f, 0.25f, 1f);

    private HealthSystem healthSystem;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private Color originalColor;
    private float flashTimer;
    private bool isFlashing;
    private float lastShotTime = float.NegativeInfinity;

    private float baseMaxHealth;
    private float baseSpeed;
    private float baseAttackDamage;
    private float baseFireCooldown;
    private int baseScrapAmount;
    private bool baseConfigured;

    private int orbitDirection = 1;
    private float tacticSeed;
    private Vector2 tacticalOffset;
    private float nextDecisionTime;
    private Vector2 spawnAnchor;
    private bool isElite;
    private bool deathHandled;

    public System.Action<Enemy> Died;
    public bool IsDeadOrDying => deathHandled || healthSystem != null && (healthSystem.IsDead || healthSystem.currentHP <= 0f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();

        healthSystem.maxHP = maxHealth;
        healthSystem.currentHP = maxHealth;
        healthSystem.armorFlat = 0f;
        healthSystem.armorPercent = 0f;
        healthSystem.minDamagePercent = 1f;
        healthSystem.OnHealthChanged += HandleDamage;
        healthSystem.OnDeath += Die;

        CaptureBaseStats();
        orbitDirection = Random.value < 0.5f ? -1 : 1;
        tacticSeed = Random.Range(0f, Mathf.PI * 2f);
        spawnAnchor = transform.position;
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (enemySprite != null)
                spriteRenderer.sprite = enemySprite;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        RecalculateTacticalOffset();
    }

    private void FixedUpdate()
    {
        if (IsDeadOrDying)
        {
            Die();
            return;
        }

        if (playerTransform == null || healthSystem == null)
        {
            TryResolvePlayerReference();
            return;
        }

        Vector2 toPlayer = (Vector2)(playerTransform.position - transform.position);
        Vector2 directionToPlayer = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.up;
        float distanceToPlayer = toPlayer.magnitude;

        if (Time.time >= nextDecisionTime)
            RecalculateTacticalOffset();

        Vector2 desiredPoint = GetDesiredPoint(directionToPlayer, distanceToPlayer);
        Vector2 desiredVelocity = GetDesiredVelocity(desiredPoint, directionToPlayer, distanceToPlayer);

        RotateTowards(directionToPlayer);
        rb.linearVelocity = desiredVelocity;

        if (role == EnemyRole.Shooter && distanceToPlayer <= attackRange)
            TryShoot(directionToPlayer, distanceToPlayer);
    }

    private void Update()
    {
        if (!isFlashing)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer > 0f)
            return;

        isFlashing = false;
        if (spriteRenderer != null)
            spriteRenderer.color = isElite ? Color.Lerp(originalColor, eliteTint, 0.55f) : originalColor;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleDamage;
            healthSystem.OnDeath -= Die;
        }
    }

    public void ConfigureEncounter(float healthScale, float speedScale, float damageScale, bool elite, int tacticalSeedValue)
    {
        CaptureBaseStats();

        isElite = elite;
        tacticSeed = tacticalSeedValue * 0.73f;
        orbitDirection = tacticalSeedValue % 2 == 0 ? 1 : -1;

        maxHealth = baseMaxHealth * healthScale;
        speed = baseSpeed * speedScale;
        attackDamage = baseAttackDamage * damageScale;
        fireCooldown = Mathf.Max(0.25f, baseFireCooldown / Mathf.Lerp(1f, 1.2f, elite ? 1f : 0f));
        scrapAmount = Mathf.RoundToInt(baseScrapAmount * (elite ? 2.5f : 1f));
        healthDropChance = Mathf.Clamp01(healthDropChance + (elite ? 0.15f : 0f));
        scrapDropChance = Mathf.Clamp01(scrapDropChance + (elite ? 0.2f : 0f));

        if (healthSystem != null)
        {
            healthSystem.maxHP = maxHealth;
            healthSystem.currentHP = maxHealth;
            healthSystem.armorFlat = 0f;
            healthSystem.armorPercent = 0f;
            healthSystem.minDamagePercent = 1f;
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = elite ? Color.Lerp(originalColor, eliteTint, 0.55f) : originalColor;
        }
    }

    private void CaptureBaseStats()
    {
        if (baseConfigured)
            return;

        baseConfigured = true;
        baseMaxHealth = maxHealth;
        baseSpeed = speed;
        baseAttackDamage = attackDamage;
        baseFireCooldown = fireCooldown;
        baseScrapAmount = scrapAmount;
    }

    private void TryResolvePlayerReference()
    {
        if (playerTransform != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void RotateTowards(Vector2 directionToPlayer)
    {
        float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);
        float newAngle = currentAngle + Mathf.Clamp(angleDiff, -rotationSpeed * Time.fixedDeltaTime, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);
    }

    private void RecalculateTacticalOffset()
    {
        nextDecisionTime = Time.time + decisionInterval + Random.Range(-0.15f, 0.2f);

        Vector2 lateral = new Vector2(-Mathf.Sin(tacticSeed + Time.time), Mathf.Cos(tacticSeed + Time.time));
        Vector2 anchorDrift = ((Vector2)spawnAnchor - (Vector2)transform.position) * 0.2f;

        switch (role)
        {
            case EnemyRole.Charger:
                tacticalOffset = lateral * weaveStrength * orbitDirection + anchorDrift;
                break;
            case EnemyRole.Tank:
                tacticalOffset = lateral * (weaveStrength * 0.45f) + anchorDrift * 1.35f;
                break;
            case EnemyRole.Shooter:
                tacticalOffset = lateral * strafeStrength * orbitDirection + anchorDrift * 0.5f;
                break;
        }
    }

    private Vector2 GetDesiredPoint(Vector2 directionToPlayer, float distanceToPlayer)
    {
        Vector2 playerPos = playerTransform.position;
        Vector2 desiredPoint = playerPos;

        switch (role)
        {
            case EnemyRole.Charger:
                desiredPoint -= directionToPlayer * Mathf.Max(0.4f, stopDistance * 0.75f);
                desiredPoint += tacticalOffset;
                break;
            case EnemyRole.Tank:
                desiredPoint -= directionToPlayer * stopDistance;
                desiredPoint += tacticalOffset * 0.75f;
                break;
            case EnemyRole.Shooter:
                float desiredRange = preferredDistance + Mathf.Sin(Time.time * 0.7f + tacticSeed) * 0.5f;
                desiredPoint -= directionToPlayer * desiredRange;
                desiredPoint += tacticalOffset;
                break;
        }

        if (distanceToPlayer < 1f && role != EnemyRole.Charger)
            desiredPoint -= directionToPlayer * 1.5f;

        return desiredPoint;
    }

    private Vector2 GetDesiredVelocity(Vector2 desiredPoint, Vector2 directionToPlayer, float distanceToPlayer)
    {
        Vector2 desiredDirection = desiredPoint - (Vector2)transform.position;
        float distanceToPoint = desiredDirection.magnitude;
        if (distanceToPoint > 0.01f)
            desiredDirection /= distanceToPoint;
        else
            desiredDirection = Vector2.zero;

        Vector2 avoidance = GetAvoidanceDirection(desiredDirection);
        Vector2 finalDirection = (desiredDirection + avoidance).normalized;
        if (finalDirection == Vector2.zero)
            finalDirection = desiredDirection;

        float moveSpeed = speed;
        switch (role)
        {
            case EnemyRole.Charger:
                if (distanceToPlayer < stopDistance * 0.8f)
                    moveSpeed *= 0.65f;
                break;
            case EnemyRole.Tank:
                moveSpeed *= distanceToPlayer > stopDistance ? 0.82f : 0.45f;
                break;
            case EnemyRole.Shooter:
                if (distanceToPlayer <= attackRange && Mathf.Abs(distanceToPlayer - preferredDistance) < 1f)
                    moveSpeed *= 0.9f;
                else if (distanceToPlayer < preferredDistance * 0.8f)
                    moveSpeed *= 1.1f;
                break;
        }

        return finalDirection * moveSpeed;
    }

    private Vector2 GetAvoidanceDirection(Vector2 desiredDirection)
    {
        if (desiredDirection == Vector2.zero)
            return Vector2.zero;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDirection, avoidanceDistance, avoidanceMask);
        if (!hit || hit.collider == null || hit.collider.transform.root == transform.root || hit.collider.CompareTag("Player"))
            return Vector2.zero;

        Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x) * orbitDirection;
        return perpendicular * 0.8f;
    }

    private void HandleDamage(float current, float max)
    {
        if (deathHandled)
            return;

        if (current <= 0f)
        {
            Die();
            return;
        }

        if (spriteRenderer == null)
            return;

        spriteRenderer.color = damageFlashColor;
        isFlashing = true;
        flashTimer = damageFlashDuration;
    }

    public void Kill()
    {
        Die();
    }

    private void Die()
    {
        if (deathHandled)
            return;

        deathHandled = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        SpawnLoot();
        Died?.Invoke(this);
        DisableBodyImmediately();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void DisableBodyImmediately()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        enabled = false;
    }

    private void TryShoot(Vector2 directionToPlayer, float distanceToPlayer)
    {
        if (IsDeadOrDying || !isActiveAndEnabled)
            return;

        if (Time.time < lastShotTime + fireCooldown)
            return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToPlayer, distanceToPlayer);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null)
                continue;
            if (hit.collider.transform.root == transform.root)
                continue;
            if (hit.collider.isTrigger)
                continue;

            SpawnTracer(transform.position, hit.point);
            HealthSystem health = hit.collider.GetComponentInParent<HealthSystem>();
            if (health != null)
                health.TakeDamage(attackDamage, -directionToPlayer);

            lastShotTime = Time.time;
            return;
        }

        SpawnTracer(transform.position, (Vector2)transform.position + directionToPlayer * distanceToPlayer);
        lastShotTime = Time.time;
    }

    private void SpawnLoot()
    {
        if (Random.value <= healthDropChance)
        {
            GameObject healthPickup = new GameObject("Health Pickup");
            healthPickup.transform.position = transform.position + new Vector3(-0.25f, 0f, 0f);
            HealthPickup pickup = healthPickup.AddComponent<HealthPickup>();
            pickup.healAmount = isElite ? 30f : 20f;
        }

        if (Random.value <= scrapDropChance)
        {
            GameObject scrapPickup = new GameObject("Scrap Pickup");
            scrapPickup.transform.position = transform.position + new Vector3(0.25f, 0f, 0f);
            ScrapPickup pickup = scrapPickup.AddComponent<ScrapPickup>();
            pickup.scrapAmount = scrapAmount;
        }
    }

    private void SpawnTracer(Vector2 start, Vector2 end)
    {
        GameObject tracer = new GameObject("Enemy Tracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.04f;
        line.endWidth = 0.04f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = shotTracerColor;
        line.endColor = shotTracerColor;
        line.sortingOrder = 9;
        Destroy(tracer, 0.05f);
    }
}
