using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class DataImporter : MonoBehaviour
{
    [SerializeField]
    TextAsset mapInfoJSON;

    // Start is called before the first frame update
    public MapInfo GetData()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        JSONMapInfo jsonMapInfo = JsonConvert.DeserializeObject<JSONMapInfo>(mapInfoJSON.text);
        stopwatch.Stop();
        UnityEngine.Debug.Log(string.Format("Imported JSON data in {0} ms", stopwatch.ElapsedMilliseconds));

        stopwatch.Start();

        MapInfo mapInfo = new MapInfo();
        mapInfo.CalculateInfo(jsonMapInfo);
        stopwatch.Stop();
        UnityEngine.Debug.Log(string.Format("Additional area data processing complete in {0} ms", stopwatch.ElapsedMilliseconds));
        return mapInfo;
    }
}


public class MapInfo
{
    public Dictionary<string,Area> areas;
    public Dictionary<string,Owner> owners;
    public Dictionary<string,Region> regions;
    public Dictionary<string,Continent> continents;
    public Dictionary<string,Terrain> terrains;
    public Dictionary<string,Modifier> modifiers;
    public Dictionary<int, Unit> units; //This is added to incrementally during the game

    public MapInfo() {}

    public void CalculateInfo(JSONMapInfo json)
    {
        areas = new Dictionary<string,Area>();
        foreach(string ID in json.ID.Values)
        {
            areas[ID] = new Area(ID, json);
        }
        foreach (Area area in areas.Values)
        {
            List<Area> neighboursList = new List<Area>();
            foreach (string strNeighbour in json.neighbours[area.ID])
            {
                neighboursList.Add(areas[strNeighbour]);
            }
            area.AssignNeighbours(neighboursList);
        }

        regions = new Dictionary<string, Region>();
        foreach(string key in json.regionInfo.Keys)
        {
            Region region = new Region();
            region.name = json.regionInfo[key];
            List<Area> areaList = new List<Area>();
            foreach (string value in json.areasInRegion[key])
            {
                areaList.Add(areas[value]);
            }
            region.AssignAreas(areaList);
            regions[json.regionInfo[key]] = region;
        }
        
        continents = new Dictionary<string, Continent>();
        foreach(string key in json.continentInfo.Keys)
        {
            Continent continent = new Continent();
            continent.name = json.continentInfo[key];
            List<Area> areaList = new List<Area>();
            foreach (string value in json.areasInContinent[key])
            {
                areaList.Add(areas[value]);
            }
            continent.AssignAreas(areaList);
            continents[json.continentInfo[key]] = continent;
        }
        
        modifiers = new Dictionary<string, Modifier>();
        foreach(string key in json.modifierInfo.Keys)
        {
            Modifier modifier = new Modifier();
            modifier.name = json.modifierInfo[key];
            List<Area> areaList = new List<Area>();
            foreach (string value in json.areasInModifier[key])
            {
                areaList.Add(areas[value]);
            }
            modifier.AssignAreas(areaList);
            modifiers[json.modifierInfo[key]] = modifier;
        }

        terrains = new Dictionary<string, Terrain>();
        foreach(string key in json.terrainInfo.Keys)
        {
            Terrain terrain = new Terrain();
            terrain.name = json.terrainInfo[key];
            List<Area> areaList = new List<Area>();
            foreach (string value in json.areasInTerrain[key])
            {
                areaList.Add(areas[value]);
            }
            terrain.AssignAreas(areaList);
            terrains[json.terrainInfo[key]] = terrain;
        }
        
        owners = new Dictionary<string, Owner>();
        foreach(string key in json.ownerInfo.Keys)
        {
            Owner owner = new Owner();
            owner.name = json.ownerInfo[key];
            List<Area> areaList = new List<Area>();
            foreach (string value in json.areasInOwner[key])
            {
                areaList.Add(areas[value]);
            }
            owner.AssignAreas(areaList);
            owners[json.ownerInfo[key]] = owner;
        }

        units = new Dictionary<int, Unit>();
    }
}

[System.Serializable]
public class JSONMapInfo
{
    public Dictionary<string,string> ID;
    public Dictionary<string,string> name;
    public Dictionary<string,bool> contiguous;
    public Dictionary<string,bool> capitol;
    public Dictionary<string,List<int>> coords;
    public Dictionary<string,List<string>> neighbours;
    public Dictionary<string,List<string>> areasInContinent;
    public Dictionary<string,List<string>> areasInRegion;
    public Dictionary<string,List<string>> areasInTerrain;
    public Dictionary<string,List<string>> areasInModifier;
    public Dictionary<string,List<string>> areasInOwner;
    public Dictionary<string,string> continentInfo;
    public Dictionary<string,string> regionInfo;
    public Dictionary<string,string> terrainInfo;
    public Dictionary<string,string> modifierInfo;
    public Dictionary<string,string> ownerInfo;
    public Dictionary<string, List<int>> centre;

    
}