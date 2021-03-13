using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;


public class CarArea : MonoBehaviour
{
    public CinemachineSmoothPath racePath;
    public GameObject checkpointprefab;
    public GameObject finishCheckpointprefab;
    public bool trainingMode;
    public List<CarAgent> CarAgents { get; private set; }
    public List<GameObject> Checkpoints { get; private set; }
    
    private void Awake()
    {
        CarAgents = transform.GetComponentsInChildren<CarAgent>().ToList();
        Debug.Assert(CarAgents.Count > 0, "No AircraftAgents found");

        
    }
    private void Start()
    {
        Debug.Assert(racePath != null, "Racepath was not set");
        Checkpoints = new List<GameObject>();
        int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
        for (int i = 0; i < numCheckpoints; i++)
        {
            GameObject checkpoint;
            if (i == numCheckpoints - 1) checkpoint = Instantiate<GameObject>(finishCheckpointprefab);
            else checkpoint = Instantiate<GameObject>(checkpointprefab);
            checkpoint.transform.SetParent(racePath.transform);
            checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
            checkpoint.transform.localRotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);
            Checkpoints.Add(checkpoint);
        }
    }


    public void ResetAgentPosition(CarAgent agent, bool randomize = false)
    {
        if (randomize)
        {

            agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
        }


        int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
        if (previousCheckpointIndex == -1) previousCheckpointIndex = Checkpoints.Count - 1;

        float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);


        Vector3 basePosition = racePath.EvaluatePosition(startPosition);


        Quaternion orientation = racePath.EvaluateOrientation(startPosition);


        Vector3 positionOffset = Vector3.right * (CarAgents.IndexOf(agent) - CarAgents.Count / 2f)
            * UnityEngine.Random.Range(9f, 10f);


        agent.transform.position = basePosition + orientation * positionOffset;
        agent.transform.rotation = orientation;
    }
}

