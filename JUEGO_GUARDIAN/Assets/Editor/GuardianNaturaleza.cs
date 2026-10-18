using System.Linq;
using UnityEditor;
using UnityEngine;

// Objetos de naturaleza (Assets/Guardian/Naturaleza/<nombre>/<nombre>.fbx + <nombre>_color.png):
// tronco_1 (tocón agrietado) y roca_1, roca_2, roca_3, exportados desde Objetos/Modelos_naturaleza.
//  - Crea un material URP con la textura de cada uno y lo asigna a su FBX.
//  - Sustituye el dibujo provisional de los troncos (Tronco_XX de la escena) por el modelo tronco_1,
//    apoyado en el suelo, con un colisionador ajustado a su forma (sigue siendo obstáculo).
// Menú: Guardianes del Bosque > Usar tronco nuevo. Las rocas quedan listas para usarlas en zonas nuevas
// con Naturaleza(nombre).
public static class GuardianNaturaleza
{
    const string Folder = "Assets/Guardian/Naturaleza";
    public static readonly string[] Nombres = { "tronco_1", "roca_1", "roca_2", "roca_3", "alambre_1" };
    const float AltoTronco = 1.1f;     // metros: se puede saltar
    static readonly Vector3 EscalaTronco = new(1.5f, 1.5f, 1f);   // escala del objeto Tronco_XX (alto final ~1.65 m)
    const string Visual = "Modelo";

    // FBX con su material listo
    public static GameObject Naturaleza(string nombre)
    {
        string fbx = $"{Folder}/{nombre}/{nombre}.fbx";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (go == null) return null;
        var mat = Material(nombre);
        var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (imp != null && !imp.GetExternalObjectMap().Values.Contains(mat))
        {
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_" + nombre), mat);
            imp.SaveAndReimport();
        }
        return go;
    }

    [MenuItem("Guardianes del Bosque/Usar tronco nuevo")]
    public static void Apply()
    {
        foreach (var n in Nombres) Naturaleza(n);
        var modelo = Naturaleza("tronco_1");
        if (modelo == null) { Debug.LogWarning("No se encontró tronco_1.fbx"); return; }
        var mat = Material("tronco_1");

        var troncos = Object.FindObjectsByType<Obstaculo>().Where(o => o.name.StartsWith("Tronco")).ToArray();
        foreach (var t in troncos)
        {
            var go = t.gameObject;
            Undo.RegisterFullObjectHierarchyUndo(go, "Tronco nuevo");
            // la base del tronco viejo estaba 0.33 m por debajo de su centro
            float suelo = go.GetComponent<MeshRenderer>() ? go.transform.position.y - 0.33f
                                                         : go.transform.position.y;
            foreach (var c in new Component[] { go.GetComponent<MeshRenderer>(), go.GetComponent<MeshFilter>() })
                if (c) Undo.DestroyObjectImmediate(c);
            var old = go.transform.Find(Visual);
            if (old) Undo.DestroyObjectImmediate(old.gameObject);

            go.transform.SetPositionAndRotation(new Vector3(go.transform.position.x, suelo, go.transform.position.z), Quaternion.identity);
            go.transform.localScale = Vector3.one;

            var vis = (GameObject)PrefabUtility.InstantiatePrefab(modelo, go.transform);
            vis.name = Visual;
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f) * modelo.transform.localRotation;
            vis.transform.localScale = modelo.transform.localScale;
            foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            // escalar a la altura deseada
            var b = Bounds(vis);
            vis.transform.localScale *= AltoTronco / Mathf.Max(0.01f, b.size.y);
            b = Bounds(vis);
            vis.transform.position += new Vector3(0f, suelo - b.min.y, 0f);
            b = Bounds(vis);

            // colisionador: un poco más estrecho que el modelo para que sea justo
            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.center = go.transform.InverseTransformPoint(b.center);
            col.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.95f, 1f);
            go.transform.localScale = EscalaTronco;   // escala el modelo y su colisionador juntos
            EditorUtility.SetDirty(go);
        }
        if (troncos.Length > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(troncos[0].gameObject.scene);
        Debug.Log($"Tronco nuevo: {troncos.Length} troncos sustituidos por tronco_1.");
    }

    public static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static Material Material(string nombre)
    {
        string path = $"{Folder}/{nombre}/M_{nombre}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{nombre}/{nombre}_color.png");
        mat.shader = shader;
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
