using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeBuilder
{
    public static void MigrateAndBuild()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Mosslight.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<MosslightGame>();
        if(!game.levelRoot) {
            Directory.CreateDirectory("Backups");
            File.Copy("Assets/Scenes/Mosslight.unity","Backups/Mosslight-before-editable.unity",false);
            Directory.CreateDirectory("Assets/Art");Directory.CreateDirectory("Assets/Prefabs");
            var sprites=game.CreateEditableLayout();
            var assets=new System.Collections.Generic.List<Sprite>();
            foreach(var pair in sprites) {
                pair.Value.name=pair.Key;
                AssetDatabase.CreateAsset(pair.Value.texture,"Assets/Art/"+pair.Key+".asset");
                AssetDatabase.AddObjectToAsset(pair.Value,pair.Value.texture);assets.Add(pair.Value);
            }
            game.artwork=assets.ToArray();
            var saved=new System.Collections.Generic.HashSet<string>();
            foreach(var placement in game.levelRoot.GetComponentsInChildren<MosslightPlacement>()) {
                string name=placement.GetComponent<SpriteRenderer>().sprite.name;
                if(saved.Add(name))PrefabUtility.SaveAsPrefabAsset(placement.gameObject,"Assets/Prefabs/"+name+".prefab");
            }
            EditorUtility.SetDirty(game);AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }
        // Keep scenery shadows attached when their object is moved or duplicated.
        foreach(var r in game.levelRoot.GetComponentsInChildren<SpriteRenderer>()) {
            if(r.sprite.name!="shadow"||r.transform.parent.GetComponent<MosslightPlacement>())continue;
            foreach(var placement in game.levelRoot.GetComponentsInChildren<MosslightPlacement>())
                if(placement.kind==MosslightPlacement.Kind.Obstacle&&Vector3.Distance(r.transform.position,placement.transform.position)<.001f) {
                    r.transform.SetParent(placement.transform,true);break;
                }
        }
        foreach(var placement in game.levelRoot.GetComponentsInChildren<MosslightPlacement>())
            if(placement.kind==MosslightPlacement.Kind.Obstacle)
                PrefabUtility.SaveAsPrefabAsset(placement.gameObject,"Assets/Prefabs/"+placement.GetComponent<SpriteRenderer>().sprite.name+".prefab");
        EditorSceneManager.SaveScene(game.gameObject.scene);
        // Reload persisted data to check the scene is self-contained before building.
        EditorSceneManager.OpenScene("Assets/Scenes/Mosslight.unity");
        game=UnityEngine.Object.FindAnyObjectByType<MosslightGame>();
        if(!game.levelRoot||!game.sceneCamera||game.artwork.Length<10)throw new Exception("Editable scene persistence failed");
        foreach(var r in game.levelRoot.GetComponentsInChildren<SpriteRenderer>())
            if(!r.sprite||!AssetDatabase.Contains(r.sprite))throw new Exception("Missing persistent sprite: "+r.name);
        Debug.Log("MOSSLIGHT_EDITABLE_SCENE_OK");
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
            var target=new RenderTexture(1280,800,24);game.sceneCamera.targetTexture=target;
            game.sceneCamera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
            File.WriteAllBytes("EditableScene.png",image.EncodeToPNG());
            game.sceneCamera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
        Build();
    }
    [MenuItem("Mosslight/Build playable prototype")]
    public static void Build()
    {
        // Build the saved scene; never replace the user's authored objects.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Mosslight.unity", true) };
        PlayerSettings.companyName = "DKov";
        PlayerSettings.productName = "Mosslight";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        // Check the projection used by movement, collision and targeting round-trips.
        for (int x = -10; x <= 10; x++)
        for (int y = -10; y <= 10; y++)
        {
            var p = new Vector2(x * .37f, y * .29f);
            if (Vector2.Distance(p, MosslightGame.Unproject(MosslightGame.Project(p))) > .0001f)
                throw new Exception("Isometric projection round-trip failed");
        }
        Debug.Log("Mosslight validation: 441 projection cases passed.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Mosslight.unity" },
            locationPathName = "Build/Mosslight.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("MOSSLIGHT_BUILD_OK");
    }
}
