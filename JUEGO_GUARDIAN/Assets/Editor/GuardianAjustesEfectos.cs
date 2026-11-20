using UnityEditor;

// Menú: Guardianes del Bosque > Ajustes de efectos
// Selecciona (y crea si falta) Assets/Resources/AjustesEfectos.asset para editarlo en el Inspector.
public static class GuardianAjustesEfectos
{
    const string Ruta = "Assets/Resources/AjustesEfectos.asset";

    [MenuItem("Guardianes del Bosque/Ajustes de efectos")]
    public static void Abrir()
    {
        var a = AssetDatabase.LoadAssetAtPath<AjustesEfectos>(Ruta);
        if (a == null)
        {
            a = UnityEngine.ScriptableObject.CreateInstance<AjustesEfectos>();
            AssetDatabase.CreateAsset(a, Ruta);
            AssetDatabase.SaveAssets();
        }
        Selection.activeObject = a;
        EditorGUIUtility.PingObject(a);
    }
}
