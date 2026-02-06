/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Core;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// ScriptedImporter for moc3 assets.
    /// </summary>
    [ScriptedImporter(1, "moc3", CubismImporterPriorities.MocImporter)]
    public sealed class CubismMocImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ctx.assetPath));
            var bytes = File.ReadAllBytes(fullPath);
            var moc = CubismMoc.CreateFrom(bytes);
            moc.name = name;
            ctx.AddObjectToAsset("moc_data", moc);
            ctx.SetMainObject(moc);
        }
    }
}
