using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Owner : AreaCollection
{
    public List<Unit> units = new List<Unit>();
    public Color32 color;
    public NetworkConnection conn = null;

    public override void AssignArea(Area area)
    {
        area.owner.areas.Remove(area);
        base.AssignArea(area);
        area.AssignOwner(this);
    }

    public override void AssignAreas(List<Area> areas, bool append = false)
    {
        base.AssignAreas(areas);
        foreach(Area area in areas)
        {
            area.AssignOwner(this);
        }
    }

    public void AddUnit(Unit unit)
    {
        units.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void AssignColor(Color32 _color)
    {
        color = _color;
    }
}
