using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class NetworkingInfo : MonoBehaviour
{
    //This is for server-only variables and methods
    public bool isServer = false;
    public Dictionary<NetworkConnection,bool> readiness  = new Dictionary<NetworkConnection,bool>();

    public List<Owner> GenerateOwnerOrder(List<Owner> owners)
    {
        owners = owners.Where(o => o.name != "No Owner").ToList();
        BattleSimulator.Shuffle(owners);
        return owners;
    }

}
