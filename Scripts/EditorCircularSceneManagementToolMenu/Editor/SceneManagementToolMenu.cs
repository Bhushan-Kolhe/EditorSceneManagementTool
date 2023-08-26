using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using Essentials.EditorCircularMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Essentials.EditorSceneLoaderMenu
{
    [InitializeOnLoad]
    public class SceneManagementToolMenu : EditorCircularMenuBase
    {
        protected override KeyCode ActivationKey => KeyCode.S;
        protected override int Radius
        {
            get
            {
                if (_activeMenuView is not null && (_activeMenuView.Path == "Load" || _activeMenuView.Path == "Close" || _activeMenuView.Path == "Remove"))
                    return 150;
                return 100;
            }
        }
        private static SceneManagementToolMenu _instance;

        private class CustomAssetPostProcessor : AssetPostprocessor
        {
            public static Action OnAssetCreatedCallback;
            public static Action OnAssetDeletedCallback;

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                if (importedAssets.Length > 0)
                {
                    OnAssetCreatedCallback?.Invoke();
                }

                if (deletedAssets.Length > 0)
                {
                    OnAssetDeletedCallback?.Invoke();
                }
            }
        }

        static SceneManagementToolMenu()
        {
            _instance = new SceneManagementToolMenu();
            EditorApplication.update -= _instance.OnEditorApplicationUpdate;
            EditorApplication.update += _instance.OnEditorApplicationUpdate;

            SceneView.duringSceneGui -= _instance.OnDuringSceneGUI;
            SceneView.duringSceneGui += _instance.OnDuringSceneGUI;

            CustomAssetPostProcessor.OnAssetCreatedCallback -= _instance.OnAssetsModifiedCallback;
            CustomAssetPostProcessor.OnAssetCreatedCallback += _instance.OnAssetsModifiedCallback;
            CustomAssetPostProcessor.OnAssetDeletedCallback -= _instance.OnAssetsModifiedCallback;
            CustomAssetPostProcessor.OnAssetDeletedCallback += _instance.OnAssetsModifiedCallback;
        }

        private void OnAssetsModifiedCallback()
        {
            ReInitializeMenu();
        }

        protected override void CreateMenu()
        {
            EditorCircularMenuView rootView = _rootMenuView;
            EditorCircularMenuView loadView = null;
            EditorCircularMenuView closeView = null;
            EditorCircularMenuView removeView = null;
            EditorCircularMenuView previousView = null;

            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/" });

            if (rootView is not null)
            {

                loadView = new EditorCircularMenuView("Load", "d_Folder Icon", () => SelectMenuItem(loadView), rootView);

                rootView.Children.Add(loadView);
                previousView = loadView;

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorCircularMenuView sceneView = null;
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string sceneName = "";

                    if (assetPath.Contains("/"))
                    {
                        string[] pathItems = assetPath.Split("/");
                        sceneName = pathItems[pathItems.Length - 1].Replace(".unity", "");
                    }
                    else
                    {
                        sceneName = assetPath.Replace(".unity", "");
                    }

                    sceneView = new EditorCircularMenuView(sceneName, "SceneAsset Icon", () => SelectMenuItem(sceneView), previousView);

                    previousView.Children.Add(sceneView);

                    var singleView = new EditorCircularMenuView("Single", "SceneAsset Icon", () =>
                    {
                        EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
                        _activeMenuView = _rootMenuView;
                        HideMenu();
                    }, sceneView);
                    sceneView.Children.Add(singleView);

                    var additiveView = new EditorCircularMenuView("Additive", "SceneAsset Icon", () =>
                    {
                        EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                        _activeMenuView = _rootMenuView;
                        HideMenu();
                    }, sceneView);
                    sceneView.Children.Add(additiveView);
                }
            }

            closeView = new EditorCircularMenuView("Close", "d_Folder Icon", () => SelectMenuItem(closeView), rootView);
            rootView.Children.Add(closeView);
            previousView = closeView;

            for (int i = 0; i < guids.Length; i++)
            {
                EditorCircularMenuView sceneView = null;
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string sceneName = "";

                if (assetPath.Contains("/"))
                {
                    string[] pathItems = assetPath.Split("/");
                    sceneName = pathItems[pathItems.Length - 1].Replace(".unity", "");
                }
                else
                {
                    sceneName = assetPath.Replace(".unity", "");
                }

                sceneView = new EditorCircularMenuView(sceneName, "SceneAsset Icon", () =>
                {
                    EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(assetPath), false);
                    _activeMenuView = _rootMenuView;
                    HideMenu();
                }, previousView);
                previousView.Children.Add(sceneView);
            }

            removeView = new EditorCircularMenuView("Remove", "d_Folder Icon", () => SelectMenuItem(removeView), rootView);
            rootView.Children.Add(removeView);
            previousView = removeView;

            for (int i = 0; i < guids.Length; i++)
            {
                EditorCircularMenuView sceneView = null;
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string sceneName = "";

                if (assetPath.Contains("/"))
                {
                    string[] pathItems = assetPath.Split("/");
                    sceneName = pathItems[pathItems.Length - 1].Replace(".unity", "");
                }
                else
                {
                    sceneName = assetPath.Replace(".unity", "");
                }

                sceneView = new EditorCircularMenuView(sceneName, "SceneAsset Icon", () =>
                {
                    EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(assetPath), true);
                    _activeMenuView = _rootMenuView;
                    HideMenu();
                }, previousView);
                previousView.Children.Add(sceneView);
            }

        }

    }
}
