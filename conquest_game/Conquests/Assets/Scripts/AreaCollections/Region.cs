using System.Collections;
using System.Collections.Generic;

public class Region : AreaCollection
{
    public override void AssignArea(Area area)
    {
        base.AssignArea(area);
        area.region = this;
    }

    public override void AssignAreas(List<Area> areas, bool append = false)
    {
        base.AssignAreas(areas);
        foreach(Area area in areas)
        {
            area.region = this;
        }
    }
}
