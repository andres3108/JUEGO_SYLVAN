using UnityEditor;
using UnityEngine;

// Construye la Zona 1 (misión 1): un sendero largo del bosque donde el Guardián camina desde el
// inicio de la foto de fondo recogiendo residuos (agua, latas y papel) que suman puntos.
// Sustituye al nivel de prueba (Nivel_Prototipo). Menú: Guardianes del Bosque > Crear Zona 1 (misión 1)
public static class GuardianZona1
{
    const float Largo = 70f;            // metros de recorrido
    const float InicioJugador = 1.2f;

    // residuos (x, altura del centro): 0.5 = en el suelo, más alto = hay que saltar
    static readonly Vector2[] Residuos =
    {
        new(4f, 0.5f), new(6.5f, 0.5f), new(9f, 1.8f), new(11.5f, 0.5f), new(14f, 0.5f), new(16f, 2.5f),
        new(19f, 0.5f), new(21.5f, 1.6f), new(24f, 0.5f), new(27f, 0.5f), new(29.5f, 2.2f), new(33f, 2.5f),
        new(36f, 0.5f), new(39f, 2.5f), new(42f, 0.5f), new(45f, 0.5f), new(47.5f, 2.1f), new(50f, 2.5f),
        new(53f, 0.5f), new(56f, 1.6f), new(59f, 0.5f), new(62f, 0.5f), new(65f, 2.5f), new(68f, 0.5f),
    };
    static readonly float[] Troncos = { 16f, 33f, 39f, 50f, 65f };   // con un residuo encima de cada uno

    [MenuItem("Guardianes del Bosque/Crear Zona 1 (misión 1)")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        var player = Object.FindAnyObjectByType<GuardianController>();
        if (player == null)
        {
            GuardianSetup.Setup();
            player = Object.FindAnyObjectByType<GuardianController>();
            if (player == null) return;
        }

        foreach (var n in new[] { "Nivel_Prototipo", "Zona_1", "Fondo", "Suelo", "Suelo_Plataforma" })
        {
            var old = GameObject.Find(n);
            if (old) Undo.DestroyObjectImmediate(old);
        }

        var zona = new GameObject("Zona_1");
        Undo.RegisterCreatedObjectUndo(zona, "Zona 1");

        // la cámara define el tamaño del fondo y, con él, la altura del suelo de la imagen
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        float Suelo(float x) => GuardianFondo.Superficie(cam, x);

        // límites invisibles en los extremos (el suelo lo crea GuardianFondo siguiendo la imagen)
        foreach (var x in new[] { -0.5f, Largo + 0.5f })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Limite";
            wall.transform.SetParent(zona.transform);
            wall.transform.position = new Vector3(x, 5f, 0f);
            wall.transform.localScale = new Vector3(1f, 12f, 3f);
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        }

        // troncos (obstáculos)
        var matTronco = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Tronco.mat");
        for (int i = 0; i < Troncos.Length; i++)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            t.name = $"Tronco_{i + 1:00}";
            t.transform.SetParent(zona.transform);
            t.transform.position = new Vector3(Troncos[i], Suelo(Troncos[i]) + 0.33f, 0f);
            t.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            t.transform.localScale = new Vector3(1.0f, 0.6f, 0.67f);
            if (matTronco) t.GetComponent<MeshRenderer>().sharedMaterial = matTronco;
            Object.DestroyImmediate(t.GetComponent<Collider>());
            t.AddComponent<BoxCollider>();
            t.AddComponent<Obstaculo>();
        }

        // residuos (el modelo 3D, tipo y puntos los pone GuardianResiduos)
        var cont = new GameObject("Residuos");
        cont.transform.SetParent(zona.transform);
        for (int i = 0; i < Residuos.Length; i++)
        {
            var r = new GameObject($"Residuo_{i + 1:00}");
            r.transform.SetParent(cont.transform);
            r.transform.position = new Vector3(Residuos[i].x, Suelo(Residuos[i].x) + Residuos[i].y, 0f);
            var col = r.AddComponent<BoxCollider>();
            col.isTrigger = true;
            r.AddComponent<Residuo>();
        }

        // personajes (Guardián y Guardiana) al inicio del recorrido, mirando a la derecha
        foreach (var pj in Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include))
        {
            Undo.RecordObject(pj.transform, "Inicio");
            pj.transform.position = new Vector3(InicioJugador, Suelo(InicioJugador) + 0.05f, 0f);
            pj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // GameManager con el título de la misión
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();
        Undo.RecordObject(gm, "Misión");
        gm.player = player;
        gm.misionTitulo = "Misión 1: Limpia el sendero del bosque";

        // cámara: sigue al jugador en horizontal sin salirse del nivel, a altura fija (así nunca se ve
        // el borde inferior de la imagen cuando el camino baja por el puente)
        if (cam != null)
        {
            Undo.RecordObject(cam, "Cámara");
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            Undo.RecordObject(follow, "Cámara");
            follow.target = player.transform;
            follow.offset = new Vector3(0f, 2f, -10f);
            follow.limitar = true;
            follow.seguirY = false;
            follow.alturaFija = 2f;
            follow.minX = 0f;
            follow.maxX = Largo;
            Undo.RecordObject(cam.transform, "Cámara");
            follow.Snap();
            EditorUtility.SetDirty(follow);
        }

        GuardianNaturaleza.Apply();     // troncos con el modelo tronco_1
        GuardianResiduos.Apply();
        GuardianFondo.Apply();
        GuardianLetreros.Apply();       // letreros INICIO y FINAL ZONA 1
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(zona.scene);
        Selection.activeGameObject = zona;
        Debug.Log($"Zona 1 creada: {Largo} m, {Residuos.Length} residuos, {Troncos.Length} troncos.");
    }
}
