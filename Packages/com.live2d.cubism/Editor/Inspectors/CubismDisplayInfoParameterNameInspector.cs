/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;
using UnityEditor;
using Live2D.Cubism.Framework;

namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismHarmonicMotionParameter"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismDisplayInfoParameterName)), CanEditMultipleObjects]
    public class CubismDisplayInfoParameterNameInspector : UnityEditor.Editor
    {
        #region Editor

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var parameter = target as CubismDisplayInfoParameterName;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Name", parameter.Name);
            EditorGUILayout.TextField("Group", parameter.GroupId);
            EditorGUI.EndDisabledGroup();

            parameter.DisplayName = EditorGUILayout.TextField("Display Name", parameter.DisplayName);
        }
        #endregion
    }
}
