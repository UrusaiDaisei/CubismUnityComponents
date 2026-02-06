/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// Context passed when a model is imported via the ScriptedImporter (model3.json).
    /// </summary>
    public interface IModelImportContext
    {
        /// <summary>Path of the model3.json asset.</summary>
        string AssetPath { get; }
        /// <summary>Model name derived from the asset path.</summary>
        string ModelName { get; }
        /// <summary>Loaded Model3Json data.</summary>
        CubismModel3Json Model3Json { get; }
        /// <summary>Imported Cubism model instance.</summary>
        CubismModel Model { get; }
        /// <summary>Add a sub-asset to the imported asset.</summary>
        void AddSubObject(UnityEngine.Object subObject);
        /// <summary>Register dependency on another source asset (e.g. reimport when that file changes).</summary>
        void DependsOnSourceAsset(string path);
        /// <summary>Register dependency on another asset's imported artifact.</summary>
        void DependsOnArtifact(string path);
    }

    /// <summary>
    /// Helper functionality for <see cref="ICubismImporter"/>s.
    /// </summary>
    public static class CubismImporter
    {
        #region Delegates

        /// <summary>
        /// Callback on <see cref="CubismModel"/> import.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="model">Imported model.</param>
        public delegate void ModelImportListener(CubismModel3JsonImporter importer, CubismModel model);


        /// <summary>
        /// Callback for textures used by Cubism model on <see cref="CubismModel"/> import.
        /// </summary>
        public delegate void TextureImportHandler(CubismModel3JsonImporter importer, CubismModel model, Texture2D texture);

        /// <summary>
        /// Callback for textures when using ScriptedImporter ( <see cref="IModelImportContext"/> ).
        /// </summary>
        public delegate void TextureImportHandlerContext(IModelImportContext context, CubismModel model, Texture2D texture);


        /// <summary>
        /// Callback on Cubism motions import as<see cref="AnimationClip"/>.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="animationClip">Generated animation.</param>
        public delegate void MotionImportHandler(CubismMotion3JsonImporter importer, AnimationClip animationClip);

        #endregion

        #region Events

        /// <summary>
        /// Allows getting called back whenever a model is imported (and before it is saved).
        /// </summary>
        public static event ModelImportListener OnDidImportModel;

        /// <summary>
        /// Allows customizing import of textures used by a Cubism model.
        /// </summary>
        /// <remarks>
        /// Set <see langword="null"/> in case you don't want Cubism model texture importing to be customized from script.
        /// </remarks>
        public static TextureImportHandler OnDidImportTexture = BuiltinTextureImportHandler;

        /// <summary>
        /// Texture import handler when using ScriptedImporter (model3.json). Used by <see cref="CubismModel3JsonImporter"/>.
        /// </summary>
        public static TextureImportHandlerContext OnDidImportTextureContext = BuiltinTextureImportHandlerContext;


        /// <summary>
        /// Material picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.DrawableMaterialPicker OnPickDrawableMaterial = CubismBuiltinPickers.DrawableMaterialPicker;

        /// <summary>
        /// Texture picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.TexturePicker OnPickTexture = CubismBuiltinPickers.TexturePicker;

        /// <summary>
        /// Offscreen material picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.OffscreenMaterialPicker OnPickOffscreenMaterial = CubismBuiltinPickers.OffscreenMaterialPicker;


        /// <summary>
        /// Allows getting called back whenever a Cubism motions is imported (and before it is saved).
        /// </summary>
        public static event MotionImportHandler OnDidImportMotion;

        #endregion

        /// <summary>
        /// Enables logging of import events.
        /// </summary>
        public static bool LogImportEvents = true;


        /// <summary>
        /// Tries to get an importer for a Cubism asset.
        /// </summary>
        /// <typeparam name="T">Importer type.</typeparam>
        /// <param name="assetPath">Path to the asset.</param>
        /// <returns>The importer on success; <see langword="null"/> otherwise.</returns>
        public static T GetImporterAtPath<T>(string assetPath) where T : class, ICubismImporter
        {
            return GetImporterAtPath(assetPath) as T;
        }

        /// <summary>
        /// Tries to deserialize an importer from <see cref="AssetImporter.userData"/>.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <returns>The importer on success; <see langword="null"/> otherwise.</returns>
        public static ICubismImporter GetImporterAtPath(string assetPath)
        {
            var importerEntry = _registry.Find(e => assetPath.EndsWith(e.FileExtension));


            // Return early in case no valid importer is registered.
            if (importerEntry.ImporterType == null)
            {
                return null;
            }


            var userData = AssetImporter
                .GetAtPath(assetPath)
                .userData;


            // Try to deserialize a importer from the user data.
            var importer = JsonUtility.FromJson(userData, importerEntry.ImporterType) as ICubismImporter;


            // Activate an instance in case Json deserialization magically fails...
            if (importer == null)
            {
                importer = Activator.CreateInstance(importerEntry.ImporterType) as ICubismImporter;
            }


            // Finalize importer initialization.
            if (importer != null)
            {
                importer.SetAssetPath(assetPath);
            }


            return importer;
        }


        /// <summary>
        /// Safely triggers <see cref="OnDidImportModel"/>.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="model">Imported model.</param>
        internal static void SendModelImportEvent(CubismModel3JsonImporter importer, CubismModel model)
        {
            if (OnDidImportModel == null)
            {
                return;
            }


            OnDidImportModel(importer, model);
        }

        /// <summary>
        /// Safely triggers texture import callback (legacy importer path).
        /// </summary>
        internal static void SendModelTextureImportEvent(CubismModel3JsonImporter importer, CubismModel model, Texture2D texture)
        {
            if (OnDidImportTexture == null)
            {
                return;
            }


            OnDidImportTexture(importer, model, texture);
        }

        /// <summary>
        /// Safely triggers texture import callback when using <see cref="IModelImportContext"/> (ScriptedImporter path).
        /// </summary>
        internal static void SendModelTextureImportEvent(IModelImportContext context, CubismModel model, Texture2D texture)
        {
            if (OnDidImportTextureContext == null)
            {
                return;
            }

            OnDidImportTextureContext(context, model, texture);
        }

        /// <summary>
        /// Safely triggers <see cref="OnDidImportMotion"/>.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="animationClip">Generated animation.</param>
        internal static void SendMotionImportEvent(CubismMotion3JsonImporter importer, AnimationClip animationClip)
        {
            if (OnDidImportMotion == null)
            {
                return;
            }


            OnDidImportMotion(importer, animationClip);
        }


        /// <summary>
        /// Logs a reimport event.
        /// </summary>
        /// <param name="sourceName">Source asset reimported.</param>
        /// <param name="destinationName">Destination asset updated.</param>
        internal static void LogReimport(string sourceName, string destinationName)
        {
            if (!LogImportEvents)
            {
                return;
            }


            Debug.LogFormat("[Cubism] Reimport: \"{0}\" was synced with \"{1}\".", destinationName, sourceName);
        }

        #region Builtin Texture Import Handler

        /// <summary>
        /// Makes sure textures used by Cubism models have the <see cref="TextureImporter.alphaIsTransparency"/> option enabled.
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="model">Imported model.</param>
        /// <param name="texture">Imported texture.</param>
        private static void BuiltinTextureImportHandler(CubismModel3JsonImporter importer, CubismModel model, Texture2D texture)
        {
            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

            if (!textureImporter)
            {
                Debug.LogError("[Texture Importer] Could not get TextureImporter for texture used by Cubism model.");
                return;
            }

            // Return early if texture already seems to be set up.
            if (!textureImporter.mipmapEnabled
                && textureImporter.alphaIsTransparency
                && textureImporter.textureType == TextureImporterType.Default
                && textureImporter.textureCompression == TextureImporterCompression.Uncompressed)
            {
                return;
            }


            // Set up texture importing.
            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = true;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;


            EditorUtility.SetDirty(texture);
            textureImporter.SaveAndReimport();
        }

        /// <summary>
        /// Default texture import handler when using <see cref="IModelImportContext"/> (ScriptedImporter).
        /// </summary>
        private static void BuiltinTextureImportHandlerContext(IModelImportContext context, CubismModel model, Texture2D texture)
        {
            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

            if (!textureImporter)
            {
                Debug.LogError("[Texture Importer] Could not get TextureImporter for texture used by Cubism model.");
                return;
            }

            if (!textureImporter.mipmapEnabled
                && textureImporter.alphaIsTransparency
                && textureImporter.textureType == TextureImporterType.Default
                && textureImporter.textureCompression == TextureImporterCompression.Uncompressed)
            {
                return;
            }

            textureImporter.mipmapEnabled = false;
            textureImporter.alphaIsTransparency = true;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;

            EditorUtility.SetDirty(texture);
            textureImporter.SaveAndReimport();
        }

        #endregion

        #region Registry

        /// <summary>
        /// Registry entry.
        /// </summary>
        private struct ImporterEntry
        {
            /// <summary>
            /// Importer type.
            /// </summary>
            public Type ImporterType;

            /// <summary>
            /// File extension valid for the importer.
            /// </summary>
            public string FileExtension;
        }


        /// <summary>
        /// List of registered <see cref="ICubismImporter"/>s.
        /// </summary>
        private static List<ImporterEntry> _registry = new List<ImporterEntry>();


        /// <summary>
        /// Registers an importer type.
        /// </summary>
        /// <typeparam name="T">The type of importer to register.</typeparam>
        /// <param name="fileExtension">The file extension the importer supports.</param>
        internal static void RegisterImporter<T>(string fileExtension) where T : ICubismImporter
        {
            _registry.Add(new ImporterEntry
            {
                ImporterType = typeof(T),
                FileExtension = fileExtension
            });
        }

        #endregion
    }
}
