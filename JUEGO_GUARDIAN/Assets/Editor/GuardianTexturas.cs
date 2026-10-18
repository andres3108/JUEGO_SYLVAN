using System.Linq;
using UnityEditor;
using UnityEngine;

// Asigna las texturas horneadas del Guardián (Assets/Guardian/Texturas/Guardian_tex_N.png)
// a materiales URP propios y los enlaza con los materiales del FBX (M_Guardian_N).
// Se ejecuta solo una vez al compilar si el FBX aún no tiene materiales asignados,
// y también desde el menú Guardianes del Bosque > Aplicar texturas del personaje.
[InitializeOnLoad]
public static class GuardianTexturas
{
    const string FbxPath = "Assets/Guardian_del_Bosque.fbx";
    const string TexFolder = "Assets/Guardian/Texturas";
    const string MatFolder = "Assets/Guardian/Materiales";

    static GuardianTexturas()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer != null && importer.GetExternalObjectMap().Count == 0)
                Apply();
        };
    }

    [MenuItem("Guardianes del Bosque/Aplicar texturas del personaje")]
    public static void Apply()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null) { Debug.LogWarning("No se encontró " + FbxPath); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Guardian")) AssetDatabase.CreateFolder("Assets", "Guardian");
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/Guardian", "Materiales");

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var fbxMaterials = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Material>().ToArray();
        int count = 0;
        foreach (var src in fbxMaterials)
        {
            // M_Guardian_7 -> Guardian_tex_7
            string id = src.name.Replace("M_Guardian_", "");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/Guardian_tex_{id}.png");
            if (tex == null) { Debug.LogWarning($"Sin textura para {src.name}"); continue; }

            string matPath = $"{MatFolder}/{src.name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mat);
            count++;
        }
        AssetDatabase.SaveAssets();
        importer.SaveAndReimport();
        Debug.Log($"Guardián: {count} materiales con textura asignados.");
    }
}
