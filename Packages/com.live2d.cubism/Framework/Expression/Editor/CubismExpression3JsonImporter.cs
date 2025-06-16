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
using UnityEditor;
using UnityEngine;
using Packages.Live2D.Editor.Importers.New;
using Live2D.Cubism.Framework;
using UnityEditor.AssetImporters;
using Packages.Live2D.Editor.Importers;

namespace Live2D.Cubism.Editor.Importers
{
    [ScriptedImporter(1, "exp3.json", CubismImporterPriorities.Expression3JsonImporter)]
    public sealed class CubismExpression3JsonImporter : ScriptedImporter
    {
        #region Unity Event Handling

        /// <summary>
        /// Registers importer.
        /// </summary>
        [InitializeOnLoadMethod]
        // ReSharper disable once UnusedMember.Local
        private static void RegisterImporter()
        {
            CubismModel3JsonImporter.OnDidImportModel += OnModelImport;
        }

        #endregion

        #region Cubism Import Event Handling

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var parentDirectory = Path.GetDirectoryName(ctx.assetPath);
            var model3JsonPath = FindModel3JsonFile(parentDirectory);
            if (model3JsonPath == null)
            {
                ctx.LogImportError("unable to find model3json file in current directory or parent directories.");
                return;
            }

            var data = File.ReadAllText(ctx.assetPath);
            var ExpressionJson = CubismExp3Json.LoadFrom(data);

            // Create expression data.
            CubismExpressionData expressionData = CubismExpressionData.CreateInstance(ExpressionJson);
            ctx.AddObjectToAsset("expressionData", expressionData);
            ctx.SetMainObject(expressionData);
        }

        /// <summary>
        /// Searches for a model3.json file in the current directory and its parent directories.
        /// </summary>
        /// <param name="startDirectory">The directory to start searching from.</param>
        /// <returns>The path to the model3.json file, or null if not found.</returns>
        private string FindModel3JsonFile(string startDirectory)
        {
            var currentDirectory = startDirectory;

            while (currentDirectory != null)
            {
                var model3JsonPath = Directory.EnumerateFiles(currentDirectory, "*.model3.json").FirstOrDefault();
                if (model3JsonPath != null)
                {
                    return model3JsonPath;
                }

                // Move up to parent directory
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return null;
        }

        /// <summary>
        /// Set expression list.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="model">Imported model.</param>
        private static void OnModelImport(IModelImportContext ctx)
        {
            if (ctx.Model3Json.FileReferences.Expressions?.Any() != true)
            {
                return;
            }

            var expressionController = ctx.Model.gameObject.GetOrAddComponent<CubismExpressionController>();
            var expressionList = ScriptableObject.CreateInstance<CubismExpressionList>();
            expressionList.name = $"{ctx.ModelName}.expressionList";
            ctx.AddSubObject(expressionList);

            var directoryName = Path.GetDirectoryName(ctx.AssetPath);

            /*
            var expressionFiles = Directory.EnumerateFiles(directoryName, "*.exp3.json", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetFileName(path));
            */
            var expressionDataList = new List<CubismExpressionData>();
            foreach (var expression in ctx.Model3Json.FileReferences.Expressions)
            {
                var expressionPath = Path.Combine(directoryName, expression.File);
                if (!File.Exists(expressionPath))
                {
                    Debug.LogWarning($"Unable to find expression: {expression.File}");
                    continue;
                }

                var expressionData = AssetDatabase.LoadAssetAtPath<CubismExpressionData>(expressionPath);
                if (expressionData != null)
                {
                    expressionDataList.Add(expressionData);
                }
                else
                {
                    Debug.LogWarning($"Unable to load expression: {expressionPath}");
                }
                ctx.ImporterContext.DependsOnArtifact(expressionPath);
            }

            expressionList.CubismExpressionObjects = expressionDataList.ToArray();
            expressionController.ExpressionsList = expressionList;
        }

        #endregion

        /// <summary>
        /// Load the .expressionList.
        /// If it does not exist, create a new one.
        /// </summary>
        /// <param name="expressionListPath">The path of the .expressionList.asset relative to the project.</param>
        /// <returns>.expressionList.asset</returns>
        private static CubismExpressionList GetExpressionList(string expressionListPath)
        {
            var assetList = CubismCreatedAssetList.GetInstance();
            var assetListIndex = assetList.AssetPaths.Contains(expressionListPath)
                ? assetList.AssetPaths.IndexOf(expressionListPath)
                : -1;

            CubismExpressionList expressionList = null;

            if (assetListIndex < 0)
            {
                expressionList = AssetDatabase.LoadAssetAtPath<CubismExpressionList>(expressionListPath);

                if (expressionList == null)
                {
                    expressionList = ScriptableObject.CreateInstance<CubismExpressionList>();
                    expressionList.CubismExpressionObjects = new CubismExpressionData[0];
                    AssetDatabase.CreateAsset(expressionList, expressionListPath);
                }

                assetList.Assets.Add(expressionList);
                assetList.AssetPaths.Add(expressionListPath);
                assetList.IsImporterDirties.Add(true);
            }
            else
            {
                expressionList = (CubismExpressionList)assetList.Assets[assetListIndex];
            }

            return expressionList;
        }

        private class ExpressionEqualityComparer : IEqualityComparer<CubismExpressionData>
        {
            public bool Equals(CubismExpressionData x, CubismExpressionData y)
            {
                return x.name == y.name;
            }

            public int GetHashCode(CubismExpressionData obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
