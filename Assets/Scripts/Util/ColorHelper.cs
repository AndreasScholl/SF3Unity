using UnityEngine;

public class ColorHelper
{
    static public Color Convert(int color)
    {
        float b = ((color >> 10) & 0x1f) / (float)0x1f;
        float g = ((color >> 5) & 0x1f) / (float)0x1f;
        float r = (color & 0x1f) / (float)0x1f;
        Color rgbColor = new Color(r, g, b);
        return rgbColor;
    }

    static public Color Desaturate(Color color)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        s *= 0.7f;
        color = Color.HSVToRGB(h, s, v);

        return color;
    }
}
