/*
 * AKLD_SOTemplate_Generator.cs
 * Created by Lautaro Dichio (ldichio.com.ar) | Unity + Wwise Tools
 * 
 * PURPOSE
 * This editor utility automatically generates strongly-typed C# methods 
 * for all entries inside an `AKLD_SOTemplate` asset (Events, RTPCs, Switches, States).
 * The goal is to replace string-based lookups with autocompletable method calls, 
 * giving programmers a safer and faster way to access audio content.
 * 
 * WHY
 * - 🧑‍💻 For programmers: no need to guess names or type strings manually.
 *   Just call the generated methods with full IntelliSense support.
 * - 🎚️ For audio teams: mappings can be updated directly in the ScriptableObject 
 *   without touching code, keeping workflows flexible and safe.
 * - 🔄 For collaboration: improves communication between audio designers and developers 
 *   by separating code logic from asset references.
 * 
 * HOW
 * Use the Unity top menu: **Tools → AKLD → Generate Autocomplete**
 * - "All" → regenerates from every `AKLD_SOTemplate` asset in the project
 * - "Selection" → regenerates from the currently selected template
 * 
 * The generated file is placed in:  
 * `…/Scripts/Generated/AKLD_SOTemplate_<TemplateName>_Auto.cs`
 * 
 * NOTE
 * This script runs in the Editor only (`#if UNITY_EDITOR`) and 
 * should not be included in build players.
 */


