using Live2D.Cubism.Core;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Rendering.Masking;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Packages.Live2D.Editor.Importers
{
    [ScriptedImporter(1, "moc3", CubismImporterPriorities.MocImporter)]
    public sealed class CubismMocImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var bytes = File.ReadAllBytes(ctx.assetPath);
            var moc = CubismMoc.CreateFrom(bytes);
            moc.name = name;
            ctx.AddObjectToAsset("moc_data", moc);
            ctx.SetMainObject(moc);
        }
    }
}
