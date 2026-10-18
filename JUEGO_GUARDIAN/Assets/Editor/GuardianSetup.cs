using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menú: Guardianes del Bosque > Configurar personaje
// Deja listo al Guardián para moverse con el teclado: animaciones en bucle, Animator Controller,
// física, suelo de prueba y cámara que lo sigue.
public static class GuardianSetup
{
    const string FbxPath = "Assets/Guardian_del_Bosque.fbx";
    const string ControllerPath = "Assets/Animations/Guardian.controller";
    static readonly string[] LoopClips = { "Idle", "Run", "Slow", "Walk" };

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    [MenuItem("Guardianes del Bosque/Configurar personaje")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Guardianes del Bosque", "Detén el modo Play antes de configurar el personaje.", "OK");
            return;
        }

        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Guardianes del Bosque", "No se encontró " + FbxPath, "OK");
            return;
        }

        // 1. Rig genérico y animaciones en bucle
        importer.animationType = ModelImporterAnimationType.Generic;
        var clips = importer.defaultClipAnimations;
        foreach (var c in clips)
        {
            string shortName = c.name.Split('|').Last();
            c.name = shortName;
            c.loopTime = LoopClips.Contains(shortName);
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        AnimationClip Clip(string n) => AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>().FirstOrDefault(a => a.name == n);

        // 2. Animator Controller: Idle <-> Run, Jump en el aire, Victory por trigger
        if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Victory", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle"); idle.motion = Clip("Idle");
        var run = sm.AddState("Run"); run.motion = Clip("Run");
        var jump = sm.AddState("Jump"); jump.motion = Clip("Jump");
        var victory = sm.AddState("Victory"); victory.motion = Clip("Victory");
        sm.defaultState = idle;

        AnimatorStateTransition T(AnimatorState from, AnimatorState to, float dur = 0.1f)
        {
            var t = from.AddTransition(to); t.hasExitTime = false; t.duration = dur; return t;
        }
        T(idle, run).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        T(run, idle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        T(idle, jump).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        T(run, jump).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        var land = T(jump, idle, 0.08f); land.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        var anyVictory = sm.AddAnyStateTransition(victory);
        anyVictory.hasExitTime = false; anyVictory.duration = 0.1f; anyVictory.canTransitionToSelf = false;
        anyVictory.AddCondition(AnimatorConditionMode.If, 0, "Victory");
        var backIdle = victory.AddTransition(idle); backIdle.hasExitTime = true; backIdle.exitTime = 1f;

        // 3. Personaje en la escena
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var guardian = Object.FindObjectsByType<Animator>()
            .Select(a => a.gameObject).FirstOrDefault(g => g.name.StartsWith("Guardian_del_Bosque") && g.transform.parent == null);
        if (guardian == null)
        {
            guardian = (GameObject)PrefabUtility.InstantiatePrefab(model);
        }
        Undo.RegisterFullObjectHierarchyUndo(guardian, "Configurar Guardián");
        guardian.name = "Guardian_del_Bosque";
        guardian.transform.position = new Vector3(0f, 0.05f, 0f);

        var animator = GetOrAdd<Animator>(guardian);
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        var capsule = GetOrAdd<CapsuleCollider>(guardian);
        capsule.center = new Vector3(0f, 0.8f, 0f);
        capsule.height = 1.6f;
        capsule.radius = 0.25f;
        var pm = new PhysicsMaterial("SinFriccion") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
        capsule.material = pm;

        var rb = GetOrAdd<Rigidbody>(guardian);
        rb.mass = 1f;
        if (guardian.GetComponent<GuardianController>() == null) guardian.AddComponent<GuardianController>();

        // 4. Suelo de prueba (si no hay ninguno)
        if (GameObject.Find("Suelo") == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Suelo";
            ground.transform.position = new Vector3(10f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 3f);
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Suelo_Plataforma";
            step.transform.position = new Vector3(6f, 0.5f, 0f);
            step.transform.localScale = new Vector3(2f, 1f, 3f);
            Undo.RegisterCreatedObjectUndo(ground, "Suelo");
            Undo.RegisterCreatedObjectUndo(step, "Plataforma");
        }

        // 5. Cámara lateral que sigue al personaje
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam.transform, "Cámara");
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = guardian.transform.position + new Vector3(0f, 1.2f, -10f);
            if (cam.orthographic) cam.orthographicSize = 3f; else cam.fieldOfView = 30f;
            var follow = GetOrAdd<CameraFollow>(cam.gameObject);
            follow.target = guardian.transform;
        }

        // Luz para ver el modelo 3D si la escena solo tiene luz 2D
        if (Object.FindObjectsByType<Light>().Length == 0)
        {
            var sun = new GameObject("Directional Light");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Undo.RegisterCreatedObjectUndo(sun, "Luz");
        }

        Selection.activeGameObject = guardian;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guardian.scene);
        Debug.Log("Guardián configurado. Pulsa Play: A/D o flechas para correr, Espacio para saltar.");
    }

    // ------------------------------------------------------------------ nivel del prototipo de Python
    // Convierte las posiciones en píxeles de guardianes_prototipo.py (pantalla 800x500, suelo en y=380)
    // a metros: 45 px = 1 m, y el centro del jugador (x=120 px) queda en x=0.
    const float PxPerMeter = 45f;
    static float U(float px) => (px - 120f) / PxPerMeter;
    static float H(float pxFromTop) => (380f - pxFromTop) / PxPerMeter;

    static readonly Vector2[] ResiduosPx =
    {
        new Vector2(300, 345), new Vector2(450, 345), new Vector2(600, 345),
        new Vector2(250, 250), new Vector2(500, 220), new Vector2(680, 345),
        new Vector2(180, 345), new Vector2(380, 200), new Vector2(580, 250), new Vector2(720, 200),
    };
    static readonly Rect[] TroncosPx = { new Rect(350, 350, 45, 30), new Rect(550, 350, 50, 30) };

    [MenuItem("Guardianes del Bosque/Crear nivel del prototipo")]
    public static void CreatePrototypeLevel()
    {
        if (EditorApplication.isPlaying) return;

        var player = Object.FindAnyObjectByType<GuardianController>();
        if (player == null)
        {
            Setup();
            player = Object.FindAnyObjectByType<GuardianController>();
            if (player == null) return;
        }

        // borrar un nivel anterior y la plataforma de prueba
        foreach (var name in new[] { "Nivel_Prototipo", "Suelo", "Suelo_Plataforma", "GameManager" })
        {
            var old = GameObject.Find(name);
            if (old) Undo.DestroyObjectImmediate(old);
        }

        var matSuelo = GetMaterial("M_Suelo", new Color32(135, 187, 85, 255));
        var matCesped = GetMaterial("M_Cesped", new Color32(46, 117, 89, 255));
        var matTronco = GetMaterial("M_Tronco", new Color32(120, 81, 45, 255));
        var matResiduo = GetMaterial("M_Residuo", Color.white);
        var matTapa = GetMaterial("M_Residuo_Tapa", new Color32(110, 190, 230, 255));

        var level = new GameObject("Nivel_Prototipo");
        Undo.RegisterCreatedObjectUndo(level, "Nivel del prototipo");

        float left = U(0), right = U(800), width = right - left, mid = (left + right) / 2f;

        // suelo (verde claro) con franja de césped (verde oscuro)
        var suelo = Prim(PrimitiveType.Cube, "Suelo", level.transform, new Vector3(mid, -1.5f, 0), new Vector3(width + 4f, 3f, 3f), matSuelo);
        var cesped = Prim(PrimitiveType.Cube, "Cesped", level.transform, new Vector3(mid, -0.17f, 0), new Vector3(width + 4f, 0.34f, 3.02f), matCesped);
        Object.DestroyImmediate(cesped.GetComponent<Collider>());

        // paredes invisibles en los bordes (como el límite de pantalla del prototipo)
        foreach (var x in new[] { left - 0.5f, right + 0.5f })
        {
            var wall = Prim(PrimitiveType.Cube, "Limite", level.transform, new Vector3(x, 5f, 0), new Vector3(1f, 12f, 3f), null);
            Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
        }

        // troncos (obstáculos estáticos)
        int i = 1;
        foreach (var r in TroncosPx)
        {
            var pos = new Vector3(U(r.x + r.width / 2f), H(r.y + r.height / 2f), 0);
            var size = new Vector3(r.width / PxPerMeter, r.height / PxPerMeter, 1.2f);
            var tronco = Prim(PrimitiveType.Cylinder, $"Tronco_{i++:00}", level.transform, pos, Vector3.one, matTronco);
            tronco.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            tronco.transform.localScale = new Vector3(size.x, size.z / 2f, size.y);
            Object.DestroyImmediate(tronco.GetComponent<Collider>());
            tronco.AddComponent<BoxCollider>();
            tronco.AddComponent<Obstaculo>();
        }

        // residuos (10)
        i = 1;
        foreach (var p in ResiduosPx)
        {
            var pos = new Vector3(U(p.x + 10f), H(p.y + 12.5f), 0);
            var res = new GameObject($"Residuo_{i++:00}");
            res.transform.SetParent(level.transform);
            res.transform.position = pos;
            var col = res.AddComponent<BoxCollider>();
            col.size = new Vector3(0.5f, 0.65f, 0.5f);
            col.isTrigger = true;
            res.AddComponent<Residuo>();
            var botella = Prim(PrimitiveType.Cylinder, "Botella", res.transform, pos, new Vector3(0.22f, 0.24f, 0.22f), matResiduo);
            Object.DestroyImmediate(botella.GetComponent<Collider>());
            var tapa = Prim(PrimitiveType.Cylinder, "Tapa", res.transform, pos + Vector3.up * 0.3f, new Vector3(0.11f, 0.06f, 0.11f), matTapa);
            Object.DestroyImmediate(tapa.GetComponent<Collider>());
        }

        // jugador en la posición inicial
        Undo.RecordObject(player.transform, "Posición inicial");
        player.transform.position = new Vector3(0f, 0.05f, 0f);

        // GameManager con el HUD
        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>().player = player;
        Undo.RegisterCreatedObjectUndo(gm, "GameManager");

        // cámara con fondo azul cielo
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Cámara");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(176, 224, 230, 255);
            if (cam.orthographic) cam.orthographicSize = 4f;
            var follow = GetOrAdd<CameraFollow>(cam.gameObject);
            follow.target = player.transform;
            follow.offset = new Vector3(0f, 2f, -10f);
            cam.transform.position = player.transform.position + follow.offset;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(level.scene);
        Selection.activeGameObject = level;
        GuardianResiduos.Apply();     // residuos 3D (botella, lata, papel) en lugar de los cilindros provisionales
        Debug.Log("Nivel del prototipo creado: 10 residuos y 2 troncos. Pulsa Play.");
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material GetMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
