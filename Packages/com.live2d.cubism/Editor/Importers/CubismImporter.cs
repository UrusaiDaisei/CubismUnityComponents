/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using Packages.Live2D.Editor.Importers.New;
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
        public static CubismModel3Json.DrawableMaterialPicker OnPickDrawableMaterial = CubismBuiltinPickers.DrawableMaterialPicker;

        /// <summary>
        /// Texture picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.TexturePicker OnPickTexture = CubismBuiltinPickers.TexturePicker;

        /// <summary>
        /// Offscreen material picker to use when importing models.
        /// </summary>
        public static CubismModel3Json.OffscreenMaterialPicker OnPickOffscreenMaterial = CubismBuiltinPickers.OffscreenMaterialPicker;

        #endregion

        /// <summary>
        /// Enables logging of import events.
        /// </summary>
        public static bool LogImportEvents = true;

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
    }
}
