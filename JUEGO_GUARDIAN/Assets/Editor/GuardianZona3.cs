using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Construye la Zona 3 (misión 3) en su propia escena, Assets/Scenes/Zona3.unity: unos 164 m con las
// imágenes zona3_2 ... zona3_7 de Fondos/Zona3 en orden (bosque con ruinas -> río -> entrada de la cueva ->
// santuario del gólem -> lago de cristal -> salida al bosque). La zona3_1 (ruinas con el puente colgante)
// se quitó de la escena. El panorama se une con Fondos/Zona3/unir_panorama.py
// y se guarda en dos mitades (Assets/Guardian/Fondo/Zona3/Fondo_Zona3_A/B.jpg), porque entero supera
// el máximo de textura de Unity (8192 px).
// En la cueva (x 56..135) caen rocas del techo (RocasQueCaen), con una sombra que avisa dónde caen.
// En la cascada del muro de zona3_2 hay dos huecos en el suelo: al caer se reaparece al inicio.
// El camino de piedra está a la misma altura en las imágenes 2..6; en la 7 (la última) está ~0.32 m más
// abajo, así que el suelo invisible sigue ese perfil (malla de colisión, como en la Zona 1).
// Al ganar la Zona 2 se pasa a esta escena (Build Settings: Menú -> Zona1 -> Zona2 -> Zona3).
// Menú: Guardianes del Bosque > Crear Zona 3 (misión 3)
public static class GuardianZona3
{
    const string Escena1 = "Assets/Scenes/Zona1.unity";
    const string Escena3 = "Assets/Scenes/Zona3.unity";
    const string Folder = "Assets/Guardian/Fondo/Zona3";
    static readonly string[] FondoTex = { Folder + "/Fondo_Zona3_A.jpg", Folder + "/Fondo_Zona3_B.jpg" };
    public const float Largo = 164f;
    const float InicioJugador = 1.2f;
    const float CaminoDesdeAbajo = 0.183f;   // camino de las imágenes 2..6 (0 = abajo, 1 = arriba) -> y = 0

    // medidas del panorama (píxeles): 6 imágenes de 2000 x 848 unidas con 220 px de fundido
    const float AnchoImagen = 2000f, AltoImagen = 848f, Fundido = 220f;
    static float Alto => 4f * 2f * GuardianFondo.Escala;       // alto del fondo en m (cámara ortográfica 4)
    static float MetrosPorPx => Alto / AltoImagen;
    const float BajadaFinal = 0.32f;                            // camino más bajo en la última imagen (zona3_7)

    // altura del suelo en x: 0 en las imágenes 2..6 y -0.32 en la 7, con una rampa suave en el fundido
    public static float Suelo(float x)
    {
        float paso = (AnchoImagen - Fundido) * MetrosPorPx;     // inicio de cada imagen
        float f = Fundido * MetrosPorPx;
        float b0 = 5f * paso;                                   // fundido zona3_6 -> zona3_7
        float t = x <= b0 ? 0f : Mathf.SmoothStep(0f, 1f, (x - b0) / f);
        return -BajadaFinal * t;
    }

    // huecos del suelo (x inicio, x fin): la cascada del muro de piedra de zona3_2, a los dos lados de la
    // roca grande del medio (medidos en la imagen: muro hasta 1060 px, roca 1145..1270 px, muro desde 1317 px).
    // Al caer por un hueco se reaparece al inicio de la zona (componente Reaparicion).
    static readonly Vector2[] Huecos = { new(16.05f, 17.3f), new(19.15f, 19.85f) };

    const float PorcentajeAlambre = 0.3f;   // parte de los obstáculos que se cambian por alambre de púas

