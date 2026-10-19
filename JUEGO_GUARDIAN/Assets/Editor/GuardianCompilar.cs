using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila el juego (escenas de Build Settings: MenuPrincipal -> Zona1 -> Zona2 -> Zona3) con el nombre,
// la empresa y el icono del juego:
//   Windows (64 bits): <repositorio>/Compilacion/Windows/GuardianesDelBosque.exe   (luego Setup/*.iss = instalador)
//   Android (APK):     <repositorio>/Android/GuardianesDelBosque.apk               (horizontal, controles táctiles)
// Menú: Guardianes del Bosque > Compilar para Windows / Compilar APK para Android
// También desde la línea de comandos: Unity -batchmode -quit -projectPath ... -executeMethod GuardianCompilar.Windows
// (o GuardianCompilar.Android)
public static class GuardianCompilar
{
    public const string Producto = "Guardianes del Bosque";
    public const string Exe = "GuardianesDelBosque.exe";
    public const string Apk = "GuardianesDelBosque.apk";
    public const string Paquete = "com.unad.guardianesdelbosque";
    const string Icono = "Assets/Guardian/Retratos/Retrato_Guardian.png";

    static string Repositorio => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    public static string Carpeta => Path.Combine(Repositorio, "Compilacion", "Windows");
    public static string CarpetaAndroid => Path.Combine(Repositorio, "Android");

    [MenuItem("Guardianes del Bosque/Compilar para Windows")]
    public static void Windows()
    {
        Comunes();
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        if (Directory.Exists(Carpeta)) Directory.Delete(Carpeta, true);   // compilación limpia
        Directory.CreateDirectory(Carpeta);
        Compilar(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, Path.Combine(Carpeta, Exe), Carpeta);
    }

    [MenuItem("Guardianes del Bosque/Compilar APK para Android")]
    public static void Android()
    {
        Comunes();
        var android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, Paquete);
        PlayerSettings.Android.bundleVersionCode = 1;
        // horizontal (se gira solo entre las dos posiciones horizontales)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        // 64 bits (lo piden los celulares actuales y Google Play): IL2CPP + ARM64
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;   // APK para instalar directamente en el celular
        Directory.CreateDirectory(CarpetaAndroid);
        Compilar(BuildTarget.Android, BuildTargetGroup.Android, Path.Combine(CarpetaAndroid, Apk), CarpetaAndroid);
    }

    static void Comunes()
    {
        PlayerSettings.companyName = "UNAD";
        PlayerSettings.productName = Producto;
        PlayerSettings.bundleVersion = "1.0";
        var icono = AssetDatabase.LoadAssetAtPath<Texture2D>(Icono);
        if (icono) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);
    }

    static void Compilar(BuildTarget destino, BuildTargetGroup grupo, string archivo, string carpeta)
    {
        if (EditorUserBuildSettings.activeBuildTarget != destino)
            EditorUserBuildSettings.SwitchActiveBuildTarget(grupo, destino);   // la primera vez reimporta las texturas

        var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = archivo,
            target = destino,
            options = BuildOptions.None,
        });
        var s = r.summary;
        Debug.Log($"COMPILACION {destino} {s.result}: {s.totalErrors} errores, {s.totalSize / (1024f * 1024f):F1} MB, " +
                  $"{s.totalTime.TotalSeconds:F0} s -> {carpeta}  (escenas: {string.Join(", ", escenas.Select(Path.GetFileNameWithoutExtension))})");
        if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
