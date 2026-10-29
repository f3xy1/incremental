VhsInheritance.VhsSaveSystem.EditorSaveDirectory = System.IO.Path.GetFullPath("Temp/verification/cartoon-preview-save");
var game = UnityEngine.Object.FindAnyObjectByType<VhsInheritance.VhsGameController>();
game.NewGame();
var data = game.CurrentSave;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
foreach (var shelf in data.shelves)
{
    int count = shelf.genre == VhsInheritance.CassetteGenre.Action ? 14 : 8;
    for (int i = 0; i < count; i++) shelf.slots[i] = new VhsInheritance.ShelfSlotData { occupied = true,
        cassette = new VhsInheritance.CassetteData { id = System.Guid.NewGuid().ToString("N"), genre = shelf.genre } };
}
while (data.pile.Count < 40) data.pile.Add(new VhsInheritance.CassetteData { id = System.Guid.NewGuid().ToString("N"), genre = (VhsInheritance.CassetteGenre)(data.pile.Count % 2) });
game.GetType().GetMethod("RefreshAllShelfVisuals", flags).Invoke(game, null);
game.GetType().GetMethod("RefreshPileVisual", flags).Invoke(game, null);
game.StartDay();
data.totalMoney = 48;
data.visitors[0].hasReadOrder = true;
for (int i = 0; i < 5; i++) game.GetType().GetMethod("AddVisitor", flags).Invoke(game, null);
data.dayTimeRemaining = 302;
return new { phase = game.CurrentPhase.ToString(), frame = UnityEngine.Time.frameCount };
