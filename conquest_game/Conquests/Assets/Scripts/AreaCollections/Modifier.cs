using System.Collections;
using System.Collections.Generic;

public class Modifier : AreaCollection
{
    public override void AssignArea(Area area)
    {
        base.AssignArea(area);
        area.modifier = this;
    }

    public override void AssignAreas(List<Area> areas, bool append = false)
    {
        base.AssignAreas(areas);
        foreach(Area area in areas)
        {
            area.modifier = this;
        }
    }
}
