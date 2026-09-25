using UnityEngine;

public sealed class MinTooltipAttribute : PropertyAttribute
{
    public float Min { get; }
    public string Tooltip { get; }

    public MinTooltipAttribute(float min, string tooltip)
    {
        Min = min;
        Tooltip = tooltip;
    }
}
