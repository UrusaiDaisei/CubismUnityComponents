/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

// Framework-level imports
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Runtime.CompilerServices;

// Cubism Core imports
using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering;
using Live2D.Cubism.Rendering.Masking;

// Cubism Framework imports
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.MotionFade;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Physics;
using Live2D.Cubism.Framework.Pose;
using Live2D.Cubism.Framework.Raycasting;
using Live2D.Cubism.Framework.UserData;

namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Exposes moc3.json asset data.
    /// </summary>
    [Serializable]
    // ReSharper disable once ClassCannotBeInstantiated
    public sealed class CubismModel3Json
    {
        #region Delegates

        /// <summary>
        /// Handles the loading of assets.
        /// </summary>
        /// <param name="assetType">The asset type to load.</param>
        /// <param name="assetPath">The path to the asset.</param>
        /// <returns></returns>
        public delegate object LoadAssetAtPathHandler(Type assetType, string assetPath);


        /// <summary>
        /// Picks a <see cref="Material"/> for a <see cref="CubismDrawable"/>.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to pick for.</param>
        /// <returns>Picked material.</returns>
        public delegate Material MaterialPicker(CubismModel3Json sender, CubismDrawable drawable);

        /// <summary>
        /// Picks a <see cref="Texture2D"/> for a <see cref="CubismDrawable"/>.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to pick for.</param>
        /// <returns>Picked texture.</returns>
        public delegate Texture2D TexturePicker(CubismModel3Json sender, CubismDrawable drawable);

        #endregion

        #region Load Methods

        /// <summary>
        /// Loads a model.json asset.
        /// </summary>
        /// <param name="assetPath">The path to the asset.</param>
        /// <returns>The <see cref="CubismModel3Json"/> on success; <see langword="null"/> otherwise.</returns>
        public static CubismModel3Json LoadAtPath(string assetPath)
        {
            // Use default asset load handler.
            return LoadAtPath(assetPath, GetBuiltinLoadAssetAtPath());
        }

        /// <summary>
        /// Loads a model.json asset.
        /// </summary>
        /// <param name="assetPath">The path to the asset.</param>
        /// <param name="loadAssetAtPath">Handler for loading assets.</param>
        /// <returns>The <see cref="CubismModel3Json"/> on success; <see langword="null"/> otherwise.</returns>
        public static CubismModel3Json LoadAtPath(string assetPath, LoadAssetAtPathHandler loadAssetAtPath)
        {
            // Load Json asset.
            var modelJsonAsset = loadAssetAtPath(typeof(string), assetPath) as string;

            // Return early in case Json asset wasn't loaded.
            if (modelJsonAsset == null)
            {
                return null;
            }


            // Deserialize Json.
            var modelJson = JsonUtility.FromJson<CubismModel3Json>(modelJsonAsset);


            // Finalize deserialization.
            modelJson.AssetPath = assetPath;
            modelJson.LoadAssetAtPath = loadAssetAtPath;


            // Set motion references.
            var value = CubismJsonParser.ParseFromString(modelJsonAsset);

            // Return early if there is no references.
            if (!value.Get("FileReferences").GetMap(null).ContainsKey("Motions"))
            {
                return modelJson;
            }


            var motionGroupNames = value.Get("FileReferences").Get("Motions").KeySet().ToArray();
            modelJson.FileReferences.Motions.GroupNames = motionGroupNames;

            var motionGroupNamesCount = motionGroupNames.Length;
            modelJson.FileReferences.Motions.Motions = new SerializableMotion[motionGroupNamesCount][];

            for (var i = 0; i < motionGroupNamesCount; i++)
            {
                var motionGroup = value.Get("FileReferences").Get("Motions").Get(motionGroupNames[i]);
                var motionCount = motionGroup.GetVector(null).ToArray().Length;

                modelJson.FileReferences.Motions.Motions[i] = new SerializableMotion[motionCount];


                var fadeInTime = -1.0f;
                var fadeOutTime = -1.0f;
                for (var j = 0; j < motionCount; j++)
                {
                    // Reset fade time cache.
                    fadeInTime = -1.0f;
                    fadeOutTime = -1.0f;

                    if (motionGroup.Get(j).GetMap(null).ContainsKey("File"))
                    {
                        modelJson.FileReferences.Motions.Motions[i][j].File = motionGroup.Get(j).Get("File").toString();
                    }

                    if (motionGroup.Get(j).GetMap(null).ContainsKey("Sound"))
                    {
                        modelJson.FileReferences.Motions.Motions[i][j].Sound = motionGroup.Get(j).Get("Sound").toString();
                    }

                    if (motionGroup.Get(j).GetMap(null).ContainsKey("FadeInTime"))
                    {
                        fadeInTime = motionGroup.Get(j).Get("FadeInTime").ToFloat();
                    }
                    modelJson.FileReferences.Motions.Motions[i][j].FadeInTime = fadeInTime;

                    if (motionGroup.Get(j).GetMap(null).ContainsKey("FadeOutTime"))
                    {
                        fadeOutTime = motionGroup.Get(j).Get("FadeOutTime").ToFloat();
                    }
                    modelJson.FileReferences.Motions.Motions[i][j].FadeOutTime = fadeOutTime;
                }
            }


            return modelJson;
        }

        #endregion

        /// <summary>
        /// Path to <see langword="this"/>.
        /// </summary>
        public string AssetPath { get; private set; }


        /// <summary>
        /// Method for loading assets.
        /// </summary>
        private LoadAssetAtPathHandler LoadAssetAtPath { get; set; }

        #region Json Data

        /// <summary>
        /// The motion3.json format version.
        /// </summary>
        [SerializeField]
        public int Version;

        /// <summary>
        /// The file references.
        /// </summary>
        [SerializeField]
        public SerializableFileReferences FileReferences;

        /// <summary>
        /// Groups.
        /// </summary>
        [SerializeField]
        public SerializableGroup[] Groups;

        /// <summary>
        /// Hit areas.
        /// </summary>
        [SerializeField]
        public SerializableHitArea[] HitAreas;

        #endregion

        /// <summary>
        /// <see cref="CubismPose3Json"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismPose3Json _pose3Json;

        /// <summary>
        /// The contents of pose3.json asset.
        /// </summary>
        public CubismPose3Json Pose3Json
        {
            get
            {
                if (_pose3Json != null)
                {
                    return _pose3Json;
                }

                var jsonString = string.IsNullOrEmpty(FileReferences.Pose) ? null : LoadReferencedAsset<String>(FileReferences.Pose);
                _pose3Json = CubismPose3Json.LoadFrom(jsonString);
                return _pose3Json;
            }
        }

        /// <summary>
        /// <see cref="Expression3Jsons"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismExp3Json[] _expression3Jsons;

        /// <summary>
        /// The referenced expression assets.
        /// </summary>
        /// <remarks>
        /// The references aren't cached internally.
        /// </remarks>
        public CubismExp3Json[] Expression3Jsons
        {
            get
            {
                // Fail silently...
                if (FileReferences.Expressions == null)
                {
                    return null;
                }

                // Load expression only if necessary.
                if (_expression3Jsons == null)
                {
                    _expression3Jsons = new CubismExp3Json[FileReferences.Expressions.Length];

                    for (var i = 0; i < _expression3Jsons.Length; ++i)
                    {
                        var expressionJson = (string.IsNullOrEmpty(FileReferences.Expressions[i].File))
                                                ? null
                                                : LoadReferencedAsset<string>(FileReferences.Expressions[i].File);
                        _expression3Jsons[i] = CubismExp3Json.LoadFrom(expressionJson);
                    }
                }

                return _expression3Jsons;
            }
        }

        /// <summary>
        /// The contents of physics3.json asset.
        /// </summary>
        public string Physics3Json
        {
            get
            {
                return string.IsNullOrEmpty(FileReferences.Physics) ? null : LoadReferencedAsset<string>(FileReferences.Physics);
            }
        }

        public string UserData3Json
        {
            get
            {
                return string.IsNullOrEmpty(FileReferences.UserData) ? null : LoadReferencedAsset<string>(FileReferences.UserData);
            }
        }

        /// <summary>
        /// The contents of cdi3.json asset.
        /// </summary>
        public string DisplayInfo3Json
        {
            get
            {
                return string.IsNullOrEmpty(FileReferences.DisplayInfo) ? null : LoadReferencedAsset<string>(FileReferences.DisplayInfo);
            }
        }

        /// <summary>
        /// <see cref="Textures"/> backing field.
        /// </summary>
        [NonSerialized]
        private Texture2D[] _textures;

        /// <summary>
        /// The referenced texture assets.
        /// </summary>
        /// <remarks>
        /// The references aren't cached internally.
        /// </remarks>
        public Texture2D[] Textures
        {
            get
            {
                // Load textures only if necessary.
                if (_textures == null)
                {
                    _textures = new Texture2D[FileReferences.Textures.Length];


                    for (var i = 0; i < _textures.Length; ++i)
                    {
                        _textures[i] = LoadReferencedAsset<Texture2D>(FileReferences.Textures[i]);
                    }
                }


                return _textures;
            }
        }

        #region Constructors

        /// <summary>
        /// Makes construction only possible through factories.
        /// </summary>
        private CubismModel3Json()
        {
        }

        #endregion

        /// <summary>
        /// Instantiates a <see cref="CubismMoc">model source</see> and a <see cref="CubismModel">model</see>.
        /// </summary>
        /// <param name="pickMaterial">The material mapper to use.</param>
        /// <param name="pickTexture">The texture mapper to use.</param>
        /// <param name="shouldImportAsOriginalWorkflow">Should import as original workflow.</param>
        /// <returns>The instantiated <see cref="CubismModel">model</see> on success; <see langword="null"/> otherwise.</returns>
        public CubismModel ToModel(CubismMoc moc, MaterialPicker pickMaterial, TexturePicker pickTexture, bool shouldImportAsOriginalWorkflow)
        {
            var model = CreateAndInitializeModel(moc);
            if (model == null) return null;

            // Initialize components in the correct order
            InitializeRenderers(model, pickMaterial, pickTexture);
            InitializeDisplayInfo(model);  // Display info needs to be initialized before parameters
            InitializeParameters(model);   // Parameters depend on display info
            InitializeParts(model);
            InitializeHitAreas(model);
            InitializePhysics(model);
            InitializeUserData(model);

            if (shouldImportAsOriginalWorkflow)
            {
                InitializeOriginalWorkflow(model);
            }

            model.gameObject.GetOrAddComponent<Animator>();
            model.ForceUpdateNow();

            return model;
        }

        private CubismModel CreateAndInitializeModel(CubismMoc moc)
        {
            var model = CubismModel.InstantiateFrom(moc);
            if (model == null) return null;

            model.name = Path.GetFileNameWithoutExtension(FileReferences.Moc);

#if UNITY_EDITOR
            model.gameObject.AddComponent<CubismParametersInspector>();
            model.gameObject.AddComponent<CubismPartsInspector>();
#endif

            return model;
        }

        private void InitializeRenderers(CubismModel model, MaterialPicker pickMaterial, TexturePicker pickTexture)
        {
            var rendererController = model.gameObject.AddComponent<CubismRenderController>();
            var renderers = rendererController.Renderers;
            var drawables = model.Drawables;

            if (renderers == null || drawables == null) return;

            for (var i = 0; i < renderers.Length; ++i)
            {
                renderers[i].Material = pickMaterial(this, drawables[i]);
                renderers[i].MainTexture = pickTexture(this, drawables[i]);
            }
        }

        private void InitializeParts(CubismModel model)
        {
            var parts = model.Parts;
            if (parts == null) return;

            for (int i = 0; i < parts.Length; i++)
            {
                var partColorsEditor = parts[i].gameObject.AddComponent<CubismPartColorsEditor>();
                partColorsEditor.TryInitialize(model);
            }
        }

        private void InitializeHitAreas(CubismModel model)
        {
            if (HitAreas == null) return;

            var drawables = model.Drawables;
            foreach (var hitArea in HitAreas)
            {
                var drawable = Array.Find(drawables, d => d.Id == hitArea.Id);
                if (drawable == null) continue;

                var hitDrawable = drawable.gameObject.AddComponent<CubismHitDrawable>();
                hitDrawable.Name = hitArea.Name;
                drawable.gameObject.AddComponent<CubismRaycastable>();
            }
        }

        private void InitializeDisplayInfo(CubismModel model)
        {
            var cdi3Json = CubismDisplayInfo3Json.LoadFrom(DisplayInfo3Json);
            if (cdi3Json == null) return;

            // Initialize parts display info
            foreach (var part in model.Parts)
            {
                var displayInfo = part.gameObject.AddComponent<CubismDisplayInfoPartName>();
                displayInfo.Name = part.Id;
                displayInfo.DisplayName = string.Empty;

                var foundPart = Array.Find(cdi3Json.Parts, p => p.Id == part.Id);
                if (foundPart.Id != null)
                {
                    displayInfo.DisplayName = foundPart.Name;
                }
            }

            // Initialize parameter display info
            foreach (var parameter in model.Parameters)
            {
                var displayInfo = parameter.gameObject.AddComponent<CubismDisplayInfoParameterName>();
                displayInfo.Name = parameter.Id;
                displayInfo.DisplayName = string.Empty;

                var foundParameter = Array.Find(cdi3Json.Parameters, p => p.Id == parameter.Id);
                if (foundParameter.Id != null)
                {
                    displayInfo.DisplayName = foundParameter.Name;
                }
            }

            // Initialize combined parameters
            var combinedParameters = cdi3Json.CombinedParameters;
            if (combinedParameters != null)
            {
                const int combinedParameterCount = 2;
                var combinedParameterInfo = model.gameObject.AddComponent<CubismDisplayInfoCombinedParameterInfo>();
                combinedParameterInfo.CombinedParameters = new CubismDisplayInfo3Json.CombinedParameter[combinedParameters.Length];

                for (var i = 0; i < combinedParameters.Length; i++)
                {
                    if (combinedParameters[i].Ids == null || combinedParameters[i].Ids.Length != combinedParameterCount)
                    {
                        Debug.LogWarning($"The data contains invalid CombinedParameters in {model.Moc.name}.cdi3.json.");
                        continue;
                    }

                    combinedParameterInfo.CombinedParameters[i] = new CubismDisplayInfo3Json.CombinedParameter
                    {
                        HorizontalParameterId = combinedParameters[i].Ids[0],
                        VerticalParameterId = combinedParameters[i].Ids[1]
                    };
                }
            }
        }

        private void InitializeParameters(CubismModel model)
        {
            if (model == null) return;

            var cdi3Json = CubismDisplayInfo3Json.LoadFrom(DisplayInfo3Json);
            var parameters = model.Parameters;
            if (parameters == null) return;

            // Initialize special parameter types first
            foreach (var parameter in parameters)
            {
                if (parameter == null) continue;

                // Initialize special parameter types
                if (IsParameterInGroup(parameter, "EyeBlink"))
                {
                    model.gameObject.GetOrAddComponent<CubismEyeBlinkController>();
                    parameter.gameObject.AddComponent<CubismEyeBlinkParameter>();
                }

                if (IsParameterInGroup(parameter, "LipSync"))
                {
                    model.gameObject.GetOrAddComponent<CubismMouthController>();
                    parameter.gameObject.AddComponent<CubismMouthParameter>();
                }

                // Ensure each parameter has a display info component
                if (parameter.gameObject.GetComponent<CubismDisplayInfoParameterName>() == null)
                {
                    var displayInfo = parameter.gameObject.AddComponent<CubismDisplayInfoParameterName>();
                    displayInfo.Name = parameter.Id;
                    displayInfo.DisplayName = string.Empty;
                    displayInfo.GroupName = string.Empty;
                }
            }

            // Initialize parameter groups if display info is available
            if (cdi3Json != null)
            {
                SetupParametersAndGroups(model, cdi3Json);
            }
        }

        private void InitializePhysics(CubismModel model)
        {
            var physics3JsonString = Physics3Json;
            if (string.IsNullOrEmpty(physics3JsonString)) return;

            var physics3Json = CubismPhysics3Json.LoadFrom(physics3JsonString);
            var physicsController = model.gameObject.GetOrAddComponent<CubismPhysicsController>();
            physicsController.Initialize(physics3Json.ToRig());
        }

        private void InitializeUserData(CubismModel model)
        {
            var userData3JsonString = UserData3Json;
            if (string.IsNullOrEmpty(userData3JsonString)) return;

            var userData3Json = CubismUserData3Json.LoadFrom(userData3JsonString);
            var drawableBodies = userData3Json.ToBodyArray(CubismUserDataTargetType.ArtMesh);

            foreach (var drawable in model.Drawables)
            {
                var index = GetBodyIndexById(drawableBodies, drawable.Id);
                if (index < 0) continue;

                var tag = drawable.gameObject.GetOrAddComponent<CubismUserDataTag>();
                tag.Initialize(drawableBodies[index]);
            }
        }

        private void InitializeOriginalWorkflow(CubismModel model)
        {
            model.gameObject.GetOrAddComponent<CubismUpdateController>();
            model.gameObject.GetOrAddComponent<CubismParameterStore>();
            model.gameObject.GetOrAddComponent<CubismPoseController>();
            model.gameObject.GetOrAddComponent<CubismExpressionController>();
            model.gameObject.GetOrAddComponent<CubismFadeController>();
        }

        #region Helper Methods

        /// <summary>
        /// Type-safely loads an asset.
        /// </summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="referencedFile">Path to asset.</param>
        /// <returns>The asset on success; <see langword="null"/> otherwise.</returns>
        private T LoadReferencedAsset<T>(string referencedFile) where T : class
        {
            var assetPath = Path.GetDirectoryName(AssetPath) + "/" + referencedFile;


            return LoadAssetAtPath(typeof(T), assetPath) as T;
        }


        /// <summary>
        /// Builtin method for loading assets based on the current Unity environment.
        /// </summary>
        /// <param name="assetType">Asset type.</param>
        /// <param name="assetPath">Path to asset.</param>
        /// <returns>The asset on success; <see langword="null"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LoadAssetAtPathHandler GetBuiltinLoadAssetAtPath()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return LoadAssetInEditor;
            }
