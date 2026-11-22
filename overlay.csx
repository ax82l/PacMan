using UndertaleModLib.Compiler;

EnsureDataLoaded();

// ---------- shared by all 4 ghosts: target tile + line to it ----------
string common = @"
var _dbgOn = true;
if (variable_global_exists(""ghostDbg""))
{
    _dbgOn = global.ghostDbg;
}
if (_dbgOn && scared == false && frozen == false && state == ""active"")
{
    var _dbgC = myColor & 16777215;
    var _dbgX = targetTileX * 8 + 4;
    var _dbgY = max(targetTileY * 8 + 4, 4);
    draw_set_alpha(0.85);
    draw_set_color(_dbgC);
    draw_line(x, y, _dbgX, _dbgY);
    draw_rectangle(_dbgX - 3, _dbgY - 3, _dbgX + 3, _dbgY + 3, false);
    draw_set_alpha(1);
    draw_set_color(c_white);
}
";

// ---------- Inky: how the target is derived (chase phase only) ----------
// yellow : Pac-Man -> pivot (2 tiles ahead of Pac-Man)
// red    : Blinky -> pivot -> target (the vector is doubled)
// white dot = pivot. pivot = (target + Blinky tile) / 2
string inkyExtra = @"
if (_dbgOn && (o_Game.changes % 2) == 1 && scared == false && frozen == false && state == ""active"")
{
    var _pvX = ((targetTileX + o_Blinky.tileX) / 2) * 8 + 4;
    var _pvY = ((targetTileY + o_Blinky.tileY) / 2) * 8 + 4;
    var _pcTX = o_Player.tileX;
    if (o_Player.mirror > 0)
    {
        _pcTX = 31 - _pcTX;
    }
    var _pcX = _pcTX * 8 + 4;
    var _pcY = o_Player.tileY * 8 + 4;
    var _blX = o_Blinky.tileX * 8 + 4;
    var _blY = o_Blinky.tileY * 8 + 4;
    var _tgX = targetTileX * 8 + 4;
    var _tgY = max(targetTileY * 8 + 4, 4);
    draw_set_alpha(0.9);
    draw_set_color(c_yellow);
    draw_line(_pcX, _pcY, _pvX, _pvY);
    draw_set_color(c_red);
    draw_line(_blX, _blY, _tgX, _tgY);
    draw_set_color(c_white);
    draw_circle(_pvX, _pvY, 3, false);
    draw_set_alpha(1);
}
";

// ---------- Clyde: 8-tile radius circle (chase phase only) ----------
// Pac-Man outside the circle -> Clyde chases him.
// Pac-Man inside the circle  -> Clyde runs to his corner (circle gets shaded).
string clydeExtra = @"
if (_dbgOn && (o_Game.changes % 2) == 1 && scared == false && frozen == false && state == ""active"")
{
    var _cyX = tileX * 8 + 4;
    var _cyY = tileY * 8 + 4;
    var _cyD2 = power(o_Player.tileX - tileX, 2) + power(o_Player.tileY - tileY, 2);
    draw_set_color(myColor & 16777215);
    if (_cyD2 < 64)
    {
        draw_set_alpha(0.25);
        draw_circle(_cyX, _cyY, 64, false);
    }
    draw_set_alpha(0.85);
    draw_circle(_cyX, _cyY, 64, true);
    draw_set_alpha(1);
    draw_set_color(c_white);
}
";

// ---------- Blinky only: ON/OFF indicator (green = on, gray = off) ----------
string blinkyExtra = @"
draw_set_alpha(1);
if (_dbgOn)
{
    draw_set_color(c_lime);
}
else
{
    draw_set_color(c_gray);
}
draw_rectangle(3, 3, 11, 11, false);
draw_set_color(c_white);
";

// ---------- toggle: tap top-left corner (x<48, y<20) or press G ----------
string toggle = @"
if (!variable_global_exists(""ghostDbg""))
{
    global.ghostDbg = true;
}
if (keyboard_check_pressed(ord(""G"")) || (device_mouse_check_button_pressed(0, mb_left) && device_mouse_x(0) < 48 && device_mouse_y(0) < 20))
{
    global.ghostDbg = !global.ghostDbg;
}
";

var group = new CodeImportGroup(Data);
group.QueueAppend("gml_Object_o_Blinky_Draw_0", common + blinkyExtra);
group.QueueAppend("gml_Object_o_Pinky_Draw_0", common);
group.QueueAppend("gml_Object_o_Inky_Draw_0", common + inkyExtra);
group.QueueAppend("gml_Object_o_Clyde_Draw_0", common + clydeExtra);
group.QueueAppend("gml_Object_o_Game_Step_2", toggle);
group.Import();

ScriptMessage("Overlay v2 injected.");
