using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Crea la escena del menú principal (Assets/Scenes/MenuPrincipal.unity) y la pone la primera en
// Build Settings, delante de las zonas: MenuPrincipal -> Zona1 -> Zona2.
// Menú: Guardianes del Bosque > Crear menú principal
public static class GuardianMenu
{
    const string EscenaMenu = "Assets/Scenes/MenuPrincipal.unity";
    static readonly string[] Zonas = { "Assets/Scenes/Zona1.unity", "Assets/Scenes/Zona2.unity", "Assets/Scenes/Zona3.unity" };

    [MenuItem("Guardianes del Bosque/Crear menú principal")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // texturas del menú sin compresión agresiva para que se vean nítidas
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Menu" }))
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (imp == null) continue;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;
        c.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var menu = new GameObject("MenuPrincipal").AddComponent<MenuPrincipal>();
        menu.escenaJuego = "Zona1";
        EditorSceneManager.SaveScene(escena, EscenaMenu);

        EditorBuildSettings.scenes = new[] { EscenaMenu }.Concat(Zonas)
            .Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null)
            .Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        Debug.Log("Menú principal creado. Orden del juego: " + string.Join(" -> ", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))));
    }
}
