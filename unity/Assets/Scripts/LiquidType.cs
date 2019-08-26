using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum LiquidType {
    none = 0,
    water,
    coffee,
    wine
}

public class Liquid {
    public Color color;
}

public class LiquidProperties {
    public static Dictionary<LiquidType, Liquid> liquids = new Dictionary<LiquidType, Liquid>() {
        {LiquidType.water, new Liquid { color = new Color(95 / 255.0f, 200 / 255.0f, 255 / 255.0f, 134 / 255.0f) } },
        {LiquidType.wine, new Liquid { color = new Color(207 / 255.0f, 65 / 255.0f, 65 / 255.0f, 189 / 255.0f) } },
        {LiquidType.coffee, new Liquid { color = new Color(47 / 255.0f, 15 / 255.0f, 7 / 255.0f, 221 / 255.0f) } }
    };
}