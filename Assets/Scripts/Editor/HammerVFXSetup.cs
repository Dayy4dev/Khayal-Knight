#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public class HammerVFXSetup : EditorWindow
{
    [MenuItem("Tools/Setup Hammer Finisher VFX")]
    public static void SetupVFX()
    {
        const string prefabPath = "Assets/Prefabs/BoomVFX.prefab";
        const string matPath    = "Assets/Materials/BoomVFX_Mat.mat";
        const string clipPath   = "Assets/_VFX/boom.mp4";

        // Material
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = Shader.Find("Custom/AdditiveVFX");
        if (shader == null) { Debug.LogError("[HammerVFXSetup] Shader 'Custom/AdditiveVFX' not found!"); return; }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject boomPrefab;

        if (existing == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BoomVFX";
            DestroyImmediate(quad.GetComponent<Collider>());

            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            VideoPlayer vp = quad.AddComponent<VideoPlayer>();
            vp.renderMode  = VideoRenderMode.MaterialOverride;
            vp.targetMaterialRenderer = mr;
            vp.playOnAwake = false;
            vp.isLooping   = false;

            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath);
            if (clip != null) vp.clip = clip;
            else Debug.LogWarning($"[HammerVFXSetup] VideoClip not found at {clipPath}");

            boomPrefab = PrefabUtility.SaveAsPrefabAsset(quad, prefabPath);
            DestroyImmediate(quad);
            Debug.Log($"[HammerVFXSetup] Created BoomVFX prefab at {prefabPath}");
        }
        else
        {
            boomPrefab = existing;
            Debug.Log("[HammerVFXSetup] BoomVFX prefab already exists, reusing it.");
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int patched = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            if (go.GetComponent<ToyHammer>() == null) continue;

            ToyHammerVFX vfxComp = go.GetComponent<ToyHammerVFX>();
            if (vfxComp == null)
                vfxComp = go.AddComponent<ToyHammerVFX>();

            SerializedObject so   = new SerializedObject(vfxComp);
            SerializedProperty sp = so.FindProperty("boomVfxPrefab");
            if (sp != null)
            {
                sp.objectReferenceValue = boomPrefab;
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SavePrefabAsset(go);
            Debug.Log($"[HammerVFXSetup] Patched: {path}");
            patched++;
        }

        Debug.Log($"[HammerVFXSetup] Done — patched {patched} prefab(s).");
        AssetDatabase.SaveAssets();
    }
}
#endif
