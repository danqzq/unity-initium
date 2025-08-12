#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor.PackageManager.Requests;

namespace Danqzq.Initium
{
    public class Initium : EditorWindow
    {
        private const string EDITOR_PREFS_CONFIG_KEY = "InitiumConfig";
        private const string EDITOR_PREFS_AUTO_SAVE_KEY = "InitiumAutoSave";
        private const string EDITOR_PREFS_LOGGING_KEY = "InitiumLogging";
        
        private const string AUTHOR_URL = "https://www.danqzq.games";
        private const string VERSION = "1.0.0";
        
        private static readonly string[] BaseFolders = 
        {
            "Animations",
            "Audio",
            "Materials",
            "Plugins",
            "Prefabs",
            "Presets",
            "Resources",
            "Textures",
            "Scenes",
            "Scripts",
            "StreamingAssets"
        };

        private static FolderConfig[] _scriptsFolders;
        private static FolderConfig[] _audioFolders;
        private static HashSet<PackageConfig> _packages;
        private static HashSet<PackageFileConfig> _packageFiles;
        
        private static string _baseNamespace = "Game";
        private static string _configFileLoadOrSaveResult;
        
        private static bool _isAutoSaveEnabled;
        
        private static Vector2 _scrollPosition;
        
        private static AddRequest _addRequest;
        
        [System.Flags]
        private enum MenuTab
        {
            All = 0,
            Dependencies = 1 << 0,
            Settings = 1 << 1,
            Configs = 1 << 2,
        }

        private static MenuTab _activeTab = MenuTab.All;
        
        [MenuItem("Window/Danqzq/Initium")]
        public static void ShowWindow()
        {
            Initium window = GetWindow<Initium>("Initium");
            window.minSize = new Vector2(300, 800);
            window.maxSize = new Vector2(600, 1200);
            window.Show();
        }
        
        private static void Initialize()
        {
            _baseNamespace = "Game";
            _scriptsFolders = new[]
            {
                new FolderConfig("Editor", true),
                new FolderConfig("Runtime", true),
                new FolderConfig("Tests", true)
            };
            _audioFolders = new[]
            {
                new FolderConfig("Music", true),
                new FolderConfig("SFX", true),
                new FolderConfig("Voice", true)
            };
            _packages = new HashSet<PackageConfig>();
            _packageFiles = new HashSet<PackageFileConfig>();
        }

        private void OnEnable()
        {
            if (EditorPrefs.HasKey(EDITOR_PREFS_CONFIG_KEY))
            {
                LoadFromEditor();
            }
            else
            {
                Initialize();
            }

            if (EditorPrefs.HasKey(EDITOR_PREFS_LOGGING_KEY))
            {
                Logger.IsEnabled = EditorPrefs.GetBool(EDITOR_PREFS_LOGGING_KEY);
            }
            
            if (EditorPrefs.HasKey(EDITOR_PREFS_AUTO_SAVE_KEY))
            {
                _isAutoSaveEnabled = EditorPrefs.GetBool(EDITOR_PREFS_AUTO_SAVE_KEY);
            }
            else
            {
                _isAutoSaveEnabled = true;
                EditorPrefs.SetBool(EDITOR_PREFS_AUTO_SAVE_KEY, _isAutoSaveEnabled);
            }
        }

        #region GUI
        
