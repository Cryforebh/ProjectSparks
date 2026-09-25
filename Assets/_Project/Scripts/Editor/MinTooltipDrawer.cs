using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinTooltipAttribute))]
public sealed class MinTooltipDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var minTooltip = (MinTooltipAttribute)attribute;

        label.tooltip = minTooltip.Tooltip;

        EditorGUI.BeginProperty(position, label, property);

        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                {
                    int min = Mathf.CeilToInt(minTooltip.Min);

                    int value = EditorGUI.IntField(position, label, property.intValue);
                    property.intValue = Mathf.Max(value, min);
                    break;
                }

            case SerializedPropertyType.Float:
                {
                    float value = EditorGUI.FloatField(position, label, property.floatValue);
                    property.floatValue = Mathf.Max(value, minTooltip.Min);
                    break;
                }

            default:
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    break;
                }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
