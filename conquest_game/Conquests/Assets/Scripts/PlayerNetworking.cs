using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class PlayerNetworking : NetworkBehaviour
{
    GameController gc;
    
    public override void OnStartServer()
    {
        gc = GameObject.FindWithTag("GameController").GetComponent<GameController>();

        //Calling here as guarantees only server gc finds a networking info.
        gc.netInfo = gc.GetComponent<NetworkingInfo>();
        gc.netInfo.isServer = true;
    }

    public override void OnStartClient()
    {
        gc = GameObject.FindWithTag("GameController").GetComponent<GameController>();

    }

    public override void OnStartLocalPlayer()
    {
        gc = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        gc.net = this;
        gc.NetworkStart();
    }

    [Command]
    public void CmdRequestOwnership(string ownerID)
    {
        Debug.Log(string.Format("Ownership of {0} was requested by {1}", ownerID, connectionToClient));
        Owner owner = gc.mapInfo.owners[ownerID];
        if (owner.conn == null)
        {
            Debug.Log(string.Format("Assigning ownership of {0} to {1}", ownerID, connectionToClient));
            owner.conn = connectionToClient;
            TargetAssignOwnership(ownerID);
            gc.netInfo.readiness.Add(connectionToClient, false);
        }
        else
        {
            Debug.Log(ownerID + "is already assigned to " + owner.conn.ToString());
        }
        
    }
    
    [TargetRpc]
    public void TargetAssignOwnership(string ownerID)
    {
        Debug.Log("Received authorisation to own " + ownerID);
        gc.playerOwner = gc.mapInfo.owners[ownerID];
        gc.InitPregame();
    }

    [Command]
    public void CmdReadyUp(bool ready)
    {
        gc.netInfo.readiness[connectionToClient] = ready;
        List<bool> readinessBools = gc.netInfo.readiness.Values.ToList();
        if (readinessBools.All(b => b == true))
        {
            // Debug.Log("All players ready");
            List<Owner> ownerOrder = gc.netInfo.GenerateOwnerOrder(gc.mapInfo.owners.Values.ToList());
            string[] strOwnerOrder = ownerOrder.Select(o => o.name).ToArray();
            gc.SetOwnerOrder(strOwnerOrder);
            
            Owner active;
            do
            {
                active = gc.NextOwner();
                Debug.Log("Beginning turn for " + active.name);
            } while (active.conn == null);
            
            RpcStartGame(strOwnerOrder, active.name);

            foreach (Area area in gc.mapInfo.areas.Values)
            {
                if (area.modifier.name != "Untraversable")
                {
                    UnitSpawnInfo info = new UnitSpawnInfo(area);
                    gc.unitIDCounter++;
                    info.ID = gc.unitIDCounter;
                    RpcSpawnUnit(info);
                }
            }
        }
    }

    [ClientRpc]
    public void RpcStartGame(string[] strOrder, string strActive)
    {
        //Slightly confusing as will be called on the player object of the last person to ready up.
        //But works as will still be a unique call.
        gc.SetOwnerOrder(strOrder);
        gc.activeOwner = null;
        gc.InitTurn(strActive);
    }

    [Command]
    public void CmdRequestNextTurn()
    {
        Owner active;
        do
        {
            active = gc.NextOwner();
            Debug.Log("Beginning turn for " + active.name);
        } while (active.conn == null);
        RpcNextTurn(active.name);
    }

    [ClientRpc]
    public void RpcNextTurn(string strActive)
    {
        gc.InitTurn(strActive);
    }

    [Command]
    public void CmdRequestSpawnUnit(UnitSpawnInfo info)
    {
        gc.unitIDCounter++;
        info.ID = gc.unitIDCounter;
        RpcSpawnUnit(info);
    }

    [ClientRpc]
    public void RpcSpawnUnit(UnitSpawnInfo info)
    {
        gc.SpawnUnit(info);
    }

    [Command]
    public void CmdRequestDespawnUnit(int unitID)
    {
        RpcDespawnUnit(unitID);
    }

    [ClientRpc]
    public void RpcDespawnUnit(int unitID)
    {
        gc.DespawnUnit(unitID);
    }

    [Command]
    public void CmdRequestChangeOwner(string areaID, string ownerName)
    {
        RpcChangeOwner(areaID, ownerName);
    }

    [ClientRpc]
    public void RpcChangeOwner(string areaID, string ownerName)
    {
        gc.ChangeOwner(areaID, ownerName);
    }

    [Command]
    public void CmdRequestMoveUnits(int[] unitIDs, string originID, string destinationID)
    {
        RpcMoveUnits(unitIDs, originID, destinationID);
    }

    [ClientRpc]
    public void RpcMoveUnits(int[] unitIDs, string originID, string destinationID)
    {
        gc.MoveUnits(unitIDs, originID, destinationID);
    }

}
