using System.Collections.Generic;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class FactionPair
    {
        public Faction faction1;
        public Faction faction2;
        public float standing;
    }


    public enum Faction
    {
        Civilians,
        Police,
        BrookbridgeBrawlers,
        RavenwoodRovers,
        SilverhillUnited,
        AFCStoneshire
    }

    [Header("Faction Standings")]
    // Expose this list in the inspector
    public List<FactionPair> factionStandings;

    public static GameManager instance;

    public LayerMask[] factionLayers;
    public Dictionary<int, int> factionMemberCounts;
    public Dictionary<int, int> factionMembersCount = new Dictionary<int, int>();
    public Dictionary<int, float> factionHealthLost;

    private int frameCounter;
    private int randomFrameInterval;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        factionMemberCounts = new Dictionary<int, int>();
        factionHealthLost = new Dictionary<int, float>();
        randomFrameInterval = Random.Range(1000, 3001);

        foreach (LayerMask layer in factionLayers)
        {
            factionMemberCounts[layer.value] = 0;
            factionHealthLost[layer.value] = 0;
        }

        
    }

    private void FixedUpdate()
    {
        frameCounter++;

        if (frameCounter % randomFrameInterval == 0)
        {
            UpdateFactionCounts();
            UpdateFactionHealthLost();
            randomFrameInterval = Random.Range(1000, 3001);
        }
    }

    private void UpdateFactionCounts()
    {
        foreach (LayerMask layer in factionLayers)
        {
            int factionCount = GameObject.FindObjectsOfType<CrowdMember>(true).Length;
            factionMemberCounts[layer.value] = factionCount;
        }
    }

    private bool FactionPairExists(Faction faction1, Faction faction2)
    {
        foreach (FactionPair pair in factionStandings)
        {
            if ((pair.faction1 == faction1 && pair.faction2 == faction2) ||
                (pair.faction1 == faction2 && pair.faction2 == faction1))
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateFactionHealthLost()
    {
        // Reset the health lost values for all factions
        foreach (LayerMask layer in factionLayers)
        {
            factionHealthLost[layer.value] = 0;
        }

        // Iterate through crowd  and add health lost since the last update
        CrowdMember[] crowdMembers = GameObject.FindObjectsOfType<CrowdMember>();
        foreach (CrowdMember member in crowdMembers)
        {
            int layer = member.gameObject.layer;
            if (factionHealthLost.ContainsKey(layer))
            {
                factionHealthLost[layer] += member.HealthLostSinceLastUpdate();
                member.ResetHealthLostCounter();
            }
        }
    }
    public int GetFactionMemberCount(int layer)
    {
        if (factionMembersCount.ContainsKey(layer))
        {
            return factionMembersCount[layer];
        }

        return 0;
    }

    public float GetFactionHealthLost(int layer)
    {
        if (factionHealthLost.ContainsKey(layer))
        {
            return factionHealthLost[layer];
        }

        return 0;
    }

    public float GetFactionStanding(Faction faction1, Faction faction2)
    {
        foreach (FactionPair pair in factionStandings)
        {
            if ((pair.faction1 == faction1 && pair.faction2 == faction2) ||
                (pair.faction1 == faction2 && pair.faction2 == faction1))
            {
                Debug.Log(pair.standing);
                return pair.standing;
            }
        }

        // If no pair is found, return 0 as default standing
        //Debug.LogError("Faction pair not found, defaulting to standing '0'");
        return 0f;
    }





}
