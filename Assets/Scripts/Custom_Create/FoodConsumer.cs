using UnityEngine;


public class FoodConsumer : MonoBehaviour
{
    private int myTier = 1;
    private int huntTierDifference = 1;
    private float huntDetectionRadius = 3f;
    private float consumeDistance = 0.8f;
    private float baseSpeed = 1.5f;
    private float currentSpeed = 1.5f;
    private float huntSuccessChance = 0.6f;

    // Stun/slow state — set by CamouflageAbility.DebuffPredator()
    private bool isStunned = false;
    private bool isSlowed = false;

    private float checkTimer = 0f;
    private const float huntCheckInterval = 0.3f;
    private GameObject currentTarget = null;

    [HideInInspector] public string foodTargetUniqueID = "";

    public void Initialize(int tier, int tierDifference, float detectionRadius,
                           float consumeDist, float speed, float successChance = 0.6f)
    {
        myTier = tier;
        huntTierDifference = tierDifference;
        huntDetectionRadius = detectionRadius;
        consumeDistance = consumeDist;
        baseSpeed = speed;
        currentSpeed = speed;
        huntSuccessChance = successChance;
    }

  
    // Stun / Slow API — called by CamouflageAbility

    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
        if (stunned)
        {
            currentTarget = null; // lose the target
            Debug.Log($"[FoodConsumer] {gameObject.name} is STUNNED — lost target");
        }
    }


    public void ApplySlow(float multiplier)
    {
        isSlowed = true;
        currentSpeed = baseSpeed * multiplier;
        Debug.Log($"[FoodConsumer] {gameObject.name} SLOWED to {currentSpeed:F1} " +
                  $"(was {baseSpeed:F1})");
    }

    public void RemoveSlow()
    {
        isSlowed = false;
        currentSpeed = baseSpeed;
        Debug.Log($"[FoodConsumer] {gameObject.name} speed restored to {currentSpeed:F1}");
    }

    public bool IsStunned => isStunned;

  
    // Runtime

    void Update()
    {
        if (isStunned) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= huntCheckInterval)
        {
            checkTimer = 0f;
            RefreshTarget();
        }

        if (currentTarget != null)
            ChaseTarget();
    }

    void RefreshTarget()
    {
        if (currentTarget != null && currentTarget.gameObject == null)
            currentTarget = null;

        // Don't hunt a camouflaged actor — they've gone invisible
        if (currentTarget != null)
        {
            CamouflageAbility cam = currentTarget.GetComponent<CamouflageAbility>();
            if (cam != null && cam.IsActive)
            {
                Debug.Log($"[FoodConsumer] Lost {currentTarget.name} — camouflaged");
                currentTarget = null;
            }
        }

        if (currentTarget != null) return; // keep chasing current target

        GameObject nearest = null;
        float nearestDist = float.MaxValue;

        Collider[] nearby = Physics.OverlapSphere(transform.position, huntDetectionRadius);
        foreach (Collider col in nearby)
        {
            if (!col.CompareTag("Actor")) continue;
            if (col.gameObject == gameObject) continue;

            // Skip camouflaged prey
            CamouflageAbility cam = col.GetComponent<CamouflageAbility>();
            if (cam != null && cam.IsActive) continue;

            ActorTierIdentity tierID = col.GetComponent<ActorTierIdentity>();
            if (tierID == null) continue;
            if (myTier - tierID.Tier < huntTierDifference) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.gameObject;
            }
        }

        currentTarget = nearest;

        // Legacy fallback
        if (currentTarget == null && !string.IsNullOrEmpty(foodTargetUniqueID))
        {
            GameObject[] allActors = GameObject.FindGameObjectsWithTag("Actor");
            foreach (var go in allActors)
            {
                ActorIdentity id = go.GetComponent<ActorIdentity>();
                if (id != null && id.uniqueId == foodTargetUniqueID)
                {
                    currentTarget = go;
                    break;
                }
            }
        }
    }

    void ChaseTarget()
    {
        if (currentTarget == null) return;

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (dist <= consumeDistance)
        {
            TryConsume();
            return;
        }

        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        Vector3 nextPosition = transform.position + direction * currentSpeed * Time.deltaTime;
        transform.position = SandboxBounds.Clamp(nextPosition);

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), 8f * Time.deltaTime);
    }

    void TryConsume()
    {
        if (currentTarget == null) return;

        // Chance-based kill — not every contact guarantees a kill
        if (Random.value > huntSuccessChance)
        {
            Debug.Log($"[FoodConsumer] {gameObject.name} missed! " +
                      $"(chance={huntSuccessChance:P0})");
            currentTarget = null; // lose target, will re-acquire next scan
            return;
        }

        Debug.Log($"[FoodConsumer] {gameObject.name} (tier {myTier}) consumed {currentTarget.name}");

        ActorIdentity identity = currentTarget.GetComponent<ActorIdentity>();
        if (identity != null)
            EnvironmentDataCache.RemoveActorById(identity.uniqueId);

        Destroy(currentTarget);
        currentTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, huntDetectionRadius);
    }
}