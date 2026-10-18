using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Letreros de madera de las zonas:
//   Zona 1: "INICIO" al empezar el recorrido y "FINAL ZONA 1" al terminarlo.
//   Zona 2: solo "FINAL ZONA 2" al terminarlo.
//   Zona 3: solo "FINAL ZONA 3" al terminarlo (164 m; el suelo ahí está 0.32 m más abajo).
// Son sprites (Assets/Guardian/Letreros) apoyados en el suelo de la imagen de fondo,
// un poco por detrás del jugador (z > 0) y sin colisionador, así que no estorban al caminar.
// Menú: Guardianes del Bosque > Agregar letreros (Zona 1) / Agregar letrero (Zona 2 / Zona 3)
public static class GuardianLetreros
{
    const string Folder = "Assets/Guardian/Letreros";

    // (nombre, textura, posición, escala): valores ajustados a mano en el Inspector.
    // z = 1.5: detrás del jugador (z = 0) y delante del fondo (z = 25)
    static readonly (string nombre, string tex, Vector3 pos, Vector3 escala)[] LetrerosZona1 =
    {
        ("Letrero_Inicio", "Letrero_Inicio.png", new(1.27f, 0.88f, 1.5f), new(0.2f, 0.2f, 0.28642f)),
        ("Letrero_Final", "Letrero_Final_Zona1.png", new(69.29f, 0.87f, 1.5f), new(0.2085f, 0.2f, 0.26282f)),
    };

    // la Zona 2 mide lo mismo (70 m), así que el letrero final va en el mismo sitio que el de la Zona 1
    static readonly (string nombre, string tex, Vector3 pos, Vector3 escala)[] LetrerosZona2 =
    {
        ("Letrero_Final", "Letrero_Final_Zona2.png", new(69.29f, 0.87f, 1.5f), new(0.2085f, 0.2f, 0.26282f)),
    };

    // mismo sitio respecto al final del recorrido que en las zonas 1 y 2
    static (string nombre, string tex, Vector3 pos, Vector3 escala)[] LetrerosZona3 => new[]
    {
        ("Letrero_Final", "Letrero_Final_Zona3.png",
            new Vector3(GuardianZona3.Largo - 0.71f, 0.87f + GuardianZona3.Suelo(GuardianZona3.Largo - 0.71f), 1.5f),
            new Vector3(0.2085f, 0.2f, 0.26282f)),
    };

    [MenuItem("Guardianes del Bosque/Agregar letreros (Zona 1)")]
    public static void Apply() => Colocar("Zona_1", LetrerosZona1, "INICIO y FINAL ZONA 1");

    [MenuItem("Guardianes del Bosque/Agregar letrero (Zona 2)")]
    public static void ApplyZona2() => Colocar("Zona_2", LetrerosZona2, "FINAL ZONA 2");

    [MenuItem("Guardianes del Bosque/Agregar letrero (Zona 3)")]
    public static void ApplyZona3() => Colocar("Zona_3", LetrerosZona3, "FINAL ZONA 3");

    static void Colocar(string raiz, (string nombre, string tex, Vector3 pos, Vector3 escala)[] letreros, string desc)
    {
        if (EditorApplication.isPlaying) return;
        var zona = GameObject.Find(raiz);
        if (zona == null)
        {
            Debug.LogWarning($"Abre la escena de la zona (con el objeto {raiz}) antes de agregar los letreros.");
            return;
        }

        // con el renderer 2D se ordena por "Order in Layer": el fondo tiene que ir detrás de los letreros
        var fondo = GameObject.Find("Fondo");
        if (fondo)
            foreach (var r in fondo.GetComponentsInChildren<Renderer>())
                if (r.sortingOrder > -100) { r.sortingOrder = -100; EditorUtility.SetDirty(r); }

        var old = zona.transform.Find("Letreros");
        if (old) Object.DestroyImmediate(old.gameObject);
        var cont = new GameObject("Letreros");
        cont.transform.SetParent(zona.transform, false);

        foreach (var l in letreros)
        {
            var sprite = CargarSprite($"{Folder}/{l.tex}");
            if (sprite == null) { Debug.LogWarning($"Falta {Folder}/{l.tex}"); continue; }
            var go = new GameObject(l.nombre);
            go.transform.SetParent(cont.transform, false);
            go.transform.localPosition = l.pos;
            go.transform.localScale = l.escala;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -1;
        }

        EditorSceneManager.MarkSceneDirty(zona.scene);
        Debug.Log($"Letreros colocados en {raiz}: {desc}.");
    }

    // la textura se importa como sprite con el pivote abajo al centro (la base de los postes)
    static Sprite CargarSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        if (imp.textureType != TextureImporterType.Sprite || s.spriteAlignment != (int)SpriteAlignment.BottomCenter
            || imp.mipmapEnabled || imp.maxTextureSize != 1024)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spritePivot = new Vector2(0.5f, 0f);
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 1024;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
