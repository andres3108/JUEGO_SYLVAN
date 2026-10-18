using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Añade la Guardiana como segundo personaje jugable y la ventana de selección al iniciar.
//  - Importa Assets/Guardian/Guardiana/Guardiana_del_Bosque.fbx (rig genérico, avatar, bucles) con un
//    material URP con su textura, y un Animator Override Controller que usa sus propias animaciones
//    con los mismos estados y parámetros que el Guardián.
//  - La coloca en la escena junto al Guardián (mismas físicas y controles) y crea "SeleccionPersonaje"
//    con los retratos de Assets/Guardian/Retratos.
// Menú: Guardianes del Bosque > Añadir selección de personaje
public static class GuardianSeleccion
{
    const string Folder = "Assets/Guardian/Guardiana";
    const string FbxPath = Folder + "/Guardiana_del_Bosque.fbx";
    const string TexPath = Folder + "/Guardiana_tex.png";
    const string MatPath = Folder + "/M_Guardiana.mat";
    const string OverridePath = Folder + "/Guardiana.overrideController";
    const string BaseController = "Assets/Animations/Guardian.controller";
    const string RetratoGuardian = "Assets/Guardian/Retratos/Retrato_Guardian.png";
    const string RetratoGuardiana = "Assets/Guardian/Retratos/Retrato_Guardiana.png";
    static readonly string[] LoopClips = { "Idle", "Run", "Slow", "Walk" };

    [MenuItem("Guardianes del Bosque/Añadir selección de personaje")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var guardian = Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include)
            .FirstOrDefault(g => g.name == "Guardian_del_Bosque");
        if (guardian == null) { Debug.LogWarning("No se encontró al Guardián en la escena."); return; }

        // ---------------- importar la Guardiana
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null) { Debug.LogWarning("No se encontró " + FbxPath); return; }
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = importer.defaultClipAnimations;
        foreach (var c in clips) { c.name = c.name.Split('|').Last(); c.loopTime = LoopClips.Contains(c.name); }
        importer.clipAnimations = clips;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Guardiana"), Material());
        importer.SaveAndReimport();

        // animaciones propias de la Guardiana sobre el mismo Animator del Guardián
        var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseController);
        var ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OverridePath);
        if (ov == null) { ov = new AnimatorOverrideController(baseCtrl); AssetDatabase.CreateAsset(ov, OverridePath); }
        ov.runtimeAnimatorController = baseCtrl;
        var own = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>().Where(a => !a.name.StartsWith("__preview")).ToArray();
        var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var orig in baseCtrl.animationClips.Distinct())
            pairs.Add(new(orig, own.FirstOrDefault(a => a.name == orig.name)));
        ov.ApplyOverrides(pairs);
        EditorUtility.SetDirty(ov);

        // ---------------- Guardiana en la escena, con los mismos componentes que el Guardián
        var old = Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include).FirstOrDefault(g => g.name == "Guardiana_del_Bosque");
        if (old) Undo.DestroyObjectImmediate(old.gameObject);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var gna = (GameObject)PrefabUtility.InstantiatePrefab(model);
        gna.name = "Guardiana_del_Bosque";
        Undo.RegisterCreatedObjectUndo(gna, "Guardiana");
        gna.transform.SetPositionAndRotation(guardian.transform.position, guardian.transform.rotation);

        var anim = gna.GetComponent<Animator>();
        if (anim == null) anim = gna.AddComponent<Animator>();
        anim.runtimeAnimatorController = ov;
        anim.avatar = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Avatar>().FirstOrDefault();
        anim.applyRootMotion = false;
        foreach (var src in new Component[] { guardian.GetComponent<CapsuleCollider>(), guardian.GetComponent<Rigidbody>(), guardian })
        {
            UnityEditorInternal.ComponentUtility.CopyComponent(src);
            var dst = gna.GetComponent(src.GetType());
            if (dst) UnityEditorInternal.ComponentUtility.PasteComponentValues(dst);
            else UnityEditorInternal.ComponentUtility.PasteComponentAsNew(gna);
        }

        // ---------------- ventana de selección
        var sel = Object.FindAnyObjectByType<SeleccionPersonaje>();
        if (sel == null)
        {
            sel = new GameObject("SeleccionPersonaje").AddComponent<SeleccionPersonaje>();
            Undo.RegisterCreatedObjectUndo(sel.gameObject, "Selección");
        }
        Undo.RecordObject(sel, "Selección");
        sel.opciones = new[]
        {
            new SeleccionPersonaje.Opcion { nombre = "Sylvan", personaje = guardian.gameObject, retrato = Sprite(RetratoGuardian) },
            new SeleccionPersonaje.Opcion { nombre = "Maya", personaje = gna, retrato = Sprite(RetratoGuardiana) },
        };
        EditorUtility.SetDirty(sel);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gna.scene);
        Debug.Log("Selección de personaje lista: Guardián y Guardiana.");
    }

    static Material Material()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MatPath); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        mat.shader = shader;
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Sprite Sprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
