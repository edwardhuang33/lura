using System.Collections;
using System.Collections.Generic;

public class AreaCollection
{
    public string name;
    public List<Area> areas;

    public AreaCollection()
    {
    }

    public virtual void AssignArea(Area area)
    {
        areas.Add(area);
    }

    public virtual void AssignAreas(List<Area> _areas, bool append = false)
    {
        if (append)
        {
            areas.AddRange(_areas);
        }
        else
        {
            areas = _areas;
        }
    }
}
