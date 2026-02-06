/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

namespace Live2D.Cubism.Editor.Importers
{
    /// <summary>
    /// ScriptedImporter callback order for Cubism assets.
    /// </summary>
    internal static class CubismImporterPriorities
    {
        public const int MocImporter = 0;
        public const int Expression3JsonImporter = 90;
        public const int Model3JsonImporter = 100;
        public const int Motion3JsonImporter = 110;
        public const int MotionFadeListImporter = 120;
    }
}
