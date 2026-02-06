/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Live2D.Cubism.Editor.Importers
{
    [ScriptedImporter(1, "exp3.json", CubismImporterPriorities.Expression3JsonImporter)]
    public sealed class CubismExpression3JsonImporter : ScriptedImporter
    {
        [InitializeOnLoadMethod]
        private static void RegisterModelImport()
        {
            CubismModel3JsonImporter.OnDidImportModel += OnModelImport;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var parentDirectory = Path.GetDirectoryName(ctx.assetPath);
            var model3JsonPath = FindModel3JsonFile(parentDirectory);
            if (model3JsonPath == null)
            {
                ctx.LogImportError("Unable to find model3.json file in current directory or parent directories.");
                return;
            }

            ctx.DependsOnSourceAsset(model3JsonPath);

            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ctx.assetPath));
            var data = File.ReadAllText(fullPath);
            var expressionJson = CubismExp3Json.LoadFrom(data);
            if (expressionJson == null)
            {
                ctx.LogImportError("Unable to load exp3.json.");
                return;
            }

            var expressionData = CubismExpressionData.CreateInstance(expressionJson);
            ctx.AddObjectToAsset("expressionData", expressionData);
            ctx.SetMainObject(expressionData);
        }

        private static string FindModel3JsonFile(string startDirectory)
        {
            if (string.IsNullOrEmpty(startDirectory))
                return null;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var currentDirectory = startDirectory;

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                var fullDir = Path.GetFullPath(Path.Combine(projectRoot, currentDirectory));
                if (Directory.Exists(fullDir))
                {
                    var files = Directory.GetFiles(fullDir, "*.model3.json");
                    if (files.Length > 0)
                    {
                        var fileName = Path.GetFileName(files[0]);
                        return Path.Combine(currentDirectory, fileName).Replace("\\", "/");
                    }
                }

                currentDirectory = Path.GetDirectoryName(currentDirectory);
                if (currentDirectory != null && !currentDirectory.StartsWith("Assets") && !currentDirectory.StartsWith("/"))
                    break;
            }

            return null;
        }

        private static void OnModelImport(IModelImportContext ctx)
        {
            if (ctx.Model3Json == null || ctx.Model3Json.FileReferences.Expressions == null || ctx.Model3Json.FileReferences.Expressions.Length == 0)
                return;

            var expressionController = (CubismExpressionController)ctx.Model.GetOrAddComponent(typeof(CubismExpressionController));
            var expressionList = ScriptableObject.CreateInstance<CubismExpressionList>();
            expressionList.name = $"{ctx.ModelName}.expressionList";
            ctx.AddSubObject(expressionList);

            var directoryName = Path.GetDirectoryName(ctx.AssetPath);
            if (string.IsNullOrEmpty(directoryName))
                directoryName = "Assets";

            var expressionDataList = new List<CubismExpressionData>();
            foreach (var expression in ctx.Model3Json.FileReferences.Expressions)
            {
                if (string.IsNullOrWhiteSpace(expression.File))
                    continue;

                var expressionPath = Path.Combine(directoryName, expression.File).Replace("\\", "/");
                var expressionData = AssetDatabase.LoadAssetAtPath<CubismExpressionData>(expressionPath);
                if (expressionData != null)
                    expressionDataList.Add(expressionData);
                else
                    Debug.LogWarning($"Unable to load expression: {expressionPath}");

                ctx.DependsOnArtifact(expressionPath);
            }

            expressionList.CubismExpressionObjects = expressionDataList.ToArray();
            expressionController.ExpressionsList = expressionList;
        }
    }
}