#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class AKLD_SOTemplate_Generator
{
    // ───────────────────────────────────────────────
    // Menú
    // ───────────────────────────────────────────────
    [MenuItem("Tools/AKLD/Generate Autocomplete (All)")]
    public static void GenerateFromAllTemplates()
    {
        string[] guids = AssetDatabase.FindAssets("t:AKLD_SOTemplate");
        int count = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<AKLD_SOTemplate>(path);
            if (so == null) continue;

            var sb = BuildHeader();
            AppendTemplate(sb, so);
            var outputPath = GetGeneratedFilePath(so);
            WriteAndImport(outputPath, sb);
            count++;
        }

        Debug.Log($"[AKLD] Generated autocomplete for {count} template(s) in /Generated.");
    }

    [MenuItem("Tools/AKLD/Generate Autocomplete (Selection)")]
    public static void GenerateFromSelection()
    {
        var so = Selection.activeObject as AKLD_SOTemplate;
        if (so == null)
        {
            Debug.LogWarning("[AKLD] Select an AKLD_SOTemplate asset to generate from.");
            return;
        }

        var sb = BuildHeader();
        AppendTemplate(sb, so);
        var outputPath = GetGeneratedFilePath(so);
        WriteAndImport(outputPath, sb);
        Debug.Log($"[AKLD] Autocomplete generated for '{so.name}' at: {outputPath}");
    }

    // ───────────────────────────────────────────────
    // Output: .../Scripts/Generated/AKLD_SOTemplate_<TemplateName>_Auto.cs
    // ───────────────────────────────────────────────
    private static string GetThisScriptPath()
    {
        var guids = AssetDatabase.FindAssets("AKLD_SOTemplate_Generator t:MonoScript");
        return (guids != null && guids.Length > 0) ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
    }

    private static void CreateFolderRecursive(string fullPath)
    {
        var parts = fullPath.Split('/');
        if (parts.Length == 0) return;
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // Mantengo el método original por compat (no se usa en este flujo nuevo).
    private static string GetGeneratedFilePath()
    {
        // …/Scripts/Editor/AKLD_SOTemplate_Generator.cs -> …/Scripts/Generated/AKLD_SOTemplate_Auto.cs
        var scriptPath = GetThisScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
            scriptPath = "Assets/WwiseToolsLD/Scripts/Editor/AKLD_SOTemplate_Generator.cs"; // fallback

        var editorDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
        var scriptsDir = Path.GetDirectoryName(editorDir).Replace("\\", "/");
        var genDir = $"{scriptsDir}/Generated";
        CreateFolderRecursive(genDir);
        return $"{genDir}/AKLD_SOTemplate_Auto.cs";
    }

    // NUEVO: path por template (cambio mínimo para permitir múltiples archivos)
    private static string GetGeneratedFilePath(AKLD_SOTemplate so)
    {
        var scriptPath = GetThisScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
            scriptPath = "Assets/WwiseToolsLD/Scripts/Editor/AKLD_SOTemplate_Generator.cs"; // fallback

        var editorDir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
        var scriptsDir = Path.GetDirectoryName(editorDir).Replace("\\", "/");
        var genDir = $"{scriptsDir}/Generated";
        CreateFolderRecursive(genDir);

        var safe = SanitizeIdentifier(so.name);
        return $"{genDir}/AKLD_SOTemplate_{safe}_Auto.cs";
    }

    // ───────────────────────────────────────────────
    // Codegen
    // ───────────────────────────────────────────────
    private static StringBuilder BuildHeader()
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* AUTO-GENERATED FILE — do not edit manually");
        sb.AppendLine(" * Re-generate via Tools/AKLD menu. */");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("public partial class AKLD_SOTemplate");
        sb.AppendLine("{");
        return sb;
    }

    private static void AppendTemplate(StringBuilder sb, AKLD_SOTemplate so)
    {
        AppendSection(sb, so, so.eventComponents, "AK.Wwise.Event", "GetEventComponent", "Events");
        AppendSection(sb, so, so.rtpcComponents, "AK.Wwise.RTPC", "GetRTPCComponent", "RTPCs");
        AppendSection(sb, so, so.switchComponents, "AK.Wwise.Switch", "GetSwitchComponent", "Switches");
        AppendSection(sb, so, so.stateComponents, "AK.Wwise.State", "GetStateComponent", "States");
    }

    private static void AppendSection<T>(StringBuilder sb, AKLD_SOTemplate so,
        List<T> list, string typeName, string getter, string title) where T : class
    {
        if (list == null || list.Count == 0) return;

        sb.AppendLine($"\t// {title} — from asset: {so.name}");
        foreach (var item in list)
        {
            if (item == null) continue;

            var nameField = item.GetType().GetField("componentName");
            if (nameField == null) continue;

            var rawName = nameField.GetValue(item) as string;
            if (string.IsNullOrWhiteSpace(rawName)) continue;

            var id = SanitizeIdentifier(rawName);

            switch (title)
            {
                case "Events":
                    // (GameObject emitter)
                    sb.AppendLine($"\tpublic void {id}(GameObject emitter) => {getter}(\"{rawName}\")?.Post(emitter);");
                    break;

                case "RTPCs":
                    // GLOBAL + overload retro-compat (acepta GO pero lo ignora)
                    // 1) Global puro
                    sb.AppendLine($"\tpublic void {id}(float value)");
                    sb.AppendLine("\t{");
                    sb.AppendLine($"\t\tvar _rtpc = {getter}(\"{rawName}\");");
                    sb.AppendLine($"\t\tif (_rtpc == null) {{ Debug.LogWarning(\"[AKLD] RTPC not found: {rawName}\"); return; }}");
                    sb.AppendLine("\t\tAkSoundEngine.SetRTPCValue(_rtpc.Id, value);");
                    sb.AppendLine("\t}");
                    // 2) Compat: firma con GameObject (se ignora, llama global igual)
                    sb.AppendLine($"\tpublic void {id}(GameObject _, float value) => {id}(value);");
                    break;

                case "Switches":
                    // (GameObject target)
                    sb.AppendLine($"\tpublic void {id}(GameObject target) => {getter}(\"{rawName}\")?.SetValue(target);");
                    break;

                case "States":
                    // Global + overload con GO ignorado (compat)
                    sb.AppendLine($"\tpublic void {id}() => {getter}(\"{rawName}\")?.SetValue();");
                    sb.AppendLine($"\tpublic void {id}(GameObject _) => {getter}(\"{rawName}\")?.SetValue();");
                    break;
            }
        }
        sb.AppendLine();
    }

    private static string SanitizeIdentifier(string name)
    {
        var id = Regex.Replace(name.Trim(), @"[^a-zA-Z0-9_]", "_");
        if (Regex.IsMatch(id, @"^\d")) id = "_" + id; // FIX: ^\d (antes \\d)
        if (string.IsNullOrEmpty(id)) id = "Unnamed";
        return id;
    }

    private static void WriteAndImport(string outputPath, StringBuilder sb)
    {
        sb.AppendLine("}");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(outputPath);
        Debug.Log($"[AKLD] Autocomplete generated at: {outputPath}");
    }
}
#endif
