 using UnityEngine;
using System.Collections;

/* Example script to apply trauma to the camera or any game object */
public class FlashbangScript : MonoBehaviour 
{
    [Tooltip("Seconds to wait before trigerring the explosion particles and the trauma effect")]
    public float Delay = 1;
    [Tooltip("Maximum stress the effect can inflict upon objects Range([0,1])")]
    public float MaximumStress = 0.6f;
    [Tooltip("Maximum distance in which objects are affected by this FlashbangScript")]
    public float Range = 45;

    public float maxFearDistance;
    public float minFearDistance;
    public float maxDamageDistance;
    public float minDamageDistance;
    public float maxDamage;
    public float minDamage;
    public float maxFearImpact;
    public float minFearImpact;
    public bool bang;

    private Collider[] hitColliders;

    private Rigidbody rb;
    public float pushTime;
    public float pushForce;
    public float pushPercentage;

    private IEnumerator Start()
    {
        bang = false;
        rb = GetComponent<Rigidbody>();
        /* Wait for the specified delay */
        yield return new WaitForSeconds(Delay);
        /* Play all the particle system this object has */
        PlayParticles();

        /* Play all the sounds this object has */
        PlaySounds();
        bang = true;
        pushTime += Time.time;

        // SphereCast to affect crowd members
        hitColliders = Physics.OverlapSphere(transform.position, maxFearDistance);
        foreach (var hitCollider in hitColliders)
        {
            CrowdMember crowdMember = hitCollider.GetComponent<CrowdMember>();
            if (crowdMember != null)
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                ApplyFearAndDamage(crowdMember, distance);
            }
        }



        /* Find all gameobjects in the scene and loop through them until we find all the nearvy stress receivers */
        var targets = UnityEngine.Object.FindObjectsOfType<GameObject>();
        for(int i = 0; i < targets.Length; ++i)
        {
            var receiver = targets[i].GetComponent<StressReceiver>();
            if(receiver == null) continue;
            float distance = Vector3.Distance(transform.position, targets[i].transform.position);
            /* Apply stress to the object, adjusted for the distance */
            if(distance > Range) continue;
            float distance01 = Mathf.Clamp01(distance / Range);
            float stress = (1 - Mathf.Pow(distance01, 2)) * MaximumStress;
            receiver.InduceStress(stress);
        }
    }

    private void ApplyFearAndDamage(CrowdMember member, float distance)
    {
        if (distance <= minFearDistance)
        {
            // Maximum fear and damage
            member.currentFear += maxFearImpact;
            member.damageDirection = rb.position; 
            // Apply maximum damage logic if needed
        }
        else
        {
            // Linear falloff for fear
            float fearImpact = maxFearImpact - (maxFearImpact - minFearImpact) * ((distance - minFearDistance) / (maxFearDistance - minFearDistance));
            member.currentFear += fearImpact;

            // Apply damage logic with falloff if within damage range
            // ...
        }
    }

    /* Search for all the particle system in the game objects children */
    private void PlayParticles()
    {
        var children = transform.GetComponentsInChildren<ParticleSystem>();
        for(var i  = 0; i < children.Length; ++i)
        {
            children[i].Play();
        }
        var current = GetComponent<ParticleSystem>();
        if(current != null) current.Play();
    }
    private void PlaySounds()
    {
        var children = transform.GetComponentsInChildren<AudioSource>();
        for (var i = 0; i < children.Length; ++i)
        {
            children[i].Play(0);
        }
        var current = GetComponents<AudioSource>();
        if (current == null)
        {
            return;
        }
        else
        {
            current[Mathf.FloorToInt(Random.Range(0, current.Length))].Play(0);
        }

    }

    private void FixedUpdate()
    {
        if (bang && Time.time < pushTime)
        {
            foreach (var hitColliderForce in hitColliders)
            {
                Rigidbody rbc = hitColliderForce.GetComponent<Rigidbody>();
                if (rbc != null && Random.Range(0f,100f) < pushPercentage)
                {
                    CrowdMember crowdM = hitColliderForce.GetComponent<CrowdMember>();
                    if (crowdM != null)
                    {
                        rbc.AddForce((rbc.position - rb.position).normalized * Random.Range(0.01f * pushForce, (maxFearDistance / Vector3.Distance(rbc.position, rb.position)) * pushForce));
                    }
                    
                }
            }
        }

    }
}