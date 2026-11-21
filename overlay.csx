using UndertaleModLib.Compiler;

EnsureDataLoaded();

// Every ghost draws its own target tile + a line to it, in its own color.
// Only while "active" (not in pen / frightened / frozen), because only then
// targetTileX/Y is meaningful (frightened ghosts pick random targets).
string common = @"
if (scared == false && frozen == false && state == ""active"")
{
    var _c = myColor & 16777215;
    var _tx = targetTileX * 8 + 4;
    var _ty = max(targetTileY * 8 + 4, 4);
    draw_set_alpha(0.85);
    draw_set_color(_c);
    draw_line(x, y, _tx, _ty);
    draw_rectangle(_tx - 3, _ty - 3, _tx + 3, _ty + 3, false);
    draw_set_alpha(1);
    draw_set_color(c_white);
}
";

// Inky: target = 2 * pivot - Blinky, so pivot = (target + Blinky tile) / 2.
// Shown only in chase mode (odd phase), as a white line Blinky -> pivot -> target.
string inkyExtra = @"
if ((o_Game.changes % 2) == 1 && scared == false && state == ""active"")
{
    var _px = ((targetTileX + o_Blinky.tileX) / 2) * 8 + 4;
    var _py = ((targetTileY + o_Blinky.tileY) / 2) * 8 + 4;
    draw_set_alpha(0.6);
    draw_set_color(c_white);
    draw_line(o_Blinky.x, o_Blinky.y, _px, _py);
    draw_circle(_px, _py, 3, true);
    draw_set_alpha(1);
}
";

var group = new CodeImportGroup(Data);
foreach (string g in new string[] { "Blinky", "Pinky", "Inky", "Clyde" })
{
    string code = common + (g == "Inky" ? inkyExtra : "");
    group.QueueAppend("gml_Object_o_" + g + "_Draw_0", code);
}
group.Import();

ScriptMessage("Overlay injected into 4 ghost Draw events.");
