using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PoliceAnimatorScript : MonoBehaviour
{
    private NavMeshAgent rb;
    private Animator anim;
    public float speedScale;
    private float lastSpeed;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponentInParent<NavMeshAgent>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lastSpeed = Mathf.Lerp(lastSpeed, rb.velocity.magnitude * speedScale, Time.deltaTime);
        anim.SetFloat("moveSpeed", lastSpeed);
        
    }
}
