using UnityEditor;
using UnityEngine;

// Fondo panorámico del bosque que hace de escenario: el personaje camina sobre el suelo dibujado en la imagen.
//  - Copias de Assets/Guardian/Fondo/Fondo_Bosque.jpg una tras otra desde el inicio del nivel (x = 0),
//    alternando la imagen normal y su reflejo para que las uniones no tengan cortes.
//  - El fondo está fijo al mundo (sin parallax) para que el suelo dibujado coincida con el suelo físico.
//  - El suelo físico ("Suelo" de la zona) sigue el perfil del camino de la imagen: sendero y muro de piedra
//    a una altura, y el puente que baja hacia la orilla del río (y vuelve a subir en la copia reflejada).
// Menú: Guardianes del Bosque > Aplicar fondo y suelo
[InitializeOnLoad]
public static class GuardianFondo
{
    const string Folder = "Assets/Guardian/Fondo";
    const string FondoTex = Folder + "/Fondo_Bosque.jpg";
    public const float Escala = 1.6f;      // alto del fondo = alto de la vista de la cámara x Escala

    // perfil del suelo de la imagen: (posición horizontal 0..1, altura desde abajo 0..1), medido en
    // Fondos/fondo 1.jpg (1584 x 672): sendero y muro, y el puente colgante (casi a nivel) sobre el río
    static readonly Vector2[] Perfil =
    {
        new(0.00f, 0.189f), new(0.74f, 0.189f), new(0.79f, 0.183f), new(0.84f, 0.175f),
        new(0.87f, 0.177f), new(0.91f, 0.187f), new(1.00f, 0.185f),
    };
    static float GroundFrac => Perfil[0].y;   // el sendero de la izquierda queda a la altura y = 0

    static GuardianFondo()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(FondoTex) == null) return;
            if (GameObject.Find("Fondo") == null && LevelRoot() != null)
                Apply();
        };
    }

    static GameObject LevelRoot() => GameObject.Find("Zona_1") ?? GameObject.Find("Nivel_Prototipo");

    // ---------------------------------------------------------------- medidas del fondo en el mundo
    public static float Alto(Camera cam) => cam.orthographicSize * 2f * Escala;

    public static float Ancho(Camera cam)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(FondoTex);
        return Alto(cam) * tex.width / tex.height;
    }

    // altura del suelo de la imagen en la x del mundo (el fondo empieza en x = 0)
    public static float Superficie(Camera cam, float x)
    {
        float h = Alto(cam), w = Ancho(cam);
        float u = Mathf.Max(0f, x) / w;
        int i = Mathf.FloorToInt(u);
        float f = u - i;
        if (i % 2 == 1) f = 1f - f;                 // copia reflejada
        float hf = Perfil[Perfil.Length - 1].y;
        for (int k = 0; k < Perfil.Length - 1; k++)
            if (f >= Perfil[k].x && f <= Perfil[k + 1].x)
            {
                float t = Mathf.InverseLerp(Perfil[k].x, Perfil[k + 1].x, f);
                hf = Mathf.Lerp(Perfil[k].y, Perfil[k + 1].y, t * t * (3f - 2f * t));
                break;
            }
        return (hf - GroundFrac) * h;
    }

    // malla de colisión que sigue la superficie entre x0 y x1
    public static Mesh MallaSuelo(Camera cam, float x0, float x1, float paso = 0.2f)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        int n = Mathf.CeilToInt((x1 - x0) / paso) + 1;
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Min(x1, x0 + i * paso), y = Superficie(cam, x);
            verts.Add(new Vector3(x, y, -1.5f)); verts.Add(new Vector3(x, y, 1.5f));
            verts.Add(new Vector3(x, -8f, -1.5f)); verts.Add(new Vector3(x, -8f, 1.5f));
        }
        for (int i = 0; i < n - 1; i++)
        {
            int a = i * 4, b = a + 4;
            tris.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });            // cara superior
            tris.AddRange(new[] { a, b, b + 2, a, b + 2, a + 2 });            // frente
            tris.AddRange(new[] { a + 1, a + 3, b + 3, a + 1, b + 3, b + 1 }); // detrás
        }
        var m = new Mesh { name = "Suelo_Perfil" };
        m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    [MenuItem("Guardianes del Bosque/Aplicar fondo y suelo")]
    public static void Apply()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("No hay cámara principal."); return; }

        var fondoTex = Texture(FondoTex, TextureWrapMode.Clamp, 4096);
        var old = GameObject.Find("Fondo");
        if (old) Undo.DestroyObjectImmediate(old);

        float h = Alto(cam), w = Ancho(cam), z = 25f;
        var follow = cam.GetComponent<CameraFollow>();
        float largo = follow != null && follow.limitar ? follow.maxX : 70f;
        int copies = Mathf.CeilToInt((largo + 2f) / w) + 1;

        var fondo = new GameObject("Fondo");
        Undo.RegisterCreatedObjectUndo(fondo, "Fondo");
        // el sendero de la imagen queda a la altura y = 0 y la imagen empieza en x = 0
        fondo.transform.position = new Vector3(0f, -GroundFrac * h + h * 0.5f, z);

        var matNormal = UnlitMaterial(Folder + "/M_Fondo.mat", fondoTex, Vector2.one, Vector2.zero);
        var matEspejo = UnlitMaterial(Folder + "/M_Fondo_Espejo.mat", fondoTex, new Vector2(-1f, 1f), new Vector2(1f, 0f));
        for (int i = 0; i < copies; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = $"Fondo_{i + 1:00}" + (i % 2 == 1 ? "_espejo" : "");
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(fondo.transform, false);
            q.transform.localPosition = new Vector3(w * (i + 0.5f), 0f, 0f);
            q.transform.localScale = new Vector3(w, h, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = i % 2 == 0 ? matNormal : matEspejo;
            r.sortingOrder = -100;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // suelo físico invisible que sigue el camino de la imagen
        var level = LevelRoot();
        if (level != null)
        {
            var oldVis = level.transform.Find("Suelo_Visual");
            if (oldVis) Undo.DestroyObjectImmediate(oldVis.gameObject);
            var oldSuelo = level.transform.Find("Suelo");
            if (oldSuelo) Undo.DestroyObjectImmediate(oldSuelo.gameObject);
            var oldCesped = level.transform.Find("Cesped");
            if (oldCesped) Undo.DestroyObjectImmediate(oldCesped.gameObject);

            var suelo = new GameObject("Suelo");
            suelo.transform.SetParent(level.transform, false);
            var mesh = MallaSuelo(cam, -1f, largo + 1f);
            AssetDatabase.DeleteAsset(Folder + "/Suelo_Perfil.asset");
            AssetDatabase.CreateAsset(mesh, Folder + "/Suelo_Perfil.asset");
            suelo.AddComponent<MeshCollider>().sharedMesh = mesh;
            Undo.RegisterCreatedObjectUndo(suelo, "Suelo");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fondo.scene);
        Debug.Log($"Fondo ({copies} tramos) fijo al mundo y suelo siguiendo el camino de la imagen.");
    }

    static Texture2D Texture(string path, TextureWrapMode wrap, int maxSize)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && (imp.wrapMode != wrap || imp.maxTextureSize != maxSize))
        {
            imp.wrapMode = wrap;
            imp.maxTextureSize = maxSize;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material UnlitMaterial(string path, Texture2D tex, Vector2 tiling, Vector2 offset)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.mainTexture = tex;
        mat.mainTextureScale = tiling;
        mat.mainTextureOffset = offset;
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetTextureOffset("_BaseMap", offset);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
