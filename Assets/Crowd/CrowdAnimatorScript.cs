using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdAnimatorScript : MonoBehaviour
{
    private Rigidbody rb;
    private Animator anim;
    public float speedScale;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponentInParent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        anim.SetFloat("moveSpeed", (rb.velocity.magnitude * speedScale));
    }
}
