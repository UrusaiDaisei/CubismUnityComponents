using Live2D.Cubism.Core;
using System.IO;
using UnityEditor.AssetImporters;

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
