UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions {
    scenes = new[] { VhsInheritance.Editor.VhsPrototypeBuilder.ScenePath },
    locationPathName = "Temp/VhsPlayerSmoke/VhsInheritance.exe",
    target = UnityEditor.BuildTarget.StandaloneWindows64,
    options = UnityEditor.BuildOptions.None,
    extraScriptingDefines = new[] { "VHS_PLAYER_SMOKE" }
});
return new { result = report.summary.result.ToString(), errors = report.summary.totalErrors };
