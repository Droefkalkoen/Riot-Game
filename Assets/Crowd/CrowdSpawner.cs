using System.Collections.Generic;
using UnityEngine;

public class CrowdSpawner : MonoBehaviour
{
    public GameObject crowdMemberPrefab;
    public int numberOfCrowdMembers = 100;
    public Vector2 spawnArea = new Vector2(20, 20);

    [Range(0, 100)]
    public int confrontationalPercentage = 20; // Percentage of confrontational crowd members

    [Range(0, 100)]
    public int boidPercentage = 50;

    [Header("10: civilian, 11: police")]
    [Header("12: BrookridgeBrawlers, 13: RavenwoodRovers")]
    [Header("14: SilverhillUnited, 15: AFCStoneshire")]

    [Range(10, 15)]
    public int crowdLayer = 10;

    private List<GameObject> crowdMembers;

    private void Start()
    {
        crowdMembers = new List<GameObject>();

        // Spawn crowd members
        for (int i = 0; i < numberOfCrowdMembers; i++)
        {
            Vector3 spawnPosition = new Vector3(
                transform.position.x + Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                transform.position.y,
                transform.position.z + Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
            );

            GameObject crowdMember = Instantiate(crowdMemberPrefab, spawnPosition, Quaternion.identity);
            crowdMembers.Add(crowdMember);

            // Assign confrontational behavior to a certain percentage of crowd members
            if (Random.Range(0, 100) < confrontationalPercentage)
            {
                crowdMember.GetComponent<CrowdMember>().isConfrontational = true;
            }
            if (Random.Range(0, 100) < boidPercentage)
            {
                crowdMember.GetComponent<CrowdMember>().isBoid = true;
            }
            else
            {
                crowdMember.GetComponent<CrowdMember>().isBoid = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, 0, spawnArea.y));
    }
}
