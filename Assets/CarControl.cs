using UnityEngine;
using UnityEngine.AI;

public class CarControl : MonoBehaviour
{
    public float motorTorque = 2000;
    public float brakeTorque = 2000;
    public float maxSpeed = 20;
    public float steeringRange = 30;
    public float steeringRangeAtMaxSpeed = 10;
    public float centreOfGravityOffset = -1f;

    public bool blocked = false;
    public float reverseTimePublic;
    private float reverseTime;

    public float reverseFactor;
    private float directionChangeLock;

    private float currentSteerAngle = 0f;
    public float steerSpeed = 5f; // Speed at which the steering changes

    public float steerSmooth = 0.1f; // Speed at which the steering changes

    public float slowDownDistance = 10f;

    private bool isReversing = false;
    private bool isAccelerating = false;

    public bool frontBlocked = false;
    public bool backBlocked = false;

    private float currentSteerVelocity = 0f;

    WheelControl[] wheels;
    Rigidbody rigidBody;

    public Collider frontTrigger;
    public Collider backTrigger;

    private NavMeshAgent agent;


    void Start()
    {
        directionChangeLock = -10.0f;
        reverseTime = -10.0f;
        agent = GetComponentInChildren<NavMeshAgent>();
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.centerOfMass += Vector3.up * centreOfGravityOffset;
        wheels = GetComponentsInChildren<WheelControl>();
    }

    public void SetDestination(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public void BlockedReverse()
    {
        if (reverseTime < 0.0f)
        {
            reverseTime = Time.time + reverseTimePublic;
        }
        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                currentSteerAngle = Mathf.Lerp(currentSteerAngle, steeringRange, steerSpeed * Time.deltaTime);
                wheel.WheelCollider.steerAngle = steeringRange;
            }
            if (wheel.motorized)
            {
                // Apply negative torque to reverse
                wheel.WheelCollider.motorTorque = -motorTorque;
                wheel.WheelCollider.brakeTorque = 0;
            }

            else
            {
                wheel.WheelCollider.motorTorque = 0;
            }
        }
        if (Time.time > reverseTime)
        {
            reverseTime = -10.0f;
            blocked = false;
        }

    }


    void Update()
    {

        agent.updatePosition = false;
        agent.updateRotation = false;

        if (blocked == true || reverseTime > Time.time + 1.0f)
        {
            BlockedReverse();
            agent.nextPosition = transform.position;
            return;
        }

        // dir from NavMeshAgent
        Vector3 desiredDirection = agent.desiredVelocity.normalized;
        float desiredSpeed = agent.desiredVelocity.magnitude;

        float currentSpeed = Vector3.Dot(transform.forward, rigidBody.velocity);
        bool movingForward = currentSpeed >= 0;


        float speedFactor = movingForward
                            ? Mathf.InverseLerp(0, maxSpeed, currentSpeed)
                            : Mathf.InverseLerp(0, -0.5f * maxSpeed, currentSpeed);


        // remaining distance
        float remainingDistance = Vector3.Distance(transform.position, agent.destination);

        // scale down the motor torque as the car approaches the destination
        float slowDownFactor = Mathf.Clamp01(remainingDistance / slowDownDistance);

        // current motor torque and steering range
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        // steering angle and motor torque based on dir
        float targetSteerAngle = Mathf.Clamp(Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up), -currentSteerRange, currentSteerRange);
        float smoothedAngle = Mathf.SmoothDampAngle(currentSteerAngle, targetSteerAngle, ref currentSteerVelocity, steerSmooth);
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, smoothedAngle, steerSpeed * Time.deltaTime);

        // reverse or forward?
        if (agent.pathPending)
        {
            directionChangeLock = -10.0f;
            isAccelerating = false;
            isReversing = false;
        }
        else if (frontBlocked && directionChangeLock > 0.1f)
        {
            isAccelerating = false;
            isReversing = false;
        }
        else if (frontBlocked && rigidBody.velocity.magnitude > 0.5f && movingForward)
        {
            isAccelerating = false;
            isReversing = false;
            directionChangeLock = Random.Range(1.0f, 3.0f);
        }
        else if (frontBlocked)
        {
            frontBlocked = false;
        }
        else if (backBlocked && directionChangeLock > 0.1f)
        {
            isAccelerating = false;
            isReversing = false;
        }
        else if (backBlocked && rigidBody.velocity.magnitude > 0.5f && !movingForward)
        {
            isAccelerating = false;
            isReversing = false;
            directionChangeLock = Random.Range(1.0f, 2.5f);
        }
        else if (backBlocked)
        {
            backBlocked = false;
        }
        else if (desiredSpeed > 0.001f && slowDownFactor > 0.5f && Vector3.Dot(transform.forward, desiredDirection) >= -reverseFactor)
        {
            if (isReversing == true && directionChangeLock > 0.1f)
            {
                isAccelerating = false;
                isReversing = true;
            }
            else if (isReversing == true)
            {
                isAccelerating = true;
                isReversing = false;
                directionChangeLock = 2.0f;
            }
            else
            {
                isAccelerating = true;
                isReversing = false;
            }

            if (rigidBody.velocity.magnitude < 1.0f && movingForward)
            {
                reverseFactor = Mathf.Lerp(reverseFactor, -1.00f, 0.25f * Time.deltaTime);
            }
            else if (movingForward)
            {
                reverseFactor = Mathf.Lerp(reverseFactor, 0.80f, 0.5f * Time.deltaTime);
            }
        }
        else if (desiredSpeed > 0.001f && slowDownFactor > 0.6f)
        {
            if (isAccelerating == true && directionChangeLock > 0.1f)
            {
                isAccelerating = true;
                isReversing = false;
            }
            else if (isAccelerating == true)
            {
                isAccelerating = false;
                isReversing = true;
                directionChangeLock = 2.0f;
            }
            else
            {
                isAccelerating = false;
                isReversing = true;
            }

            if (rigidBody.velocity.magnitude < 1.0f)
            {
                reverseFactor = Mathf.Lerp(reverseFactor, 1.10f, 0.10f * Time.deltaTime);

            }
            else if (!movingForward)
            {
                reverseFactor = Mathf.Lerp(reverseFactor, 0.80f, 0.08f * Time.deltaTime);
            }
        }
        else
        {
            isAccelerating = false;
            isReversing = false;
        }

        foreach (var wheel in wheels)
        {
            if (wheel.steerable)
            {
                if (!movingForward)
                {
                    wheel.WheelCollider.steerAngle = currentSteerAngle * 0.2f;
                }
                else
                {
                    wheel.WheelCollider.steerAngle = currentSteerAngle;
                }

            }

            if (isAccelerating)
            {
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = slowDownFactor * desiredSpeed * currentMotorTorque;
                }
                wheel.WheelCollider.brakeTorque = 0;
            }

            else if (isReversing)
            {
                if (wheel.motorized)
                {

                    wheel.WheelCollider.motorTorque = slowDownFactor * -desiredSpeed * currentMotorTorque;
                }
                wheel.WheelCollider.brakeTorque = 0;
            }
            else
            {

                wheel.WheelCollider.brakeTorque = brakeTorque;
                wheel.WheelCollider.motorTorque = 0;
            }
        }
        agent.nextPosition = transform.position;
        directionChangeLock -= Time.deltaTime;
    }
}