#endif
            return LoadAssetInRuntime;

#if UNITY_EDITOR

            static object LoadAssetInEditor(Type assetType, string assetPath)
            {
                // Handle raw file types
                if (assetType == typeof(byte[]))
                {
                    return File.ReadAllBytes(assetPath);
                }

                if (assetType == typeof(string))
                {
                    return File.ReadAllText(assetPath);
                }

                // Handle Unity assets
                return AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            }

#endif

            static object LoadAssetInRuntime(Type assetType, string assetPath)
            {
                // Handle text assets
                if (assetType == typeof(byte[]) || assetType == typeof(string))
                {
                    var textAsset = Resources.Load(assetPath, typeof(TextAsset)) as TextAsset;
                    if (textAsset == null) return null;

                    return assetType == typeof(byte[]) ? textAsset.bytes : textAsset.text;
                }

                // Handle Unity assets
                return Resources.Load(assetPath, assetType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsParameterInGroup(CubismParameter parameter, string groupName)
        {
            if (Groups == null || Groups.Length == 0) return false;

            var group = Array.Find(Groups, g => g.Name == groupName);
            if (group.Ids == null) return false;

            return Array.Exists(group.Ids, id => id == parameter.name);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBodyIndexById(CubismUserDataBody[] bodies, string id)
        {
            return Array.FindIndex(bodies, body => body.Id == id);
        }


        #endregion

        #region Json Helpers

        /// <summary>
        /// File references data.
        /// </summary>
        [Serializable]
        public struct SerializableFileReferences
        {
            /// <summary>
            /// Relative path to the moc3 asset.
            /// </summary>
            [SerializeField]
            public string Moc;

            /// <summary>
            /// Relative paths to texture assets.
            /// </summary>
            [SerializeField]
            public string[] Textures;

            /// <summary>
            /// Relative path to the pose3.json.
            /// </summary>
            [SerializeField]
            public string Pose;

            /// <summary>
            /// Relative path to the expression asset.
            /// </summary>
            [SerializeField]
            public SerializableExpression[] Expressions;

            /// <summary>
            /// Relative path to the pose motion3.json.
            /// </summary>
            [SerializeField]
            public SerializableMotions Motions;

            /// <summary>
            /// Relative path to the physics asset.
            /// </summary>
            [SerializeField]
            public string Physics;

            /// <summary>
            /// Relative path to the user data asset.
            /// </summary>
            [SerializeField]
            public string UserData;

            /// <summary>
            /// Relative path to the cdi3.json.
            /// </summary>
            [SerializeField]
            public string DisplayInfo;
        }

        /// <summary>
        /// Group data.
        /// </summary>
        [Serializable]
        public struct SerializableGroup
        {
            /// <summary>
            /// Target type.
            /// </summary>
            [SerializeField]
            public string Target;

            /// <summary>
            /// Group name.
            /// </summary>
            [SerializeField]
            public string Name;

            /// <summary>
            /// Referenced IDs.
            /// </summary>
            [SerializeField]
            public string[] Ids;
        }

        /// <summary>
        /// Expression data.
        /// </summary>
        [Serializable]
        public struct SerializableExpression
        {
            /// <summary>
            /// Expression Name.
            /// </summary>
            [SerializeField]
            public string Name;

            /// <summary>
            /// Expression File.
            /// </summary>
            [SerializeField]
            public string File;

            /// <summary>
            /// Expression FadeInTime.
            /// </summary>
            [SerializeField]
            public float FadeInTime;

            /// <summary>
            /// Expression FadeOutTime.
            /// </summary>
            [SerializeField]
            public float FadeOutTime;
        }

        /// <summary>
        /// Motion data.
        /// </summary>
        [Serializable]
        public struct SerializableMotions
        {
            /// <summary>
            /// Motion group names.
            /// </summary>
            [SerializeField]
            public string[] GroupNames;

            /// <summary>
            /// Motion groups.
            /// </summary>
            [SerializeField]
            public SerializableMotion[][] Motions;
        }

        /// <summary>
        /// Motion data.
        /// </summary>
        [Serializable]
        public struct SerializableMotion
        {
            /// <summary>
            /// File path.
            /// </summary>
            [SerializeField]
            public string File;

            /// <summary>
            /// Sound path.
            /// </summary>
            [SerializeField]
            public string Sound;

            /// <summary>
            /// Fade in time.
            /// </summary>
            [SerializeField]
            public float FadeInTime;

            /// <summary>
            /// Fade out time.
            /// </summary>
            [SerializeField]
            public float FadeOutTime;
        }

        /// <summary>
        /// Hit Area.
        /// </summary>
        [Serializable]
        public struct SerializableHitArea
        {
            /// <summary>
            /// Hit area name.
            /// </summary>
            [SerializeField]
            public string Name;

            /// <summary>
            /// Hit area id.
            /// </summary>
            [SerializeField]
            public string Id;
        }

        #endregion

        private void SetupParametersAndGroups(CubismModel model, CubismDisplayInfo3Json cdi3Json)
        {
            if (model == null || cdi3Json == null) return;

            var parameters = model.Parameters;
            if (parameters == null) return;

            // Find or create Parameters container
            var parametersContainer = model.transform.Find("Parameters")?.gameObject;
            if (parametersContainer == null)
            {
                parametersContainer = new GameObject("Parameters");
                parametersContainer.transform.SetParent(model.transform);
            }

            var groupsManager = parametersContainer.GetComponent<CubismParameterGroups>()
                ?? parametersContainer.AddComponent<CubismParameterGroups>();

            var groups = new Dictionary<string, (GameObject obj, List<CubismDisplayInfoParameterName> parameters)>();

            // First pass: Create parameter components and group containers
            foreach (var parameter in parameters)
            {
                if (parameter == null) continue;

                var displayInfo = parameter.GetComponent<CubismDisplayInfoParameterName>();
                if (displayInfo == null) continue;

                var paramInfo = Array.Find(cdi3Json.Parameters, p => p.Id == parameter.Id);
                if (paramInfo.Id == null) continue;

                // Find group info
                var groupInfo = Array.Find(cdi3Json.ParameterGroups, g => g.Id == paramInfo.GroupId);
                if (groupInfo.Id == null) continue;

                // Create group container if needed
                if (!groups.ContainsKey(groupInfo.Id))
                {
                    var groupObject = new GameObject(groupInfo.Name);
                    groupObject.transform.SetParent(parametersContainer.transform);
                    groups[groupInfo.Id] = (groupObject, new List<CubismDisplayInfoParameterName>());
                }

                // Update parameter info
                displayInfo.GroupName = groupInfo.Name;
                groups[groupInfo.Id].parameters.Add(displayInfo);
            }

            // Set up groups data
            if (groups.Any())
            {
                groupsManager.Groups = groups.Select(kvp => new CubismParameterGroups.ParameterGroup
                {
                    Id = kvp.Key,
                    Name = kvp.Value.obj.name,
                    Parameters = kvp.Value.parameters.ToArray()
                }).ToArray();

                // Organize parameters
                foreach (var parameter in parameters)
                {
                    if (parameter == null) continue;

                    var displayInfo = parameter.GetComponent<CubismDisplayInfoParameterName>();
                    if (displayInfo == null) continue;

                    if (string.IsNullOrEmpty(displayInfo.GroupName))
                    {
                        parameter.transform.SetParent(parametersContainer.transform, false);
                        continue;
                    }

                    var group = groups.FirstOrDefault(g => g.Value.obj.name == displayInfo.GroupName);
                    if (group.Value.obj == null) continue;

                    parameter.transform.SetParent(group.Value.obj.transform, false);
                }
            }
        }
    }
}
