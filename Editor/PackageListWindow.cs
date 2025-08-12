#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Danqzq.Initium
{
    public class PackageListWindow : EditorWindow
    {
        private static ListRequest _listRequest;

        private static List<string> _packageNames;
        private static Dictionary<string, List<string>> _packageBundleLists;
        private static Dictionary<string, bool> _packagesToAdd;
        
        private static string _customPackageName;
        private static Vector2 _scrollPosition;
        
        internal static void ShowWindow(IEnumerable<string> addedPackages)
        {
            _listRequest = Client.List(false, true); 
            EditorApplication.update += Progress;

            _packageNames = new List<string>();
            _packageBundleLists = new Dictionary<string, List<string>>();
            _packagesToAdd = new Dictionary<string, bool>();

            foreach (var package in addedPackages)
            {
                _packagesToAdd[package] = true;
            }
            
            var window = GetWindow<PackageListWindow>();
            window.titleContent = new GUIContent("Package List");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Add Custom Package", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            _customPackageName = EditorGUILayout.TextField("Package Name / Git URL:", _customPackageName);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (string.IsNullOrWhiteSpace(_customPackageName))
                {
                    Logger.LogWarning("Package name cannot be empty.");
                    return;
                }
                
                if (!_packagesToAdd.TryAdd(_customPackageName, true))
                {
                    Logger.LogWarning($"Package '{_customPackageName}' is already in the list.");
                    return;
                }
                
                _packageNames.Add(_customPackageName);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Available Packages to Install (Unity Registry)", EditorStyles.boldLabel);
            
            if (_listRequest is not { IsCompleted: true })
            {
                GUILayout.Label("Loading packages...");
                return;
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                ToggleAll(true);
            }
            if (GUILayout.Button("Deselect All"))
            {
                ToggleAll(false);
            }
            GUILayout.EndHorizontal();
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            
            foreach (var packageName in _packageNames)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                _packagesToAdd.TryAdd(packageName, false);
                _packagesToAdd[packageName] = EditorGUILayout.ToggleLeft(packageName, _packagesToAdd[packageName]);
                EditorGUILayout.EndVertical();
            }
            foreach (var bundle in _packageBundleLists)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(bundle.Key, EditorStyles.boldLabel);
                foreach (var packageName in bundle.Value)
                {
                    _packagesToAdd.TryAdd(packageName, false);
                    _packagesToAdd[packageName] = EditorGUILayout.ToggleLeft(packageName, _packagesToAdd[packageName]);
                }
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.EndScrollView();
            
            const string defaultButtonName = "Select Packages";
            var selectedPackageCount = _packagesToAdd.Count(x => x.Value);
            var buttonName = selectedPackageCount == 0 ? defaultButtonName : $"{defaultButtonName} ({selectedPackageCount})";
            if (GUILayout.Button(buttonName, GUILayout.Height(50)))
            {
                if (_packagesToAdd.Count == 0)
                {
                    Logger.LogWarning("No packages selected to add.");
                    return;
                }
                Initium.AddPackages(_packagesToAdd.Where(x => x.Value).Select(x => x.Key));
                Close();
            }
        }
        
        private static void Progress()
        {
            if (!_listRequest.IsCompleted)
            {
                return;
            }
            
            switch (_listRequest.Status)
            {
                case StatusCode.Success:
                {
                    foreach (var package in _listRequest.Result)
                    {
                        AddPackageToList(package.name);
                    }
                    
                    var window = GetWindow<PackageListWindow>();
                    window.Repaint();

                    break;
                }
                case >= StatusCode.Failure:
                    Logger.LogError(_listRequest.Error.message);
                    break;
            }
            EditorApplication.update -= Progress;
        }

        private static void AddPackageToList(string packageName)
        {
            if (packageName.Count(x => x == '.') > 2)
            {
                var bundleName = packageName.Split('.')[2].ToUpper();
                _packageBundleLists.TryGetValue(bundleName, out var bundleList);
                if (bundleList == null)
                {
                    bundleList = new List<string>();
                    _packageBundleLists[bundleName] = bundleList;
                }
                bundleList.Add(packageName);
                return;
            }
            _packageNames.Add(packageName);
            _packagesToAdd.TryAdd(packageName, false);
        }
        
        private static void ToggleAll(bool toggle)
        {
            var keys = new List<string>(_packagesToAdd.Keys);
            foreach (var key in keys)
            {
                _packagesToAdd[key] = toggle;
            }
        }
    }
}
#endif