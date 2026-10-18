using System.Linq;
using UnityEditor;
using UnityEngine;

// Pone los modelos 3D de residuos (botella, lata y papel arrugado, documento de diseño 8.4) en lugar del
// dibujo provisional de cada Residuo, repartidos de forma alterna. Cada tipo usa un material URP con
// color, relieve y un ligero brillo para que se distinga como objeto recogible.
// Modelos: Assets/Guardian/Residuos/<Tipo>.fbx (+ <Tipo>_Color.png y <Tipo>_Normal.png).
// Se ejecuta solo al compilar si hay residuos sin modelo, y desde el menú
// Guardianes del Bosque > Usar residuos 3D.
[InitializeOnLoad]
public static class GuardianResiduos
{
    const string Folder = "Assets/Guardian/Residuos";
    const string VisualName = "Residuo3D";
    static readonly string[] OldVisuals = { "Botella", "Tapa", "Botella3D", VisualName };

    // tipo, escala en el juego (sobre la altura del FBX), brillo propio, puntos al recogerlo
    static readonly (string tipo, float escala, float brillo, int puntos)[] Tipos =
    {
        ("Botella", 1.4f, 0.35f, 10),
        ("Lata", 1.4f, 0.25f, 15),
        ("Papel", 1.4f, 0.2f, 5),
    };

    static GuardianResiduos()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Tipos.All(t => AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{t.tipo}.fbx") != null)) return;
            if (Object.FindObjectsByType<Residuo>().Any(r => r.transform.Find(VisualName) == null))
                Apply();
        };
    }

    [MenuItem("Guardianes del Bosque/Usar residuos 3D")]
    public static void Apply()
    {
        var modelos = Tipos.Select(t => (t.tipo, t.escala, t.puntos, modelo: Prepare(t.tipo, t.brillo))).Where(m => m.modelo.go != null).ToArray();
        if (modelos.Length == 0) { Debug.LogWarning("No hay modelos en " + Folder); return; }

        var residuos = Object.FindObjectsByType<Residuo>().OrderBy(r => r.name).ToArray();
        for (int i = 0; i < residuos.Length; i++)
        {
            var r = residuos[i];
            var (tipo, escala, puntos, (go, mat)) = modelos[i % modelos.Length];
            Undo.RegisterFullObjectHierarchyUndo(r.gameObject, "Residuos 3D");
            r.tipo = tipo;
            r.puntos = puntos;
            EditorUtility.SetDirty(r);
            foreach (Transform child in r.transform.Cast<Transform>().ToArray())
                if (OldVisuals.Contains(child.name))
                    Undo.DestroyObjectImmediate(child.gameObject);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(go, r.transform);
            visual.name = VisualName;
            visual.transform.localPosition = Vector3.zero;
            // conservar la rotación y escala del FBX (corrigen los ejes de Blender a los de Unity)
            visual.transform.localRotation = go.transform.localRotation;
            visual.transform.localScale = go.transform.localScale * escala;
            foreach (var rend in visual.GetComponentsInChildren<Renderer>())
                rend.sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(visual, "Residuo 3D");

            var col = r.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = Vector3.zero;
                col.size = new Vector3(0.6f, 0.75f, 0.6f);   // zona de recogida algo más amplia que el modelo
            }
        }
        if (residuos.Length > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Residuos: {residuos.Length} modelos 3D colocados ({string.Join(", ", modelos.Select(m => m.tipo))}).");
    }

    // crea/actualiza el material del tipo y lo asigna al FBX
    static (GameObject go, Material mat) Prepare(string tipo, float brillo)
    {
        string fbx = $"{Folder}/{tipo}.fbx";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (go == null) return (null, null);

        string normalPath = $"{Folder}/{tipo}_Normal.png";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{tipo}_Color.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        string matPath = $"{Folder}/M_{tipo}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.shader = shader;
        mat.mainTexture = color;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (normal != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", tipo == "Papel" ? 0.2f : 0.7f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", tipo == "Lata" ? 0.4f : 0f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", color);
            mat.SetColor("_EmissionColor", Color.white * brillo);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);

        var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (importer != null && !importer.GetExternalObjectMap().Values.Contains(mat))
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_" + tipo), mat);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return (go, mat);
    }
}
