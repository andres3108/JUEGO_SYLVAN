using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Construye la Zona 2 (misión 2) en su propia escena, Assets/Scenes/Zona2.unity: 70 m que empiezan en el
// bosque y terminan dentro de la cueva. El fondo es un panorama de 3 imágenes generadas con Higgsfield
// (Fondos/Zona2, unidas con unir_panorama.py en Assets/Guardian/Fondo/Zona2/Fondo_Zona2.jpg).
// El camino de las 3 imágenes está a la misma altura, así que el suelo es plano (y = 0).
// Obstáculos: troncos (tronco_1) en el bosque y rocas (roca_1..3) en la cueva, con un residuo encima de
// cada uno. Al ganar la Zona 1 se pasa a esta escena (las dos quedan en Build Settings).
// Menú: Guardianes del Bosque > Crear Zona 2 (misión 2)
public static class GuardianZona2
{
    const string Escena1 = "Assets/Scenes/Zona1.unity";
    const string Escena2 = "Assets/Scenes/Zona2.unity";
    const string FondoTex = "Assets/Guardian/Fondo/Zona2/Fondo_Zona2.jpg";
    const float Largo = 70f;
    const float InicioJugador = 1.2f;
    const float CaminoDesdeAbajo = 0.188f;   // altura del camino en las imágenes (0 = abajo, 1 = arriba)

    // obstáculos: (x, modelo, alto del modelo en m, escala del objeto)
    static readonly (float x, string modelo, float alto, Vector3 escala)[] Obstaculos =
    {
        (12f, "tronco_1", 1.1f, new Vector3(1.5f, 1.5f, 1f)),
        (24f, "tronco_1", 1.1f, new Vector3(1.5f, 1.5f, 1f)),
        (37f, "tronco_1", 1.1f, new Vector3(1.5f, 1.5f, 1f)),
        (50f, "roca_3", 1.0f, new Vector3(1.3f, 1.3f, 1f)),
        (57f, "roca_1", 1.0f, new Vector3(1.3f, 1.3f, 1f)),
        (64f, "roca_2", 1.0f, new Vector3(1.3f, 1.3f, 1f)),
    };

    // residuos (x, altura del centro): 0.5 = en el suelo; 2.5 = encima de un obstáculo; 1.8 = flotando
    static readonly Vector2[] Residuos =
    {
        new(4f, 0.5f), new(7f, 0.5f), new(10f, 0.5f), new(12f, 2.5f), new(15f, 0.5f), new(18f, 0.5f),
        new(19.5f, 1.8f), new(21f, 0.5f), new(24f, 2.5f), new(27f, 0.5f), new(30f, 0.5f), new(31.5f, 1.8f),
        new(33f, 0.5f), new(37f, 2.5f), new(40f, 0.5f), new(43f, 0.5f), new(44.5f, 1.8f), new(46f, 0.5f),
        new(50f, 2.5f), new(53f, 0.5f), new(57f, 2.5f), new(60f, 0.5f), new(64f, 2.5f), new(67f, 0.5f),
    };

