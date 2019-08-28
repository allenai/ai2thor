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
    public Color flowColor;
    public Color topColor;
}

public class LiquidProperties {
    public static Dictionary<LiquidType, Liquid> liquids = new Dictionary<LiquidType, Liquid>() {
        {
            LiquidType.water, new Liquid {
                color = new Color(201 / 255.0f, 255 / 255.0f, 255 / 255.0f, 128 / 255.0f),
                flowColor = new Color(201 / 255.0f, 255 / 255.0f, 255 / 255.0f, 125 / 255.0f),
                topColor = new Color(201 / 255.0f, 255 / 255.0f, 255 / 255.0f, 128 / 255.0f)
             }
        },
        {
            LiquidType.coffee, new Liquid {
                color = new Color(62 / 255.0f, 40 / 255.0f, 30 / 255.0f, 255 / 255.0f),
                flowColor = new Color(119 / 255.0f, 43 / 255.0f, 0 / 255.0f, 198 / 255.0f),
                topColor = new Color(62 / 255.0f, 40 / 255.0f, 30 / 255.0f, 255 / 255.0f)
            }
        },
        {
            LiquidType.wine, new Liquid {
                color = new Color(51 / 255.0f, 0 / 255.0f, 5 / 255.0f, 255 / 255.0f),
                flowColor =  new Color(106 / 255.0f, 0 / 255.0f, 0 / 255.0f, 230 / 255.0f),
                topColor = new Color(51 / 255.0f, 0 / 255.0f, 5 / 255.0f, 255 / 255.0f)
            }
        }
    };
}