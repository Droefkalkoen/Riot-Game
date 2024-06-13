using System.Collections.Generic;
using UnityEngine;

public class CrowdMember : MonoBehaviour
{
    private GameManager gameManager;

    [Header("General Settings")]
    public bool panick = false;
    public bool isBoid = true;
    [Range(0, 50)]
    public float particleWeight = 20f;
    public LayerMask peopleLayer;
    public LayerMask myLayer;
    public LayerMask weaponLayer;
    [Range(0, 50)]
    public int updateForcesEveryNthFrame = 10;

    [Header("Movement Settings")]
    public float speed = 4f;
    public float rotationSpeed = 5f;

    [Header("Steering Weights")]
    [Range(0, 10)]
    public float separationWeight = 3f;
    [Range(0, 10)]
    public float cohesionWeight = 5f;
    [Range(0, 10)]
    public float alignmentWeight = 5f;
    [Range(0, 10)]
    public float seekWeight = 1f;

    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public float separationDistanceThreshold = 3f;

    [Header("Fear Settings")]
    public List<LayerMask> enemyFactionLayers;

    public float fearThreshold = 2f;
    public float fearIncreaseRate = 0.5f;
    public float fearDecreaseRate = 0.2f;
    public float fearRadius = 6f;
    public float fearWeight;

    public Vector3 damageDirection = new Vector3(0, 0, 0);


    [Header("Seek Settings")]
    public Transform seekTarget;
    public int targetUpdateEveryNthFrame = 833;

    [Header("Confrontation Settings")]
    public bool isConfrontational = false;

