using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class GameController : MonoBehaviour
{
    DataImporter dataImporter;
    UIController uic;
    CameraController cameraController;
    public PlayerNetworking net;
    MapRenderer mapRenderer;
    public Owner activeOwner, playerOwner;
    public List<Owner> ownerOrder;
    public GameState gs;
    Area storedArea1;
    Area storedArea2;
    int storedInt;
    List<Area> storedAreas1;

    public MapInfo mapInfo;
    public int unitIDCounter = 0;
    Dictionary<string, Area> areas;

    public NetworkingInfo netInfo = null;
    

    void Start()
    {
        dataImporter = GetComponent<DataImporter>();
        cameraController = Camera.main.GetComponent<CameraController>();
        uic = GetComponent<UIController>();
        gs = GameState.Lock;
    }

    public void NetworkStart()
    {
        mapInfo = dataImporter.GetData();
        
        areas = mapInfo.areas;

        uic.gc = this;

        uic.InitialDrawUnderlay();
        uic.InitialDrawOwnership();

        uic.ShowOwnership();

        foreach (Area area in mapInfo.areas.Values)
        {
            UnitSpawnInfo info = new UnitSpawnInfo(area);
            uic.DrawAreaInfo(area);
        }
        GameObject.FindObjectsOfType<NetworkManagerHUD>()[0].showGUI = false;
        List<string> ownerIDs = mapInfo.owners.Keys.ToList();
        uic.ShowDropdown(true, ownerIDs);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnClick();
        }
        else if(Input.GetKeyDown("1"))
        {
            uic.ResetOverlay();
        }
        else if(Input.GetKeyDown("2"))
        {
            uic.ShowOwnership();
        }
        else if(Input.GetKeyDown("3"))
        {
            uic.ShowContinents();
        }
        else if(Input.GetKeyDown("4"))
        {
            uic.ShowRegions();
        }
        else if(Input.GetKeyDown("5"))
        {
            uic.ShowModifiers();
        }
        else if(Input.GetKeyDown(KeyCode.Hash))
        {
            GameObject.FindObjectsOfType<NetworkManagerHUD>()[0].showGUI ^= true;
        }
        else 
        {
            cameraController.HandleInput();
        }
    }

    public void SetOwnerOrder(string[] strOrder)
    {
        ownerOrder = strOrder.Select(s => mapInfo.owners[s]).ToList();
    }

    public Owner NextOwner()
    {
        if (!netInfo.isServer)
        {
            Debug.Log("Warning: NextOwner called from non-server!");
        }

        if (activeOwner == null)
        {
            activeOwner = ownerOrder[0];
        }
        else
        {
            int index = ownerOrder.IndexOf(activeOwner);
            int newIndex = index == ownerOrder.Count - 1 ? 0 : index + 1;
            activeOwner = ownerOrder[newIndex];
        }
        return activeOwner;
    }

    public void InitTurn(string strOwner)
    {
        activeOwner = mapInfo.owners[strOwner];
        uic.SetActiveOwnerBox("Current turn: " + activeOwner.name);

        if (playerOwner != activeOwner)
        {
            InitBrowse();
        }
        else
        {
            //Make each owned area selectable and start deployment phase
            foreach (Owner _owner in mapInfo.owners.Values)
            {
                foreach (Area area in _owner.areas)
                {
                    area.selectable = _owner == playerOwner;
                }
            }
            uic.ShowEndTurn(true);
            InitDeployUnits();
        }
    }

    void OnClick() 
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Locator")))
        {
            //Get color32 of pixel that we hit
            Vector2 v2 = hit.textureCoord;
            Texture2D t2d = hit.collider.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D;
            v2.x *= t2d.width;
            v2.y *= t2d.height;
            Color32 color = t2d.GetPixel(Mathf.FloorToInt(v2.x), Mathf.FloorToInt(v2.y));
            string strArea = ColorSettings.RGBToInt(color).ToString();
            if (!areas.ContainsKey(strArea)) return;

            Area area = areas[strArea];
        
            if (gs == GameState.SelectMoveFrom )
            {
                uic.SelectArea(area);
                if (uic.selectedArea.owner == activeOwner) 
                {
                    uic.ShowPrompt1(true, "Move (Space)");
                }
                else
                {
                    uic.ShowPrompt1(false);
                } 
            }
            else if (gs == GameState.DeployUnits)
            {
                uic.SelectArea(area, false);
                if (storedInt > 0 && uic.selectedArea.selectable) 
                {

                    UnitSpawnInfo info = new UnitSpawnInfo(area);
                    net.CmdRequestSpawnUnit(info);
                    storedInt--;
                    uic.SetInfoBox("Remaining available to deploy: " + storedInt.ToString());
                }
            }
            else if (gs == GameState.SelectMoveTo)
            {
                uic.SelectArea(area);
                if (storedAreas1.Contains(uic.selectedArea)) 
                {
                    uic.ShowPrompt1(true, "Move Here (Space)");
                }
                else
                {
                    uic.ShowPrompt1(false);
                } 
            }
            else if (gs == GameState.Browse)
            {
                uic.SelectArea(area);
            }
        }
    }

    public void InitPregame()
    {
        InitBrowse();
        gs = GameState.Pregame;
        uic.ShowDropdown(false);
        uic.SetOwnerBox("Playing as: " + playerOwner.name);
        uic.ShowPrompt1(true, "Ready");
    }

    public void ReadyUp(bool ready)
    {
        net.CmdReadyUp(ready);
    }
    public void InitBrowse()
    {
        gs = GameState.Browse;
        uic.ResetHighlight();
        uic.ResetHighlight(true);
        storedInt = 0;
        storedArea1 = null;
        storedArea2 = null;
        storedAreas1 = null;
        uic.ShowEndTurn(false);
        uic.buttonPrompt1.interactable = true;
        uic.ShowPrompt1(false);
    }

    public void SpawnUnit(UnitSpawnInfo info, bool updateUI = true)
    {
        //Should only be called by PlayerNetworking RpcSpawnUnit
        Unit unit = new Unit(info, mapInfo);
        unit.area.AddUnit(unit);
        mapInfo.units[info.ID] = unit;
        if (updateUI)
        {
            uic.UpdateAreaInfo(unit.area);
        }
        
    }

    public void DespawnUnit(int unitID)
    {
        Unit unit = mapInfo.units[unitID];
        unit.owner.RemoveUnit(unit);
        unit.area.RemoveUnit(unit);
        uic.UpdateAreaInfo(unit.area);
    }

    public void ChangeOwner(string areaID, string ownerName)
    {
        Area area = mapInfo.areas[areaID];
        Owner owner = mapInfo.owners[ownerName];

        owner.AssignArea(area);
        uic.UpdateOwnership(new List<Area>{area});
    }

    public void MoveUnits(int[] unitIDs, string originID, string destinationID)
    {
        Area origin = mapInfo.areas[originID];
        Area destination = mapInfo.areas[destinationID];
        foreach (int unitID in unitIDs)
        {
            Unit unit = origin.units.Find(u => u.ID == unitID);
            unit.Move(destination);
        }
        uic.UpdateAreaInfo(destination);
        uic.UpdateAreaInfo(origin);
        
    }

    public void InitSelectMoveFrom()
    {
        uic.SetInfoBox("Select an area to view info.");
        gs = GameState.SelectMoveFrom;
        List<Area> selectable = activeOwner.areas.Where(c => c.selectable).ToList();
        uic.ShowPrompt1(false);

        uic.ResetHighlight();
        uic.ResetHighlight(true);
        uic.HighlightAreas(selectable, true);
    }

    public void InitSelectMoveUnits()
    {
        storedArea2 = uic.selectedArea;
        gs = GameState.SelectMoveUnits;
        uic.ShowSlider(true, 0, storedArea1.units.Count, "Select number of units to move");
    }

    public void InitSelectMoveTo()
    {
        uic.SetInfoBox("Select an area to view info.");
        gs = GameState.SelectMoveTo;
        storedArea1 = uic.selectedArea;
        List<Area> neighbours = storedArea1.neighbours;
        storedAreas1 = neighbours;
        uic.ResetHighlight(true);
        uic.HighlightAreas(neighbours, true);
    }

    public void ExecSelectMoveTo()
    {
        uic.ResetHighlight();
        uic.ResetHighlight(true);
        uic.ShowPrompt1(false);
        Area origin = storedArea1;
        Area destination = storedArea2;
        storedArea1 = null;
        storedArea2 = null;
        storedAreas1 = null;
        gs = GameState.SelectMoveFrom;
        ExecuteMove(origin, destination, origin.units.GetRange(0, (int)uic.slider.value));
    }

    void ExecuteMove(Area origin, Area destination, List<Unit> _attackers)
    {
        //Make copy of attackers for looping when we might be altering the original (which derives from area lists)
        List<Unit> attackers = new List<Unit>(_attackers);

        Debug.Log(string.Format("Now attacking from {0} with {1} attackers into {2}", origin.name, attackers.Count, destination.name));
        if (destination.owner == origin.owner)
        {
            net.CmdRequestMoveUnits(attackers.Select(u => u.ID).ToArray(), origin.ID, destination.ID);
        }
        else
        {
            BattleInfo info = new BattleInfo(attackers, destination.units, destination);
            BattleResult result = BattleSimulator.SimulateBattle(info);
            foreach (Unit unit in result.units)
            {
                if (unit.dead)
                {
                    net.CmdRequestDespawnUnit(unit.ID);
                }
                else
                {
                    unit.Reset();
                }
            }
            if (result.victor != destination.owner)
            {
                
                net.CmdRequestChangeOwner(destination.ID, origin.owner.name);
                int[] unitIDs = attackers.Where(u => !u.dead).Select(u => u.ID).ToArray();
                net.CmdRequestMoveUnits(unitIDs, origin.ID, destination.ID);
            }
        }

        origin.selectable = false;
        destination.selectable = false;
        InitSelectMoveFrom();
    }

    public void InitDeployUnits()
    {
        gs = GameState.DeployUnits;
        uic.ShowPrompt1(true, "Finish deployment");
        uic.ResetHighlight();
        uic.ResetHighlight(true);
        uic.HighlightAreas(activeOwner.areas, true);

        storedInt = activeOwner.areas.Count;
        uic.SetInfoBox("Remaining available to deploy: " + storedInt.ToString());
    }

    public void ExecEndTurn()
    {
        InitBrowse();
        net.CmdRequestNextTurn();
    }
}

public enum GameState
{
    Lock,
    Pregame,
    Browse,
    DeployUnits,
    SelectMoveFrom,
    SelectMoveTo,
    SelectMoveUnits,
}