    [MenuItem("Guardianes del Bosque/Crear Zona 2 (misión 2)")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. escena nueva copiando la Zona 1 (personajes, cámara, GameManager, selección)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Escena2) == null)
            AssetDatabase.CopyAsset(Escena1, Escena2);
        var escena = EditorSceneManager.OpenScene(Escena2);
        foreach (var n in new[] { "Zona_1", "Zona_2", "Fondo", "Nivel_Prototipo" })
        {
            var old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        var zona = new GameObject("Zona_2");

        // 2. suelo plano (el camino de las imágenes está a la misma altura) y límites invisibles
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Suelo";
        suelo.transform.SetParent(zona.transform);
        suelo.transform.position = new Vector3(Largo * 0.5f, -1.5f, 0f);
        suelo.transform.localScale = new Vector3(Largo + 2f, 3f, 3f);
        Object.DestroyImmediate(suelo.GetComponent<MeshRenderer>());
        foreach (var x in new[] { -0.5f, Largo + 0.5f })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Limite";
            wall.transform.SetParent(zona.transform);
            wall.transform.position = new Vector3(x, 5f, 0f);
            wall.transform.localScale = new Vector3(1f, 12f, 3f);
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        }

        // 3. obstáculos con los modelos de naturaleza
        for (int i = 0; i < Obstaculos.Length; i++)
        {
            var o = Obstaculos[i];
            string nombre = (o.modelo.StartsWith("tronco") ? "Tronco_" : "Roca_") + $"{i + 1:00}";
            CrearObstaculo(zona.transform, nombre, o.modelo, o.x, o.alto, o.escala);
        }

        // 4. residuos (modelo, tipo y puntos los pone GuardianResiduos)
        var cont = new GameObject("Residuos");
        cont.transform.SetParent(zona.transform);
        for (int i = 0; i < Residuos.Length; i++)
        {
            var r = new GameObject($"Residuo_{i + 1:00}");
            r.transform.SetParent(cont.transform);
            r.transform.position = new Vector3(Residuos[i].x, Residuos[i].y, 0f);
            var col = r.AddComponent<BoxCollider>();
            col.isTrigger = true;
            r.AddComponent<Residuo>();
        }

        // 5. personajes al inicio
        foreach (var pj in Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include))
        {
            pj.transform.position = new Vector3(InicioJugador, 0.05f, 0f);
            pj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // 6. misión
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm)
        {
            gm.misionTitulo = "Misión 2: Limpia el bosque y la cueva de cristal";
            gm.tituloVictoria = "¡Cueva limpia!";
            EditorUtility.SetDirty(gm);
        }

        // 7. cámara: sigue al jugador en horizontal, a altura fija, sin salirse de los 70 m
        var follow = cam.GetComponent<CameraFollow>() ? cam.GetComponent<CameraFollow>() : cam.gameObject.AddComponent<CameraFollow>();
        follow.target = Object.FindAnyObjectByType<GuardianController>()?.transform;
        follow.offset = new Vector3(0f, 2f, -10f);
        follow.seguirY = false;
        follow.alturaFija = 2f;
        follow.limitar = true;
        follow.minX = 0f;
        follow.maxX = Largo;
        follow.Snap();
        EditorUtility.SetDirty(follow);

        // 8. fondo panorámico fijo al mundo (empieza en x = 0, el camino queda en y = 0)
        CrearFondo(cam);

        GuardianResiduos.Apply();
        GuardianLetreros.ApplyZona2();   // letrero FINAL ZONA 2
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        // 9. Build Settings: Zona 1 -> Zona 2
        // (el menú principal, si existe, va siempre primero, y la Zona 3 detrás si existe)
        EditorBuildSettings.scenes = GuardianZona3.OrdenEscenas().Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        Debug.Log($"Zona 2 creada: {Largo} m, {Residuos.Length} residuos, {Obstaculos.Length} obstáculos. " +
                  "Al ganar la Zona 1 se pasa a la Zona 2.");
    }

    static void CrearObstaculo(Transform padre, string nombre, string modelo, float x, float alto, Vector3 escala)
    {
        var prefab = GuardianNaturaleza.Naturaleza(modelo);
        var mat = GuardianNaturaleza.Material(modelo);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre);
        go.transform.position = new Vector3(x, 0f, 0f);
        var col = go.AddComponent<BoxCollider>();
        go.AddComponent<Obstaculo>();

        var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
        vis.name = "Modelo";
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localRotation = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f) * prefab.transform.localRotation;
        vis.transform.localScale = prefab.transform.localScale;
        foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        var b = GuardianNaturaleza.Bounds(vis);
        vis.transform.localScale *= alto / Mathf.Max(0.01f, b.size.y);
        b = GuardianNaturaleza.Bounds(vis);
        vis.transform.position += new Vector3(0f, -b.min.y, 0f);
        b = GuardianNaturaleza.Bounds(vis);

        col.center = go.transform.InverseTransformPoint(b.center);
        col.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.95f, 1f);
        go.transform.localScale = escala;   // escala el modelo y su colisionador juntos
    }

    static void CrearFondo(Camera cam)
    {
        var imp = AssetImporter.GetAtPath(FondoTex) as TextureImporter;
        if (imp != null && (imp.maxTextureSize != 8192 || imp.wrapMode != TextureWrapMode.Clamp || imp.mipmapEnabled))
        {
            imp.maxTextureSize = 8192;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(FondoTex);
        float h = cam.orthographicSize * 2f * GuardianFondo.Escala;
        float w = h * tex.width / tex.height;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        string matPath = "Assets/Guardian/Fondo/Zona2/M_Fondo_Zona2.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = shader;
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);

        var fondo = new GameObject("Fondo");
        fondo.transform.position = new Vector3(0f, -CaminoDesdeAbajo * h + h * 0.5f, 25f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Fondo_Zona2";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(fondo.transform, false);
        q.transform.localPosition = new Vector3(w * 0.5f, 0f, 0f);
        q.transform.localScale = new Vector3(w, h, 1f);
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortingOrder = -100;   // como el fondo de la Zona 1: detrás de letreros y auras (renderer 2D)
        AssetDatabase.SaveAssets();
    }
}
