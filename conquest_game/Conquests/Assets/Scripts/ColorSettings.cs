using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSettings : MonoBehaviour
{
    public Color32 plains;
    public Color32 mountains;
    public Color32 water;
    public Color32 desert;
    public Color32 forest;
    public Color32 ice;
    public Color32 debug;

    public static Color32 GetCycledColor(int i, byte opacity = 255)
    {
        //Generate a color32 that is distinctly different from previous incremental i
        i = i + 1;
        byte r = (byte)(57*i % 256);
        byte g = (byte)((69*i+50) % 203);
        byte b = (byte)((41*i+100) % 193);

        // Debug.Log(r.ToString() + g.ToString() + b.ToString());

        return new Color32(r,g,b,opacity);
    }

    public static int RGBToInt(Color color)
    {
        return 0x10000 * (int)(color.r * 255) + 0x100 * (int)(color.g * 255) + (int)(color.b * 255);
    }

    public static int RGBToInt(Color32 color)
    {
        return 0x10000 * color.r + 0x100 * color.g + color.b;
    }
}