        private void OnGUI()
        {
            Title();
            BaseMenu();
            DrawSeparator();
            var centeredTitleStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Customize", centeredTitleStyle);
            GUILayout.Space(10);
            DrawSeparator(0f, false);
            DrawTabs();
            DrawSeparator(0f, false);
            GUILayout.Space(10);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

            if ((_activeTab & MenuTab.Dependencies) != 0 || _activeTab == MenuTab.All)
            {
                DependenciesMenu();
            }
            if ((_activeTab & MenuTab.Settings) != 0 || _activeTab == MenuTab.All)
            {
                SettingsMenu();
            }
            if ((_activeTab & MenuTab.Configs) != 0 || _activeTab == MenuTab.All)
            {
                ConfigsMenu();
            }
            
            GUILayout.EndScrollView();
            DrawSeparator();
            
            CreditsMenu();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_activeTab == MenuTab.All, "All", EditorStyles.toolbarButton))
                _activeTab = MenuTab.All;
            if (GUILayout.Toggle(_activeTab == MenuTab.Dependencies, "Dependencies", EditorStyles.toolbarButton))
                _activeTab = MenuTab.Dependencies;
            if (GUILayout.Toggle(_activeTab == MenuTab.Settings, "Settings", EditorStyles.toolbarButton))
                _activeTab = MenuTab.Settings;
            if (GUILayout.Toggle(_activeTab == MenuTab.Configs, "Configs", EditorStyles.toolbarButton))
                _activeTab = MenuTab.Configs;
            GUILayout.EndHorizontal();
        }

        private static void Title()
        {
            var centeredTitleStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Welcome to Initium!", centeredTitleStyle);
        } 

        private void BaseMenu()
        {
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);

            var bigButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 18,
                fixedHeight = 48
            };

            if (GUILayout.Button("Initialize Project", bigButtonStyle))
            {
                InitializeProject();
            }
            
            GUI.backgroundColor = new Color(0.7f, 0.2f, 0.7f);
            if (GUILayout.Button("Fetch Dependencies", bigButtonStyle))
            {
                FetchDependencies();
            }

            GUI.backgroundColor = prevColor;
        }
        
        private static void DependenciesMenu()
        {
            GUILayout.Label("Dependencies", EditorStyles.centeredGreyMiniLabel);
            
            // Unity Registry
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Packages", EditorStyles.boldLabel);
            if (_packages.Count <= 0)
            {
                GUILayout.Label("No packages added yet.", GUILayout.ExpandWidth(false));
            }
            else
            {
                if (GUILayout.Button("Remove Selected", GUILayout.Width(120)))
                {
                    var toBeRemoved = _packages.Where(packageConfig => packageConfig.Include).ToList();
                    foreach (var packageConfig in toBeRemoved)
                    {
                        _packages.Remove(packageConfig);
                    }
                }

                if (GUILayout.Button("Remove All", GUILayout.Width(90)))
                {
                    _packages.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            foreach (var packageConfig in _packages)
            {
                GUILayout.BeginHorizontal();
                packageConfig.Include = EditorGUILayout.ToggleLeft(packageConfig.Name, packageConfig.Include);
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _packages.Remove(packageConfig);
                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Packages to List"))
            {
                PackageListWindow.ShowWindow(_packages.Select(p => p.Name));
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            // Custom Unity Packages
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Custom Unity Packages (.unitypackage)", EditorStyles.boldLabel);
            foreach (var packageFile in _packageFiles)
            {
                GUILayout.BeginHorizontal();
                packageFile.Include = EditorGUILayout.ToggleLeft(packageFile.Name, packageFile.Include);
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _packageFiles.Remove(packageFile);
                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Unity Package File Path"))
            {
                string path = EditorUtility.OpenFilePanel("Add Unity Package", "", "unitypackage");
                if (!string.IsNullOrEmpty(path))
                {
                    _packageFiles.Add(new PackageFileConfig(path, true));
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static void SettingsMenu()
        {
            GUILayout.Label("Settings", EditorStyles.centeredGreyMiniLabel);

            // Scripts section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Scripts Folders & Assembly Definitions", EditorStyles.boldLabel);
            foreach (var folderConfig in _scriptsFolders)
            {
                folderConfig.Include = EditorGUILayout.ToggleLeft(folderConfig.Name, folderConfig.Include);
            }
            GUILayout.Space(6);
            GUILayout.Label("Base Namespace", EditorStyles.miniLabel);
            _baseNamespace = EditorGUILayout.TextField(_baseNamespace, EditorStyles.textField);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Audio section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Audio Folders", EditorStyles.boldLabel);
            foreach (var folderConfig in _audioFolders)
            {
                folderConfig.Include = EditorGUILayout.ToggleLeft(folderConfig.Name, folderConfig.Include);
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Other settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Other Settings", EditorStyles.boldLabel);
            var prevAutoSave = _isAutoSaveEnabled;
            _isAutoSaveEnabled = EditorGUILayout.ToggleLeft("Auto-Save Config to Editor",
                _isAutoSaveEnabled);
            if (prevAutoSave != _isAutoSaveEnabled)
            {
                EditorPrefs.SetBool(EDITOR_PREFS_AUTO_SAVE_KEY, _isAutoSaveEnabled);
                if (_isAutoSaveEnabled)
                {
                    SaveToEditor();
                }
            }
            
            var prevLogging = Logger.IsEnabled;
            Logger.IsEnabled = EditorGUILayout.ToggleLeft("Enable Logging", Logger.IsEnabled);
            if (prevLogging != Logger.IsEnabled)
            {
                EditorPrefs.SetBool(EDITOR_PREFS_LOGGING_KEY, Logger.IsEnabled);
            }
            EditorGUILayout.EndVertical();
        }
        
        private static void ConfigsMenu()
        {
            GUILayout.Label("Configuration", EditorStyles.centeredGreyMiniLabel);
            
            // Editor section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("In-Editor Config", EditorStyles.boldLabel);
            var isFoundInEditor = EditorPrefs.HasKey(EDITOR_PREFS_CONFIG_KEY);
            var noticeLabelText = $"{(isFoundInEditor ? "Loaded config from" : "Not saved in")} editor preferences.";
            GUILayout.Label(noticeLabelText, EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save To Editor"))
            {
                SaveToEditor();
            }
            if (GUILayout.Button("Reset"))
            {
                EditorPrefs.DeleteKey(EDITOR_PREFS_CONFIG_KEY);
                Initialize();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            
            GUILayout.Space(6);
            
            // File section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("File Config", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(_configFileLoadOrSaveResult))
            {
                GUILayout.Label(_configFileLoadOrSaveResult, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import"))
            {
                var configFilePath = EditorUtility.OpenFilePanel("Select Initium Config File", "", "json");
                if (!string.IsNullOrEmpty(configFilePath))
                {
                    LoadConfigFile(configFilePath);
                }
            }
            if (GUILayout.Button("Export"))
            {
                var savePath = EditorUtility.SaveFilePanel("Save Initium Config File", "", "initium_config.json", "json");
                if (!string.IsNullOrEmpty(savePath))
                {
                    SaveConfigFile(savePath);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void CreditsMenu()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            var urlStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
            };

            var buttonRect = GUILayoutUtility.GetRect(new GUIContent("Made by @danqzq"), urlStyle);

            var isHover = buttonRect.Contains(Event.current.mousePosition);
            var prevColor = GUI.color;
            GUI.color = isHover ? new Color(0.3f, 0.9f, 1f) : new Color(0.3f, 0.7f, 0.9f);

            if (GUI.Button(buttonRect, "<u>Made by @danqzq</u>", urlStyle))
            {
                Application.OpenURL(AUTHOR_URL);
            }

            GUI.color = prevColor;
            
            GUILayout.Label($"<color=white>v{VERSION}</color>", new GUIStyle{alignment = TextAnchor.MiddleRight});
            GUILayout.EndHorizontal();
        }
        
        private static void DrawSeparator(float alpha = 0.8f, bool isWithSpacing = true)
        {
            if (isWithSpacing)
            {
                GUILayout.Space(10);
            }
            
            var rect = EditorGUILayout.BeginHorizontal();
            var color =  Color.white * alpha;
            color.a = 1f;
            Handles.color = color;
            Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
            EditorGUILayout.EndHorizontal();
            
            if (isWithSpacing)
            {
                GUILayout.Space(10);
            }
        }
        
        #endregion

        #region Project Initialization
        
        private void InitializeProject()
        {
            CreateBaseFolders();
            CreateScriptsFolders();
            CreateAssemblyDefinitions();
            CreateAudioFolders();
            CreateAudioMixer();
            
            if (_isAutoSaveEnabled)
            {
                SaveToEditor();
            }
        }

        private static void CreateBaseFolders()
        {
            CreateFolders(BaseFolders);
            Logger.Log("Project folders created successfully!");
        }
        
        private static void CreateScriptsFolders()
        {
            var selectedFolders = _scriptsFolders.Where(folderConfig => folderConfig.Include)
                    .Select(folderConfig => folderConfig.Name).ToArray();

            if (selectedFolders.Length <= 0)
            {
                return;
            }
            
            CreateFolders(selectedFolders, "Assets/Scripts");
            Logger.Log("Scripts folders created successfully!");
        }
        
        private static void CreateAssemblyDefinitions()
        {
            var selectedFolders = _scriptsFolders.Where(folderConfig => folderConfig.Include)
                .Select(folderConfig => folderConfig.Name).ToArray();

            if (selectedFolders.Length <= 0)
            {
                return;
            }
            
            foreach (var folder in selectedFolders)
            {
                string path = $"Assets/Scripts/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    Logger.LogWarning($"Folder {path} does not exist.");
                    continue;
                }

                CreateAssemblyDefinitionFile(folder, folder == "Editor");
            }
            
            Logger.Log("Assembly definitions created successfully!");
        }

        private static void CreateAudioFolders()
        {
            var selectedFolders = _audioFolders.Where(folderConfig => folderConfig.Include)
                .Select(folderConfig => folderConfig.Name).ToArray();

            if (selectedFolders.Length <= 0)
            {
                return;
            }
            
            CreateFolders(selectedFolders, "Assets/Audio");
            Logger.Log("Audio folders created successfully!");
        }
        
        private static void CreateAudioMixer()
        {
            var audioMixerFileContents = System.IO.File.ReadAllText("Assets/AudioMixerContents.txt");
            
            const string audioMixerPath = "Assets/Audio/Master.mixer";
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(audioMixerPath))
            {
                return;
            }
            
            System.IO.File.WriteAllText(audioMixerPath, audioMixerFileContents);
            AssetDatabase.ImportAsset(audioMixerPath);
            Logger.Log($"Audio Mixer created at {audioMixerPath}");
        }
        
        #endregion
        
        #region Dependency Management

        public static void AddPackages(IEnumerable<string> packages)
        {
            _packages.Clear();
            foreach (var package in packages)
            {
                _packages.Add(new PackageConfig(package, true));
            }
            if (_isAutoSaveEnabled)
                SaveToEditor();
        }
        
        private static void FetchDependencies()
        {
            FetchUnityRegistryPackages();
            FetchCustomUnityPackages();
        }

        private static void FetchUnityRegistryPackages()
        {
            foreach (var package in _packages.Where(package => package.Include))
            {
                _addRequest = Client.Add(package.Name);
                EditorApplication.update += Progress;
            }
            return;

            static void Progress()
            {
                if (!_addRequest.IsCompleted)
                {
                    return;
                }
                
                switch (_addRequest.Status)
                {
                    case StatusCode.Success:
                        Logger.Log($"Package {_addRequest.Result.name} added successfully.");
                        break;
                    case >= StatusCode.Failure:
                        Logger.LogError($"Failed to add package: {_addRequest.Error.message}");
                        break;
                }

                EditorApplication.update -= Progress;
            }
        }
        
        private static void FetchCustomUnityPackages()
        {
            foreach (var packagePath in _packageFiles.Where(packageFile => packageFile.Include)
                         .Select(packageFile => packageFile.Name))
            {
                if (!System.IO.File.Exists(packagePath))
                {
                    Logger.LogError($"Package file not found: {packagePath}");
                    continue;
                }
                
                AssetDatabase.ImportPackage(packagePath, true);
                Logger.Log($"Package imported from {packagePath}");
            }
        }
        
        #endregion
        
        #region Configuration
        
        private static InitiumConfig CreateConfig()
        {
            return new InitiumConfig
            {
                baseNamespace = _baseNamespace,
                ScriptsFolders = _scriptsFolders,
                AudioFolders = _audioFolders,
                PackageFiles = _packageFiles.ToList(),
                Packages = _packages.ToList()
            };
        }
        
        private static void LoadFromEditor()
        {
            var configJson = EditorPrefs.GetString(EDITOR_PREFS_CONFIG_KEY);
            LoadConfig(configJson);
        }
        
        private static void SaveToEditor()
        {
            var config = CreateConfig();
            EditorPrefs.SetString(EDITOR_PREFS_CONFIG_KEY, JsonUtility.ToJson(config));
            Logger.Log("Config saved to Editor preferences.");
        }

        private static void LoadConfig(string configJson)
        {
            var config = JsonUtility.FromJson<InitiumConfig>(configJson);
            _baseNamespace = config.baseNamespace;
            _scriptsFolders = config.ScriptsFolders;
            _audioFolders = config.AudioFolders;
            _packageFiles = new HashSet<PackageFileConfig>(config.PackageFiles?.Select(p =>
                new PackageFileConfig(p.Name, p.Include)) ?? System.Array.Empty<PackageFileConfig>());
            _packages = new HashSet<PackageConfig>(config.Packages?.Select(p =>
                new PackageConfig(p.Name, p.Include)) ?? System.Array.Empty<PackageConfig>());
        }

        private static void LoadConfigFile(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                Logger.LogWarning("No config file path set.");
                return;
            }

            if (!System.IO.File.Exists(configFilePath))
            {
                Logger.LogError($"Config file not found at {configFilePath}");
                return;
            }

            var configJson = System.IO.File.ReadAllText(configFilePath);
            try
            {
                LoadConfig(configJson);
                _configFileLoadOrSaveResult = $"Config file loaded from {configFilePath}";
                Logger.Log("Config file loaded successfully!");
            } 
            catch (System.Exception e)
            {
                _configFileLoadOrSaveResult = "Failed to load.";
                Logger.LogError($"Failed to load config file: {e.Message}");
            }
        }

        private static void SaveConfigFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogWarning("No config file path set.");
                return;
            }

            var config = CreateConfig();

            var configJson = JsonUtility.ToJson(config, true);
            System.IO.File.WriteAllText(path, configJson);
            _configFileLoadOrSaveResult = $"Config file saved to {path}";
            Logger.Log($"Config file saved to {path}");
        }
        
        #endregion
        
        #region Utility Methods
        
        private static void CreateFolders(string[] folders, string parentPath = "Assets")
        {
            foreach (var folder in folders)
            {
                var path = $"{parentPath}/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(parentPath, folder);
                }
            }
            AssetDatabase.Refresh();
        }
        
        private static void CreateAssemblyDefinitionFile(string folderName, bool isEditorOnly = false)
        {
            var asmdefFileContents = $@"{{
""name"": ""{_baseNamespace}.{folderName}"",
""rootNamespace"": ""{_baseNamespace}"",
""references"": [],
""includePlatforms"": [{(isEditorOnly ? "\"Editor\"" : "")}],
""excludePlatforms"": [],
""allowUnsafeCode"": false,
""overrideReferences"": false,
""precompiledReferences"": [],
""autoReferenced"": true,
""defineConstraints"": [],
""versionDefines"": [],
""noEngineReferences"": false
}}";
            var asmdefFilePath = $"Assets/Scripts/{folderName}/{_baseNamespace}.{folderName}.asmdef";
            if (AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(asmdefFilePath))
            {
                return;
            }
            
            System.IO.File.WriteAllText(asmdefFilePath, asmdefFileContents);
            AssetDatabase.ImportAsset(asmdefFilePath);
            Logger.Log($"Assembly definition created at {asmdefFilePath}");
        }
        
        #endregion
    }
}

#endif