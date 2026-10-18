using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Mantiene al Guardián listo para animarse cuando se reemplaza su FBX:
//  - el FBX crea su avatar (sin avatar el Animator no mueve los huesos),
//  - la lista de animaciones incluye las nuevas (por ejemplo Walk) y las de ciclo van en bucle,
//  - el Animator de la escena usa ese avatar y el controlador Guardian.
// Se ejecuta solo al compilar si algo falta, y desde el menú Guardianes del Bosque > Actualizar animaciones.
[InitializeOnLoad]
public static class GuardianAnimaciones
{
    const string FbxPath = "Assets/Guardian_del_Bosque.fbx";
    const string ControllerPath = "Assets/Animations/Guardian.controller";
    static readonly string[] LoopClips = { "Idle", "Run", "Slow", "Walk" };

    static GuardianAnimaciones()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null) return;
            bool needsImport = importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                               || importer.clipAnimations.Length < importer.defaultClipAnimations.Length;
            if (needsImport || SceneAnimatorNeedsFix())
                Update();
        };
    }

    [MenuItem("Guardianes del Bosque/Actualizar animaciones")]
    public static void Update()
    {
        var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null) return;

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = importer.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.name = c.name.Split('|').Last();
            c.loopTime = LoopClips.Contains(c.name);
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        FixSceneAnimator();
        Debug.Log("Guardián: avatar y animaciones actualizados -> " + string.Join(", ", clips.Select(c => c.name)));
    }

    static Avatar FbxAvatar() =>
        AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Avatar>().FirstOrDefault();

    static Animator SceneGuardian() =>
        Object.FindObjectsByType<GuardianController>()
              .Select(g => g.GetComponentInChildren<Animator>()).FirstOrDefault(a => a != null);

    static bool SceneAnimatorNeedsFix()
    {
        var anim = SceneGuardian();
        return anim != null && (anim.avatar == null || anim.runtimeAnimatorController == null);
    }

    static void FixSceneAnimator()
    {
        var anim = SceneGuardian();
        if (anim == null) return;
        Undo.RecordObject(anim, "Avatar del Guardián");
        anim.avatar = FbxAvatar();
        if (anim.runtimeAnimatorController == null)
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        anim.applyRootMotion = false;
        EditorUtility.SetDirty(anim);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(anim.gameObject.scene);
    }
}
