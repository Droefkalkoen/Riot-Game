using UnityEngine;
using UnityEngine.AI;

public class PoliceOfficer : MonoBehaviour
{
    // Start is called before the first frame update
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Quaternion direction;

    public float rotationSpeed;



    void Start()
    {


    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (agent.remainingDistance < agent.stoppingDistance && direction != null)
        {
            Quaternion currentRotation = transform.rotation;

            // Project on plane
            Vector3 projectedForward = Vector3.ProjectOnPlane(direction * Vector3.forward, Vector3.up).normalized;
            Quaternion projectedRotation = Quaternion.LookRotation(projectedForward);

            Quaternion newRotation = Quaternion.Slerp(currentRotation, projectedRotation, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(newRotation);
        }
    }
    public void MoveToPosition(Vector3 destination, Quaternion dir)
    {
        agent.destination = destination;
        direction = dir;
    }
}