    // obstáculos: (x, modelo). Troncos en el bosque, rocas en la cueva (zona3_4..6, ~54..138 m).
    // Al crear la escena, el 30 % (al azar) se cambia por alambre de púas.
    static readonly (float x, string modelo)[] Obstaculos =
    {
        (10f, "tronco_1"), (29f, "tronco_1"), (41f, "tronco_1"),
        (53f, "roca_2"), (65f, "roca_1"), (77f, "roca_3"), (89f, "roca_2"), (101f, "roca_1"), (113f, "roca_3"),
        (125f, "roca_2"), (139f, "tronco_1"), (151f, "tronco_1"),
    };

    [MenuItem("Guardianes del Bosque/Crear Zona 3 (misión 3)")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. escena nueva copiando la Zona 1 (personajes, cámara, GameManager, selección)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Escena3) == null)
            AssetDatabase.CopyAsset(Escena1, Escena3);
        var escena = EditorSceneManager.OpenScene(Escena3);
        foreach (var n in new[] { "Zona_1", "Zona_2", "Zona_3", "Fondo", "Nivel_Prototipo" })
        {
            var old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        var zona = new GameObject("Zona_3");

        // 2. suelo invisible que sigue el camino de las imágenes, cortado en los huecos, y límites
        AssetDatabase.DeleteAsset(Folder + "/Suelo_Zona3.asset");
        var tramos = new List<Vector2>();
        float desde = -1f;
        foreach (var h in Huecos) { tramos.Add(new Vector2(desde, h.x)); desde = h.y; }
        tramos.Add(new Vector2(desde, Largo + 1f));
        for (int i = 0; i < tramos.Count; i++)
        {
            var suelo = new GameObject(i == 0 ? "Suelo" : $"Suelo_{i + 1}");
            suelo.transform.SetParent(zona.transform, false);
            var malla = MallaSuelo(tramos[i].x, tramos[i].y);
            string ruta = $"{Folder}/Suelo_Zona3_{i + 1}.asset";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(malla, ruta);
            suelo.AddComponent<MeshCollider>().sharedMesh = malla;
        }
        // caer en un hueco: reaparecer al inicio de la zona (sin perder lo recogido)
        var rea = zona.AddComponent<Reaparicion>();
        rea.punto = new Vector3(InicioJugador, Suelo(InicioJugador) + 0.05f, 0f);
        foreach (var x in new[] { -0.5f, Largo + 0.5f })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Limite";
            wall.transform.SetParent(zona.transform);
            wall.transform.position = new Vector3(x, 5f, 0f);
            wall.transform.localScale = new Vector3(1f, 12f, 3f);
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        }

        // 3. obstáculos con los modelos de naturaleza (mismo tamaño que en la Zona 2)
        // el 30 % de los obstáculos, elegidos al azar, se cambian por alambre de púas (alambre_1)
        int nAlambre = Mathf.RoundToInt(Obstaculos.Length * PorcentajeAlambre);
        var conAlambre = new HashSet<int>(Enumerable.Range(0, Obstaculos.Length).OrderBy(_ => Random.value).Take(nAlambre));
        int nt = 0, nr = 0, na = 0;
        for (int i = 0; i < Obstaculos.Length; i++)
        {
            var o = Obstaculos[i];
            if (conAlambre.Contains(i))
            {
                // rollo de ~1.1 m de alto y ~2.5 m de largo; casi sin giro para que se vean las vueltas
                CrearObstaculo(zona.transform, $"Alambre_{++na:00}", "alambre_1", o.x, 0.9f, new Vector3(1.2f, 1.2f, 1f), 10f);
                continue;
            }
            bool tronco = o.modelo.StartsWith("tronco");
            string nombre = tronco ? $"Tronco_{++nt:00}" : $"Roca_{++nr:00}";
            CrearObstaculo(zona.transform, nombre, o.modelo, o.x, tronco ? 1.1f : 1.0f,
                tronco ? new Vector3(1.5f, 1.5f, 1f) : new Vector3(1.3f, 1.3f, 1f));
        }
        Debug.Log("Alambre de púas en x = " + string.Join(", ", conAlambre.OrderBy(k => k).Select(k => Obstaculos[k].x)));

        // rocas que caen del techo de la cueva (entrada de la cueva -> lago de cristal)
        CrearRocasQueCaen(zona.transform);

        // 4. residuos: uno encima de cada obstáculo y el resto cada 3 m por el camino (algunos flotando)
        var cont = new GameObject("Residuos");
        cont.transform.SetParent(zona.transform);
        var puntos = Residuos();
        for (int i = 0; i < puntos.Count; i++)
        {
            var r = new GameObject($"Residuo_{i + 1:00}");
            r.transform.SetParent(cont.transform);
            r.transform.position = new Vector3(puntos[i].x, Suelo(puntos[i].x) + puntos[i].y, 0f);
            var col = r.AddComponent<BoxCollider>();
            col.isTrigger = true;
            r.AddComponent<Residuo>();
        }

        // 5. personajes al inicio
        foreach (var pj in Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include))
        {
            pj.transform.position = new Vector3(InicioJugador, Suelo(InicioJugador) + 0.05f, 0f);
            pj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // 6. misión
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm)
        {
            gm.misionTitulo = "Misión 3: Limpia las ruinas y el santuario de cristal";
            gm.tituloVictoria = "¡Santuario limpio!";
            EditorUtility.SetDirty(gm);
        }

        // 7. cámara: sigue al jugador en horizontal, a altura fija, sin salirse del recorrido
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
        CrearFondo();

        GuardianResiduos.Apply();
        GuardianLetreros.ApplyZona3();   // letrero FINAL ZONA 3
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        // 9. Build Settings: Menú -> Zona 1 -> Zona 2 -> Zona 3
        EditorBuildSettings.scenes = OrdenEscenas().Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        Debug.Log($"Zona 3 creada: {Largo} m, {puntos.Count} residuos, {Obstaculos.Length} obstáculos. " +
                  "Al ganar la Zona 2 se pasa a la Zona 3.");
    }

    // orden del juego con las escenas que existan (también lo usan la Zona 2 y el menú)
    public static string[] OrdenEscenas() =>
        new[] { "Assets/Scenes/MenuPrincipal.unity", Escena1, "Assets/Scenes/Zona2.unity", Escena3 }
            .Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null).ToArray();

    // (x, altura sobre el suelo): 0.5 = en el suelo, 2.5 = encima de un obstáculo, 1.8 = flotando
    // (también uno flotando sobre cada hueco, para recogerlo al saltarlo)
    static List<Vector2> Residuos()
    {
        var l = new List<Vector2>();
        foreach (var o in Obstaculos) l.Add(new Vector2(o.x, 2.5f));
        foreach (var h in Huecos) l.Add(new Vector2((h.x + h.y) * 0.5f, 1.8f));
        int k = 0;
        for (float x = 4f; x <= Largo - 4f; x += 3f)
        {
            if (Obstaculos.Any(o => Mathf.Abs(o.x - x) < 1.6f)) continue;
            if (Huecos.Any(h => x > h.x - 0.6f && x < h.y + 0.6f)) continue;
            l.Add(new Vector2(x, ++k % 4 == 0 ? 1.8f : 0.5f));
        }
        return l.OrderBy(v => v.x).ToList();
    }

    static Mesh MallaSuelo(float x0, float x1, float paso = 0.2f)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        int n = Mathf.CeilToInt((x1 - x0) / paso) + 1;
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Min(x1, x0 + i * paso), y = Suelo(x);
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
        int u = (n - 1) * 4;
        tris.AddRange(new[] { 0, 2, 3, 0, 3, 1 });                    // pared al inicio del tramo
        tris.AddRange(new[] { u, u + 1, u + 3, u, u + 3, u + 2 });    // pared al final del tramo
        var m = new Mesh { name = "Suelo_Zona3" };
        m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // tramo con rocas que caen: plantillas inactivas (roca_1..3, ~0.7 m) que RocasQueCaen copia al jugar
    static void CrearRocasQueCaen(Transform zona)
    {
        var go = new GameObject("RocasQueCaen");
        go.transform.SetParent(zona, false);
        var comp = go.AddComponent<RocasQueCaen>();
        comp.desdeX = 56f;
        comp.hastaX = 135f;
        comp.alturaSuelo = Suelo(95f);
        var lista = new List<GameObject>();
        foreach (var modelo in new[] { "roca_1", "roca_2", "roca_3" })
        {
            var prefab = GuardianNaturaleza.Naturaleza(modelo);
            var mat = GuardianNaturaleza.Material(modelo);
            var t = new GameObject("Plantilla_" + modelo);
            t.transform.SetParent(go.transform, false);
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.transform);
            vis.name = "Modelo";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = prefab.transform.localRotation;
            vis.transform.localScale = prefab.transform.localScale;
            foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            var b = GuardianNaturaleza.Bounds(vis);
            vis.transform.localScale *= 0.7f / Mathf.Max(0.01f, b.size.y);
            b = GuardianNaturaleza.Bounds(vis);
            vis.transform.position += t.transform.position - b.center;   // centro del modelo en el origen
            b = GuardianNaturaleza.Bounds(vis);
            var col = t.AddComponent<SphereCollider>();
            col.radius = Mathf.Max(b.extents.x, b.extents.y) * 0.85f;
            t.SetActive(false);
            lista.Add(t);
        }
        comp.plantillas = lista.ToArray();
    }

    static void CrearObstaculo(Transform padre, string nombre, string modelo, float x, float alto, Vector3 escala,
                               float giro = 25f)
    {
        var prefab = GuardianNaturaleza.Naturaleza(modelo);
        var mat = GuardianNaturaleza.Material(modelo);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre);
        go.transform.position = new Vector3(x, Suelo(x), 0f);
        var col = go.AddComponent<BoxCollider>();
        go.AddComponent<Obstaculo>();

        var vis = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
        vis.name = "Modelo";
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localRotation = Quaternion.Euler(0f, Random.Range(-giro, giro), 0f) * prefab.transform.localRotation;
        vis.transform.localScale = prefab.transform.localScale;
        foreach (var r in vis.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        var b = GuardianNaturaleza.Bounds(vis);
        vis.transform.localScale *= alto / Mathf.Max(0.01f, b.size.y);
        b = GuardianNaturaleza.Bounds(vis);
        vis.transform.position += new Vector3(0f, go.transform.position.y - b.min.y, 0f);
        b = GuardianNaturaleza.Bounds(vis);

        col.center = go.transform.InverseTransformPoint(b.center);
        col.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.95f, 1f);
        go.transform.localScale = escala;   // escala el modelo y su colisionador juntos
    }

    static void CrearFondo()
    {
        float h = Alto;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var fondo = new GameObject("Fondo");
        fondo.transform.position = new Vector3(0f, -CaminoDesdeAbajo * h + h * 0.5f, 25f);
        float x = 0f;
        for (int i = 0; i < FondoTex.Length; i++)
        {
            var imp = AssetImporter.GetAtPath(FondoTex[i]) as TextureImporter;
            if (imp != null && (imp.maxTextureSize != 8192 || imp.wrapMode != TextureWrapMode.Clamp || imp.mipmapEnabled))
            {
                imp.maxTextureSize = 8192;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(FondoTex[i]);
            float w = h * tex.width / tex.height;

            string matPath = $"{Folder}/M_Fondo_Zona3_{(char)('A' + i)}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = shader;
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = $"Fondo_Zona3_{(char)('A' + i)}";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(fondo.transform, false);
            q.transform.localPosition = new Vector3(x + w * 0.5f, 0f, 0f);
            q.transform.localScale = new Vector3(w, h, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = -100;   // detrás de letreros y auras (renderer 2D)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            x += w;
        }
        AssetDatabase.SaveAssets();
    }
}
