using Live2D.Cubism.Core;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor.Inspectors
{
    [CustomEditor(typeof(CubismParameter))]
    internal sealed class CubismParameterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var parameter = target as CubismParameter;
            GUI.enabled = false;
            EditorGUILayout.FloatField("Default Value", parameter.DefaultValue);
            EditorGUILayout.FloatField("Minimum Value", parameter.MinimumValue);
            EditorGUILayout.FloatField("Maximum Value", parameter.MaximumValue);
            GUI.enabled = true;
        }

    }
}