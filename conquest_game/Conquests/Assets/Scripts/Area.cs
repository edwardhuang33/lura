using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Area
{
    public string ID { get; private set; }
    public string name { get; private set; }
    public Region region;
    public Continent continent;
    public Terrain terrain;
    public Modifier modifier;
    public bool capitol { get; private set; }
    public bool contiguous { get; private set; }
    public List<int> coords { get; private set; }
    public List<int> centre { get; private set; }
    public List<Unit> units  { get; private set; }
    public List<Area> neighbours { get; private set; }
    public Owner owner { get; private set; }
    public bool selectable;


    public Area(string _id, JSONMapInfo info)
    {
        ID = info.ID[_id];
        name = info.name[_id];
        capitol = info.capitol[_id];
        contiguous = info.contiguous[_id];
        coords = info.coords[_id];
        centre = info.centre[_id];

        units = new List<Unit>();
    }

    public List<Unit> UpdateUnits(List<Unit> _units)
    {
        units = _units;
        return units;
    }  

    public List<Unit> AddUnit(Unit unit)
    {
        units.Add(unit);
        return units;
    }

    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void AssignOwner(Owner _owner)
    {
        //This should only be called by AssignArea in Owner
        owner = _owner;
    }

    public void AssignNeighbours(List<Area> _neighbours)
    {
        neighbours = _neighbours;
    }
    
}
