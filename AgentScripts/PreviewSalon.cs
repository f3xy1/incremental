VhsInheritance.VhsSaveSystem.EditorSaveDirectory = System.IO.Path.GetFullPath("Temp/verification/cartoon-preview-save");
System.IO.Directory.CreateDirectory("Temp/verification");
var game = UnityEngine.Object.FindAnyObjectByType<VhsInheritance.VhsGameController>();
game.NewGame();
return new { phase = game.CurrentPhase.ToString(), frame = UnityEngine.Time.frameCount };
