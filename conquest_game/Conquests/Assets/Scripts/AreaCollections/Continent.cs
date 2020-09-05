using System.Collections;
using System.Collections.Generic;

public class Continent : AreaCollection
{
    public override void AssignArea(Area area)
    {
        base.AssignArea(area);
        area.continent = this;
    }

    public override void AssignAreas(List<Area> areas, bool append = false)
    {
        base.AssignAreas(areas);
        foreach(Area area in areas)
        {
            area.continent = this;
        }
    }
}
