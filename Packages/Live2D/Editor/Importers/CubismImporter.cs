﻿/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using Packages.Live2D.Editor.Importers.New;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// Helper functionality for <see cref="ICubismImporter"/>s.
    /// </summary>
    public static class CubismImporter
    {
        #region Delegates

        /// <summary>
        /// Callback for textures used by Cubism model on <see cref="CubismModel"/> import.
        /// </summary>
        public delegate void TextureImportHandler(IModelImportContext ctx, CubismModel model, Texture2D texture);


        #endregion

        #region Events

        /// <summary>
        /// Allows customizing import of textures used by a Cubism model.
        /// </summary>
        /// <remarks>
        /// Set <see langword="null"/> in case you don't want Cubism model texture importing to be customized from script.
        /// </remarks>
        public static TextureImportHandler OnDidImportTexture = BuiltinTextureImportHandler;


        /// <summary>
        /// Material picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.MaterialPicker OnPickMaterial = CubismBuiltinPickers.MaterialPicker;

        /// <summary>
        /// Texture picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.TexturePicker OnPickTexture = CubismBuiltinPickers.TexturePicker;

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
        /// Safely triggers <see cref="OnDidImportModelTexture"/>
        /// </summary>
        /// <param name="importer">Importer.</param>
        /// <param name="model">Imported model.</param>
        /// <param name="texture">Imported texture.</param>
        internal static void SendModelTextureImportEvent(IModelImportContext ctx, CubismModel model, Texture2D texture)
        {
            if (OnDidImportTexture == null)
            {
                return;
            }


            OnDidImportTexture(ctx, model, texture);
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
        private static void BuiltinTextureImportHandler(IModelImportContext ctx, CubismModel model, Texture2D texture)
        {
            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;


            // Return early if texture already seems to be set up.
            if (textureImporter.alphaIsTransparency)
            {
                return;
            }


            // Set up texture importing.
            textureImporter.alphaIsTransparency = true;
            textureImporter.textureType = TextureImporterType.Default;


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
