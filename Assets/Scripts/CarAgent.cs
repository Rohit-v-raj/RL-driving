using System.Collections;
using Unity.MLAgents;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;



public class CarAgent : Agent
{
    [Header("Movement Parameters")]
    
    private float m_steeringAngle;

    public WheelCollider frontDriverW, frontPassengerW;
    public WheelCollider rearDriverW, rearPassengerW;
    public Transform frontDriverT, frontPassengerT;
    public Transform rearDriverT, rearPassengerT;
    public float maxSteerAngle = 30;
    public float motorForce = 200;
    public float thrust = 1500f;
    public float checkpointradius=5f;
    [Header("Explosion Stuff")]
    [Tooltip("The aircraft mesh that will disappear on explosion")]
    public GameObject meshObject;

    [Tooltip("The game object of the explosion particle effect")]
    public GameObject explosionEffect;

    [Header("Training")]
    [Tooltip("Number of steps to time out after in training")]
    public int stepTimeout = 5000;

    public int NextCheckpointIndex { get; set; }

    // Components to keep track of
    private CarArea area;
    new private Rigidbody rigidbody;
    

    // When the next step timeout will be during training
    private float nextStepTimeout;

    // Whether the aircraft is frozen (intentionally not flying)
    private bool frozen = false;

    // Controls
    private float forceChange = 0f;
    private float turnChange = 0f;

    public override void Initialize()
    {
        //Debug.Log("yes4");
        base.Initialize();
        area = GetComponentInParent<CarArea>();
        rigidbody = GetComponent<Rigidbody>();
       
        // Override the max step set in the inspector
        // Max 5000 steps if training, infinite steps if racing
        MaxStep = area.trainingMode ? 150000 : 0;
    }
    public override void Heuristic(float[] a)
    {
       // Debug.Log("yes3");
        float forwardAction = 0f;
        float turnAction = 0f;
        
        if (Input.GetKey("w"))
        {
            forwardAction = 1f;
        }
        if (Input.GetKey("a"))
        {
            turnAction = 2f;
        }

        if (Input.GetKey("s"))
        {
            forwardAction = 2f;
        }

        if (Input.GetKey("d"))
        {
            turnAction = 1f;
        }


        
        a[0] = forwardAction;
        a[1] = turnAction;

    }
    /// <summary>
    /// Read action inputs from vectorAction
    /// </summary>
    /// <param name="vectorAction">The chosen actions</param>
    /// <param name="textAction">The chosen text action (unused)</param>
    public override void OnActionReceived(float[] vectorAction)
    {
       // Debug.Log("yes");
        // Read values for pitch and yaw
        forceChange = vectorAction[0]; // up or none
        if (forceChange == 2) forceChange = -1f; // down
        turnChange = vectorAction[1]; // turn right or none
        if (turnChange == 2) turnChange = -1f; // turn left

        // Read value for boost and enable/disable trail renderer
     

        if (frozen) return;

        ProcessMovement();

        if (area.trainingMode)
        {
            // Small negative reward every step
            AddReward(-5f / MaxStep);

            // Make sure we haven't run out of time if training
            if (StepCount > nextStepTimeout)
            {
                AddReward(-.5f);
                EndEpisode();
            }

           
            Vector3 localCheckpointDir = VectorToNextCheckpoint();
            if (localCheckpointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f))
            {
                GotCheckpoint();
            }
        }
    }

    /// <summary>
    /// Collects observations used by agent to make decisions
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
      //  Debug.Log("yes5");
        // Observe aircraft velocity (1 vector3 = 3 values)
        sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

        // Where is the next checkpoint? (1 vector3 = 3 values)
        sensor.AddObservation(VectorToNextCheckpoint());

        // Orientation of the next checkpoint (1 vector3 = 3 values)
        Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
        sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));


    }

    public override void OnEpisodeBegin()
    {
        // Reset the velocity, position, and orientation
        rigidbody.velocity = Vector3.zero;
       
        area.ResetAgentPosition(agent: this, randomize: area.trainingMode);

        // Update the step timeout if training
        if (area.trainingMode) nextStepTimeout = StepCount + stepTimeout;
    }

    /// <summary>
    /// Prevent the agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
      Debug.Log("yes2");
        Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
        frozen = true;
        rigidbody.Sleep();
       
    }

    /// <summary>
    /// Resume agent movement and actions
    /// </summary>
    public void ThawAgent()
    {
        Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// Called when the agent flies through the correct checkpoint
    /// </summary>
    private void GotCheckpoint()
    {
        // Next checkpoint reached, update
        NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

        if (area.trainingMode)
        {
            AddReward(.5f);
            nextStepTimeout = StepCount + stepTimeout;
        }
    }

    /// <summary>
    /// Gets a vector to the next checkpoint the agent needs to fly through
    /// </summary>
    /// <returns>A local-space vector</returns>
    private Vector3 VectorToNextCheckpoint()
    {
        Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
        Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
        return localCheckpointDir;
    }
    private void ProcessMovement()
    {
       
        rigidbody.AddForce(transform.forward * thrust , ForceMode.Force);
        Steer();
        Accelerate();
        UpdateWheelPoses();
    }
    
   

    private void Steer()
    {
            m_steeringAngle = maxSteerAngle *turnChange ;
            frontDriverW.steerAngle = m_steeringAngle;
            frontPassengerW.steerAngle = m_steeringAngle;
    }

    private void Accelerate()
    {
            frontDriverW.motorTorque = forceChange * motorForce;
            frontPassengerW.motorTorque = forceChange * motorForce;
    }

    private void UpdateWheelPoses()
    {
            UpdateWheelPose(frontDriverW, frontDriverT);
            UpdateWheelPose(frontPassengerW, frontPassengerT);
            UpdateWheelPose(rearDriverW, rearDriverT);
            UpdateWheelPose(rearPassengerW, rearPassengerT);
    }

    private void UpdateWheelPose(WheelCollider _collider, Transform _transform)
    {
            Vector3 _pos = _transform.position;
            Quaternion _quat = _transform.rotation;

            _collider.GetWorldPose(out _pos, out _quat);

            _transform.position = _pos;
            _transform.rotation = _quat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("checkpoint") &&
            other.gameObject == area.Checkpoints[NextCheckpointIndex])
        {
            GotCheckpoint();
        }
        else if (other.transform.CompareTag("boundary"))
        {
            Debug.Log("yes boundary");
            AddReward(-2.5f);
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
       
        if (!collision.transform.CompareTag("agent") & !collision.transform.CompareTag("Untagged") &!collision.transform.CompareTag("checkpoint"))
        {
          
            // We hit something that wasn't another agent
            if (area.trainingMode)
            {
                    Debug.Log("yes1");
                    AddReward(-1.5f);
                EndEpisode();
                return;
                
            }
            else
            {
                Debug.Log("yes10");
                StartCoroutine(ExplosionReset());
            }
        }
    }

    /// <summary>
    /// Resets the aircraft to the most recent complete checkpoint
    /// </summary>
    /// <returns>yield return</returns>
    private IEnumerator ExplosionReset()
    {
        FreezeAgent();

        // Disable aircraft mesh object, enable explosion
        meshObject.SetActive(false);
        explosionEffect.SetActive(true);
        yield return new WaitForSeconds(2f);

        // Disable explosion, re-enable aircraft mesh
        meshObject.SetActive(true);
        explosionEffect.SetActive(false);
        area.ResetAgentPosition(agent: this);
        yield return new WaitForSeconds(1f);

        ThawAgent();
    }
}