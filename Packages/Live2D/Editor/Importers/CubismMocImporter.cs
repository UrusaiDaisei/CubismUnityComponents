using Live2D.Cubism.Core;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Rendering.Masking;
using Packages.Live2D.Editor.Importers.New;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Packages.Live2D.Editor.Importers
{
    [ScriptedImporter(1, "moc3",CubismImporterPriorities.MocImporter)]
    public sealed class CubismMocImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            var bytes = File.ReadAllBytes(ctx.assetPath);
            var moc = CubismMoc.CreateFrom(bytes);
            moc.name = name;
            ctx.AddObjectToAsset("moc_data", moc);

            //var model = CubismModel.LoadFromMoc(
            //    moc,
            //    (_,drawable)=>CubismBuiltinPickers.SelectDrawableMaterial(drawable),
            //    (_, drawable) => null,
            //    ShouldImportAsOriginalWorkflow
            //);

            //ctx.AddObjectToAsset("prefab", model.gameObject);
            ctx.SetMainObject(moc);
        }
    }
}
