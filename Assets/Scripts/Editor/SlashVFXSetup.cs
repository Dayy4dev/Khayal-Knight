#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public class SlashVFXSetup : EditorWindow
{
    [MenuItem("Tools/Setup Slash VFX")]
    public static void SetupVFX()
    {
        
        GameObject slashVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SlashVFX.prefab");

        string[] guids = AssetDatabase.FindAssets("BalloonSword t:Prefab");
        foreach(string guid in guids)
        {
            string bsPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject bsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bsPath);
            
            BalloonSwordVFX vfxComponent = bsPrefab.GetComponent<BalloonSwordVFX>();
            if (vfxComponent == null)
            {
                vfxComponent = bsPrefab.AddComponent<BalloonSwordVFX>();
            }

            SerializedObject so = new SerializedObject(vfxComponent);
            SerializedProperty prop = so.FindProperty("slashVfxPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = slashVfxPrefab;
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SavePrefabAsset(bsPrefab);
            Debug.Log($"Updated BalloonSword Prefab at {bsPath}");
        }

        Debug.Log("Setup Slash VFX completed!");
    }
}
#endif