    [Header("Attack Settings")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float lastAttackTime;


    [Header("Target objects")]
    public List<GameObject> targetObjects;
    public List<GameObject> visitedObjects;
    public List<GameObject> leaveObjects;

    //Private variables
    private Vector3 separation;
    private Vector3 cohesion;
    private Vector3 alignment;
    private Vector3 flee;

    public float boredomFactor;

    private int minRangeEnemy;
    private int maxRangeEnemy;

    private Vector3 desiredVelocity;

    private float healthLostSinceLastUpdate;

    private Rigidbody rb;

    [Header("Current statistics")]
    [Range(0, 100)]
    public float health;
    [Range(0, 100)]
    public float armor;
    [Range(0, 100)]
    public float gasmask;
    [Range(0, 100)]
    public float gas;
    [Range(0, 100)]
    public float bravery;
    [Range(0, 100)]
    public float currentFear;
    [Range(0, 100)]
    public float currentAggression;

    private Vector3 particleForces;
    private float forceMagnitude;

    private int frameCounter = 0;

    private void Start()
    {
        CapsuleCollider cCollider = GetComponent<CapsuleCollider>();
        cCollider.radius = cCollider.radius * Random.Range(0.6f, 1.4f);
        GameManager gameManager = FindObjectOfType<GameManager>();
        minRangeEnemy = Random.Range(-10, 1);
        maxRangeEnemy = Random.Range(-50, -100);
        healthLostSinceLastUpdate = 0;

        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Initialize a random frame counter offset to stagger updates
        frameCounter = Random.Range(0, 9999);

        // Randomize various weights and thresholds to create diversity among crowd members
        cohesionWeight = cohesionWeight * Random.Range(0.6f, 1.4f);
        alignmentWeight = alignmentWeight * Random.Range(0.6f, 1.4f);
        separationWeight = separationWeight * Random.Range(0.6f, 1.4f);
        separationDistanceThreshold = separationDistanceThreshold * Random.Range(0.6f, 1.4f);
        speed = speed * Random.Range(0.6f, 1.4f);
        rotationSpeed = rotationSpeed * Random.Range(0.6f, 1.4f);
        detectionRadius = detectionRadius * Random.Range(0.6f, 1.4f);
        seekWeight = seekWeight * Random.Range(0.6f, 1.4f);

        // Find all the game objects with the crowdTarget script attached
        targetObjects = new List<GameObject>();
        crowdTarget[] foundcrowdTargets = GameObject.FindObjectsOfType<crowdTarget>();
        foreach (crowdTarget targetObject in foundcrowdTargets)
        {
            targetObjects.Add(targetObject.gameObject);
        }
        if (targetObjects.Count < 1 || targetObjects == null)
        {
            Debug.LogWarning("NPC: No crowd targets found!");
        }

        // Find all the game objects with the leaveTarget script attached
        leaveObjects = new List<GameObject>();
        leaveTarget[] foundleaveTargets = GameObject.FindObjectsOfType<leaveTarget>();
        foreach (leaveTarget targetObject in foundleaveTargets)
        {
            leaveObjects.Add(targetObject.gameObject);
        }
        if (leaveObjects.Count < 1 || leaveObjects == null)
        {
            Debug.LogWarning("NPC: No leave targets found!");
        }

        // Assign initial target
        AssignNewTarget();

        //// Calculate initial values
        //CalculateCohesionSeparationAlignment();
        //// Calculate initial flee force
        //Vector3 flee = CalculateFlee();
    }


    private void FixedUpdate()
    {
        frameCounter++;

        // Update forces every Nth frame
        if (frameCounter % updateForcesEveryNthFrame == 0)
        {
            updateForcesEveryNthFrame = Random.Range(4, 54);

            // Handle Boid-like behavior
            if (isBoid == true)
            {
                
                UpdateFear();
                CalculateCohesionSeparationAlignment();

                desiredVelocity = Vector3.zero;

                // Determine the desired velocity based on the current state of the crowd member
                if (currentFear >= fearThreshold && !panick)
                {
                    updateForcesEveryNthFrame = Random.Range(5, 300);
                    if (Random.Range(35.0f, 100.0f) < currentFear)
                    {
                        float fearShare = fearThreshold / 50f * currentFear;

                        // Find colliders in radius 
                        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, fearRadius, myLayer);


                        // Iterate
                        foreach (Collider collider in nearbyColliders)
                        {
                            // Ignore self
                            if (collider.gameObject != gameObject && fearShare > 0)
                            {
                                fearShare -= Random.Range(collider.GetComponent<CrowdMember>().currentFear, 100f);
                                collider.GetComponent<CrowdMember>().currentFear -= Random.Range(collider.GetComponent<CrowdMember>().currentFear, 100f);
                            }
                        }
                    }
                    panick = true;
                    Vector3 flee = CalculateFlee();
                    desiredVelocity = (flee * 2f).normalized * speed * Random.Range(1.0f, 2.0f); // Move away and flee.
                }
                else if (currentFear >= fearThreshold)
                {
                    updateForcesEveryNthFrame = Random.Range(5, 60);
                    Vector3 flee = CalculateFlee();
                    desiredVelocity = (flee * 2f).normalized * speed * Random.Range(1.0f, 2.0f); // Move away and flee.
                }
                else if (panick && (currentFear < 0.8f * fearThreshold))
                {
                    panick = false;
                    if (Random.Range(0.0f, 85.0f) > health)
                    {
                        AssignLeaveTarget();
                    }

                }
                else
                {
                    float tempSeekWeight = seekWeight;
                    if (seekTarget != null)
                    {
                        tempSeekWeight = seekWeight * seekTarget.GetComponent<crowdTarget>().seekMultiplier;
                    }
                    Vector3 seek = CalculateSeek();
                    desiredVelocity = ((separation * separationWeight * (1 + (currentFear / fearThreshold * fearWeight))) +
                                      (cohesion * cohesionWeight * ((fearThreshold - currentFear) / fearThreshold * fearWeight)) +
                                      (alignment * alignmentWeight) +
                                      (seek * tempSeekWeight * ((fearThreshold - currentFear) / fearThreshold * fearWeight))).normalized * speed * (1 + (currentFear / fearThreshold * fearWeight));
                }

                // steering force
                Vector3 steering = desiredVelocity - rb.velocity;
                steering.y = 0;
                rb.AddForce(steering);
                rb.velocity = Vector3.ClampMagnitude(rb.velocity, speed / 4 * (1 + (currentFear / fearThreshold * fearWeight)));
                if (panick)
                {
                    rb.velocity = rb.velocity * Random.Range(1.0f, 2.0f);
                }
            }
            // non-Boid behavior
            else
            {
                // Update fear lvl
                UpdateFear();
                desiredVelocity = Vector3.zero;

                Vector3 seek = CalculateSeek();
                particleForces = CalculateForces();

                // Determine velocity
                if (currentFear >= fearThreshold && !panick)
                {
                    if (Random.Range(35.0f, 200.0f) < currentFear)
                    {
                        float fearShare = fearThreshold / 50f * currentFear;

                        // Find colliders in radius
                        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, fearRadius, myLayer);


                        // Iterate
                        foreach (Collider collider in nearbyColliders)
                        {
                            // Ignore self
                            if (collider.gameObject != gameObject && fearShare > 0)
                            {
                                fearShare -= Random.Range(collider.GetComponent<CrowdMember>().currentFear, 100f);
                                collider.GetComponent<CrowdMember>().currentFear += Random.Range(0f, collider.GetComponent<CrowdMember>().currentFear);
                            }
                        }
                        Mathf.Clamp(currentFear, 0f, 100f);
                    }
                    panick = true;
                    Vector3 flee = CalculateFlee();
                    desiredVelocity = (flee * 2f).normalized * speed * Random.Range(1.0f, 2.0f); // Move away and flee.
                }
                else if (currentFear >= fearThreshold)
                {
                    Vector3 flee = CalculateFlee();
                    desiredVelocity = (flee * 2f).normalized * speed * Random.Range(1.0f, 2.0f); // Move away and flee.
                }
                else if (panick && (currentFear < 0.8f * fearThreshold))
                {
                    panick = false;

                }
                else
                {
                    float tempSeekWeight = seekWeight;
                    if (seekTarget != null)
                    {
                        tempSeekWeight = seekWeight * seekTarget.GetComponent<crowdTarget>().seekMultiplier;
                    }
                    Vector3 preForceCalculation = (particleForces * particleWeight + (seek * tempSeekWeight * ((fearThreshold - currentFear) / fearThreshold * fearWeight)));
                    preForceCalculation.y = 0;
                    rb.AddForce(preForceCalculation);
                    rb.velocity = Vector3.ClampMagnitude(rb.velocity, speed / 4 * (1 + (currentFear / fearThreshold * fearWeight)));
                }
            }
        }
        // Handle velocity updates when not updating forces for Boid behavior
        else if (isBoid == true)
        {
            if (panick == true)
            {
                desiredVelocity = (flee * 2f).normalized * speed * Random.Range(1.0f, 2.0f); // Move away and flee.
            }
            else
            {
                float tempSeekWeight = seekWeight;
                if (seekTarget != null)
                {
                    tempSeekWeight = seekWeight * seekTarget.GetComponent<crowdTarget>().seekMultiplier;
                }
                Vector3 seek = CalculateSeek();
                desiredVelocity = ((separation * separationWeight * (1 + (currentFear / fearThreshold * fearWeight))) +
                                          (cohesion * cohesionWeight * ((fearThreshold - currentFear) / fearThreshold * fearWeight)) +
                                          (alignment * alignmentWeight * (1 + (currentFear / fearThreshold * fearWeight))) +
                                          (seek * tempSeekWeight * ((fearThreshold - currentFear) / fearThreshold * fearWeight))).normalized * speed * (1 + (currentFear / fearThreshold * fearWeight));
            }
            
        }
        // Handle velocity updates when not updating forces for non-Boid behavior
        else
        {
            float tempSeekWeight = seekWeight;
            if (seekTarget != null)
            {
                tempSeekWeight = seekWeight * seekTarget.GetComponent<crowdTarget>().seekMultiplier;
            }
            Vector3 seek = CalculateSeek();
            Vector3 preForceCalculation = (particleForces * particleWeight + ((seek * tempSeekWeight) * ((fearThreshold - currentFear) / fearThreshold * fearWeight)));
            preForceCalculation.y = 0;
            rb.AddForce(preForceCalculation / 10);
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, speed / 4 * (1 + (currentFear / fearThreshold * fearWeight)));
        }
        // Rotate the crowd member to face the direction of movement
        if (rb.velocity.magnitude > 0.05)
        {
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            if (flatVelocity.magnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatVelocity);
                Quaternion currentRotation = transform.rotation;

                // Project rotation on plane
                Vector3 projectedForward = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, Vector3.up).normalized;
                Quaternion projectedRotation = Quaternion.LookRotation(projectedForward);

                // Slerp
                Quaternion newRotation = Quaternion.Slerp(currentRotation, projectedRotation, rotationSpeed * Time.fixedDeltaTime * (1 + (currentFear / fearThreshold * fearWeight)));

                // Apply rot
                rb.MoveRotation(newRotation);
            }
        }



        // Update the target every Nth frame
        if (frameCounter % targetUpdateEveryNthFrame == 0)
        {
            AssignNewTarget();
        }

        damageDirection = new Vector3(0, 0, 0);

    }


    private Vector3 CalculateForces()
    {
        Vector3 forces = Vector3.zero;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, peopleLayer);

        foreach (Collider collider in nearbyColliders)
        {

            if (collider.gameObject != gameObject)
            {

                float distance = Vector3.Distance(transform.position, collider.transform.position);

                forceMagnitude = 0.26f * Mathf.Log(distance) + 0.1536f;
                if (collider.gameObject.layer != this.myLayer)
                {
                    forceMagnitude += 0.5f + ((currentFear / fearThreshold * fearWeight));
                }
                    
                // direction of the force (away from the nearby collider)
                Vector3 direction = (transform.position - collider.transform.position).normalized;

                // add together
                forces += direction * forceMagnitude;
            }
        }


        return forces;
    }

    void Update()
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);

        if ((Time.time - lastAttackTime < attackCooldown) && randomValue <= currentAggression)
        {
            Attack();
        }
    }


    private void UpdateFear()
    {
        float fearFactor = 0;
        //foreach (LayerMask enemyFactionLayer in enemyFactionLayers)
        //{
        //    Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, fearRadius, peopleLayer);

        //    foreach (Collider enemy in nearbyEnemies)
        //    {
        //        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        //        float distanceFactor = Mathf.Clamp01(1 - distance / fearRadius);
        //        fearFactor += distanceFactor;
        //    }
        //}

        if (fearFactor > 0)
        {
            currentFear += fearIncreaseRate * Time.fixedDeltaTime * fearFactor;
        }
        else
        {
            currentFear -= fearDecreaseRate * Time.fixedDeltaTime;
        }

        currentFear = Mathf.Clamp(currentFear, 0.01f, 100f);
    }



    private void CalculateCohesionSeparationAlignment()
    {
        Vector3 separation = Vector3.zero;
        Vector3 center = Vector3.zero;
        Vector3 averageVelocity = Vector3.zero;
        int nearbyMembers = 0;
        int count = 0;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius, peopleLayer);

        foreach (Collider collider in nearbyColliders)
        {
            if (collider.gameObject != gameObject)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                Vector3 direction = transform.position - collider.transform.position;

                if (collider.gameObject.layer != this.myLayer)
                {
                    direction /= distance * distance;
                    direction.Normalize();
                    direction = direction * 3;
                }
                else
                {
                    direction /= distance * distance;
                    direction.Normalize();
                }

                if (distance < (0.1 * detectionRadius))
                {
                    distance = 3 * distance;
                }
                if (distance < (0.2 * detectionRadius))
                {
                    distance = 3 * distance;
                }

                separation += direction;
                count++;
                if (collider.gameObject.layer == this.myLayer)
                {
                    averageVelocity += collider.attachedRigidbody.velocity;
                    center += collider.transform.position;
                    nearbyMembers++;
                }

            }
        }

        if (nearbyMembers > 0)
        {
            averageVelocity /= nearbyMembers;
            alignment = averageVelocity.normalized;
            center /= nearbyMembers;
            cohesion = (center - transform.position).normalized;
        }
        else
        {
            cohesion = Vector3.zero;
            alignment = Vector3.zero;
        }

        if (count > 0)
        {
            //          separation /= count;
        }
        else
        {
            separation = Vector3.zero;
        }

        this.separation = separation;


    }
    private Vector3 CalculateFlee()
    {

        Vector3 fleeDirection = Vector3.zero;
        if (damageDirection != new Vector3(0, 0, 0))
        {
            fleeDirection = (rb.position - damageDirection).normalized;
            return fleeDirection;
        }
        int numberOfEnemies = 0;

            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, fearRadius, (peopleLayer - myLayer));

            foreach (Collider enemy in nearbyEnemies)
            {
                Vector3 enemyToSelf = transform.position - enemy.transform.position;
                float distance = enemyToSelf.magnitude;
                float distanceFactor = Mathf.Clamp01(1 - distance / fearRadius);

                fleeDirection += enemyToSelf.normalized * distanceFactor;
                numberOfEnemies++;
            }
        if (numberOfEnemies > 0)
        {
            fleeDirection /= numberOfEnemies;
        }
        else
        {
            fleeDirection = Random.Range(-1f, 1f) * Vector3.forward + Random.Range(-1f, 1f) * Vector3.right;
        }

        return fleeDirection;
    }

    private void AssignNewTarget()
    {
        frameCounter = Random.Range(0, 9999);
        if (targetObjects.Count > 0)
        {
            float totalImportance = 0f;

            // Calculate the total importance of all target objects
            foreach (GameObject targetObject in targetObjects)
            {
                totalImportance += targetObject.GetComponent<crowdTarget>().targetImportance;
            }
            totalImportance += boredomFactor;

            // Generate a random number
            float randomValue = Random.Range(0, totalImportance);

            // Select a random targt
            foreach (GameObject targetObject in targetObjects)
            {
                randomValue -= targetObject.GetComponent<crowdTarget>().targetImportance;

                if (randomValue <= 0)
                {
                    seekTarget = targetObject.transform;
                    break;
                }
            }
        }
    }

    private void AssignLeaveTarget()
    {
        if (leaveObjects.Count > 0)
        {
            int randomIndex = Mathf.RoundToInt(Random.Range(0.0f, leaveObjects.Count - 1f));
            seekTarget = leaveObjects[randomIndex].transform;
            targetObjects.Clear();
        }
    }

    public float HealthLostSinceLastUpdate()
    {
        return healthLostSinceLastUpdate;
    }

    public void ResetHealthLostCounter()
    {
        healthLostSinceLastUpdate = 0;
    }

    public void UpdateHealth(float damage)
    {
        float previousHealth = health;
        health -= damage;
        healthLostSinceLastUpdate += previousHealth - health;
    }

    private Vector3 CalculateSeek()
    {
        if (seekTarget == null) return Vector3.zero;

        Vector3 desiredVelocity = (seekTarget.position - transform.position).normalized * speed * (1 + (currentFear / fearThreshold * fearWeight));
        Vector3 seekForce = desiredVelocity - rb.velocity;

        return seekForce;
    }

    public float CalculateEffectiveBravery(LayerMask attackingLayer)
    {
        int ownMembersCount = gameManager.GetFactionMemberCount(gameObject.layer);
        int attackingMembersCount = gameManager.GetFactionMemberCount(attackingLayer);
        float ownFactionHealthLost = gameManager.GetFactionHealthLost(gameObject.layer);
        float attackingFactionHealthLost = gameManager.GetFactionHealthLost(attackingLayer);

        float memberRatio = (float)ownMembersCount / attackingMembersCount;
        float healthLostRatio = ownFactionHealthLost / attackingFactionHealthLost;

        return bravery * memberRatio * healthLostRatio;
    }

    public void TakeDamage(float damage, LayerMask attackingLayer)
    {
        // Update health and health lost counter
        UpdateHealth(damage);

        float effectiveBravery = CalculateEffectiveBravery(attackingLayer);
        float randomValue = UnityEngine.Random.Range(0f, 100f);

        if (randomValue <= effectiveBravery)
        {
            currentAggression += damage;
        }
        else
        {
            currentFear += damage;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void Attack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, myLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            CrowdMember target = hitCollider.GetComponent<CrowdMember>();

            if (target != null && IsEnemyFaction(target.gameObject.layer))
            {
                target.TakeDamage(attackDamage, this.myLayer);

                lastAttackTime = Time.time;

                // Break out of the loop after attacking one target
                break;
            }
        }
    }

    private void Die()
    {
        //death-logic goes here

        Destroy(gameObject);
    }

    public bool IsEnemyFaction(LayerMask nearbyCrowdLayer)
    {
        GameManager.Faction nearbyFaction = LayerMaskToFaction(nearbyCrowdLayer);
        GameManager.Faction ownFaction = LayerMaskToFaction(this.myLayer);

        float randomThreshold = UnityEngine.Random.Range(minRangeEnemy, maxRangeEnemy);
        float factionStanding = gameManager.GetFactionStanding(ownFaction, nearbyFaction);

        return factionStanding < randomThreshold;
    }

    private GameManager.Faction LayerMaskToFaction(LayerMask layerMask)
    {
        
        int layerValue = (int)Mathf.Log(layerMask.value, 2);
        int factionIndex = layerValue - 10; // 10 is the first layer used for factions
        return (GameManager.Faction)factionIndex;
    }



}

