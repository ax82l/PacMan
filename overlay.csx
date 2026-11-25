using System.Text;
using UndertaleModLib.Compiler;

EnsureDataLoaded();

string[] G = new string[] { "Blinky", "Pinky", "Inky", "Clyde" };

// =====================================================================
// 1) TARGET OVERLAY (drawn in each ghost's Draw event, room coordinates)
// =====================================================================
// A pinned ghost is also drawn while frightened (it has a target then),
// with a white X on the pinned tile. Eaten ghosts keep the pen target.
string common = @"
var _dbgOn = true;
if (variable_global_exists(""ghostDbg""))
{
    _dbgOn = global.ghostDbg;
}
if (_dbgOn && (scared == false || gcPin == true) && frozen == false && state == ""active"")
{
    var _dbgC = myColor & 16777215;
    var _dbgX = targetTileX * 8 + 4;
    var _dbgY = max(targetTileY * 8 + 4, 4);
    draw_set_alpha(0.85);
    draw_set_color(_dbgC);
    draw_line(x, y, _dbgX, _dbgY);
    draw_rectangle(_dbgX - 3, _dbgY - 3, _dbgX + 3, _dbgY + 3, false);
    if (gcPin == true && chomped == false)
    {
        draw_set_color(c_white);
        draw_line(_dbgX - 5, _dbgY - 5, _dbgX + 5, _dbgY + 5);
        draw_line(_dbgX - 5, _dbgY + 5, _dbgX + 5, _dbgY - 5);
    }
    draw_set_alpha(1);
    draw_set_color(c_white);
}
";

// Inky derivation (only while Inky is effectively in CHASE and not pinned).
string inkyExtra = @"
if (_dbgOn && gcPin == false && (gcForce == 2 || (gcForce == 0 && (o_Game.changes % 2) == 1)) && scared == false && frozen == false && state == ""active"")
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

// Clyde 8-tile circle (only while Clyde is effectively in CHASE and not pinned).
string clydeExtra = @"
if (_dbgOn && gcPin == false && (gcForce == 2 || (gcForce == 0 && (o_Game.changes % 2) == 1)) && scared == false && frozen == false && state == ""active"")
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

// =====================================================================
// 2) MENU LOGIC (appended to o_Game Step_2 = End Step)
// =====================================================================
// Per-ghost: apply the chosen mode when the menu closes.
string applyT = @"
var _gcM@@I@@ = global.gcMode[@@I@@];
var _gcP@@I@@ = global.gcPrev[@@I@@];
with (o_@@N@@)
{
    if (_gcM@@I@@ != _gcP@@I@@ && (_gcM@@I@@ == 1 || _gcM@@I@@ == 2 || _gcM@@I@@ == 3))
    {
        change = true;
    }
    if (_gcP@@I@@ == 3 && _gcM@@I@@ != 3)
    {
        if (gcByUs == true && scared == true)
        {
            scared = false;
            o_Game.scaredGhosts += -1;
        }
        gcByUs = false;
    }
    if (_gcM@@I@@ == 4 && state == ""active"" && chomped == false)
    {
        chomped = true;
        scared = false;
        interactive = false;
        o_Game.chompedGhosts += 1;
        o_Game.scaredGhosts += 1;
    }
}
global.gcPrev[@@I@@] = _gcM@@I@@;
if (_gcM@@I@@ == 4)
{
    global.gcMode[@@I@@] = 0;
    global.gcPrev[@@I@@] = 0;
}
";

// Per-ghost, every running frame: enforce forced mode + copy the pinned target.
string stickyT = @"
with (o_@@N@@)
{
    gcForce = 0;
    if (global.gcMode[@@I@@] == 1)
    {
        gcForce = 1;
    }
    if (global.gcMode[@@I@@] == 2)
    {
        gcForce = 2;
    }
    if (global.gcMode[@@I@@] == 3 && chomped == false && scared == false)
    {
        scared = true;
        gcByUs = true;
        o_Game.scaredGhosts += 1;
    }
    gcPin = false;
    if (global.gcPinOn[@@I@@] == 1)
    {
        gcPin = true;
        gcPinX = global.gcPinX[@@I@@];
        gcPinY = global.gcPinY[@@I@@];
    }
}
";

// Page 0, rows 1..4: cycle the ghost mode.
string modeCaseT = @"
                    case @@R@@:
                        if (_gcRight)
                        {
                            global.gcMode[@@I@@] = (global.gcMode[@@I@@] + 1) % 5;
                        }
                        else
                        {
                            global.gcMode[@@I@@] = (global.gcMode[@@I@@] + 4) % 5;
                        }
                        global.ghostCheat = true;
                        break;
";

// Page 1: three rows per ghost (PIN on/off, X, Y).
string pinCaseT = @"
                    case @@B0@@:
                        if (global.gcPinOn[@@I@@] == 1)
                        {
                            global.gcPinOn[@@I@@] = 0;
                        }
                        else
                        {
                            global.gcPinOn[@@I@@] = 1;
                            global.gcPinX[@@I@@] = 14;
                            global.gcPinY[@@I@@] = 17;
                            with (o_@@N@@)
                            {
                                global.gcPinX[@@I@@] = targetTileX;
                                global.gcPinY[@@I@@] = targetTileY;
                            }
                        }
                        global.ghostCheat = true;
                        break;
                    case @@B1@@:
                        if (_gcRight)
                        {
                            global.gcPinX[@@I@@] = min(31, global.gcPinX[@@I@@] + global.gcCoordStep);
                        }
                        else
                        {
                            global.gcPinX[@@I@@] = max(0, global.gcPinX[@@I@@] - global.gcCoordStep);
                        }
                        global.ghostCheat = true;
                        break;
                    case @@B2@@:
                        if (_gcRight)
                        {
                            global.gcPinY[@@I@@] = min(36, global.gcPinY[@@I@@] + global.gcCoordStep);
                        }
                        else
                        {
                            global.gcPinY[@@I@@] = max(-3, global.gcPinY[@@I@@] - global.gcCoordStep);
                        }
                        global.ghostCheat = true;
                        break;
";

var applyAll = new StringBuilder();
var stickyAll = new StringBuilder();
var modeCases = new StringBuilder();
var pinCases = new StringBuilder();
for (int i = 0; i < 4; i++)
{
    applyAll.Append(applyT.Replace("@@N@@", G[i]).Replace("@@I@@", i.ToString()));
    stickyAll.Append(stickyT.Replace("@@N@@", G[i]).Replace("@@I@@", i.ToString()));
    modeCases.Append(modeCaseT.Replace("@@R@@", (i + 1).ToString()).Replace("@@I@@", i.ToString()));
    pinCases.Append(pinCaseT.Replace("@@N@@", G[i]).Replace("@@I@@", i.ToString())
        .Replace("@@B0@@", (i * 3).ToString())
        .Replace("@@B1@@", (i * 3 + 1).ToString())
        .Replace("@@B2@@", (i * 3 + 2).ToString()));
}

// ---------- teleport page (0 = Pac-Man, 1..4 = ghosts) ----------
string[] O = new string[] { "o_Player", "o_Blinky", "o_Pinky", "o_Inky", "o_Clyde" };

var tpHere = new StringBuilder();
var tpNow = new StringBuilder();
var tpFace = new StringBuilder();
for (int k = 0; k < 5; k++)
{
    tpHere.Append("if (global.gcTpWho == " + k + ")\n{\n    with (" + O[k] + ")\n    {\n        global.gcTpX[" + k + "] = floor(x / 8);\n        global.gcTpY[" + k + "] = floor(y / 8);\n    }\n}\n");
    string stateFix = "";
    if (k > 0)
    {
        stateFix = "        if (state != \"active\")\n        {\n            state = \"active\";\n        }\n";
    }
    tpNow.Append("if (global.gcTpWho == " + k + ")\n{\n    with (" + O[k] + ")\n    {\n        x = global.gcTpX[" + k + "] * 8 + 4;\n        y = global.gcTpY[" + k + "] * 8 + 4;\n        tileX = global.gcTpX[" + k + "];\n        tileY = global.gcTpY[" + k + "];\n" + stateFix + "    }\n}\n");
    tpFace.Append("if (global.gcTpWho == " + k + ")\n{\n    with (" + O[k] + ")\n    {\n        facing = (facing + _gcD) % 4;\n    }\n}\n");
}

string tpCases = @"
                    case 0:
                        if (_gcRight)
                        {
                            global.gcTpWho = (global.gcTpWho + 1) % 5;
                        }
                        else
                        {
                            global.gcTpWho = (global.gcTpWho + 4) % 5;
                        }
                        break;
                    case 1:
                        if (_gcRight)
                        {
                            global.gcTpX[global.gcTpWho] = min(34, global.gcTpX[global.gcTpWho] + global.gcCoordStep);
                        }
                        else
                        {
                            global.gcTpX[global.gcTpWho] = max(-4, global.gcTpX[global.gcTpWho] - global.gcCoordStep);
                        }
                        break;
                    case 2:
                        if (_gcRight)
                        {
                            global.gcTpY[global.gcTpWho] = min(42, global.gcTpY[global.gcTpWho] + global.gcCoordStep);
                        }
                        else
                        {
                            global.gcTpY[global.gcTpWho] = max(-6, global.gcTpY[global.gcTpWho] - global.gcCoordStep);
                        }
                        break;
                    case 3:
" + tpHere.ToString() + @"
                        break;
                    case 4:
" + tpNow.ToString() + @"
                        global.ghostCheat = true;
                        break;
                    case 5:
                        global.gcTpHold[global.gcTpWho] = 1 - global.gcTpHold[global.gcTpWho];
                        global.ghostCheat = true;
                        break;
                    case 6:
" + tpFace.ToString() + @"
                        global.ghostCheat = true;
                        break;
                    case 7:
                        if (global.gcCoordStep == 1)
                        {
                            global.gcCoordStep = 5;
                        }
                        else
                        {
                            global.gcCoordStep = 1;
                        }
                        break;
                    case 8:
                        global.gcPage = 0;
                        break;
";

// hold flags: copied to the instances every frame (read by patched sc_enemy / sc_playermove)
var holdAll = new StringBuilder();
holdAll.Append("with (o_Player)\n{\n    gcHold = false;\n    if (global.gcTpHold[0] == 1)\n    {\n        gcHold = true;\n    }\n}\n");
for (int i = 0; i < 4; i++)
{
    holdAll.Append("with (o_" + G[i] + ")\n{\n    gcHold = false;\n    if (global.gcTpHold[" + (i + 1) + "] == 1 && chomped == false)\n    {\n        gcHold = true;\n    }\n}\n");
}

string closeCode = "global.gcOpen = false;\n" + applyAll.ToString() + "\naudio_resume_all();\nrunning = true;\n";

string jumpCode = @"
global.ghostCheat = true;
global.gcOpen = false;
audio_resume_all();
stageNo = global.gcLevel;
script_execute(sc_difficulty);
powerPellets = 0;
powerpellet = 0;
powerup = 0;
gametime = 0;
changes = 0;
changeTime = 0;
waittimer = 0;
nextLifeDotsCheck = false;
timeline_running = false;
timeline_position = 0;
sc_mazeselect();
timeline_index = gameStart;
timeline_loop = 0;
timeline_running = 1;
";

string step =
@"
if (!variable_global_exists(""gcInit""))
{
    global.gcInit = true;
    global.gcOpen = false;
    global.gcPage = 0;
    global.gcMode = array_create(4, 0);
    global.gcPrev = array_create(4, 0);
    global.gcPinOn = array_create(4, 0);
    global.gcPinX = array_create(4, 0);
    global.gcPinY = array_create(4, 0);
    global.gcCoordStep = 1;
    global.gcTpWho = 0;
    global.gcTpX = array_create(5, 0);
    global.gcTpY = array_create(5, 0);
    global.gcTpHold = array_create(5, 0);
    global.gcStep = 1000;
    global.gcLvStep = 1;
    global.gcLevel = 1;
    if (!variable_global_exists(""ghostDbg""))
    {
        global.ghostDbg = true;
    }
}
var _gcDoClose = false;
if (state == ""playing"" && demo == false)
{
    var _gcTap = device_mouse_check_button_pressed(0, mb_left);
    var _gcX = device_mouse_x_to_gui(0);
    var _gcY = device_mouse_y_to_gui(0);
    if (_gcTap)
    {
        if (_gcX >= 166 && _gcX <= 210 && _gcY >= (y + 18) && _gcY <= (y + 30))
        {
            if (global.gcOpen == false)
            {
                if (running == true && chomping == false)
                {
                    global.gcOpen = true;
                    global.gcPage = 0;
                    global.gcLevel = stageNo;
                    running = false;
                    audio_pause_all();
                }
            }
            else
            {
                _gcDoClose = true;
            }
        }
        else if (global.gcOpen == true)
        {
            var _gcTop = y + 56;
            var _gcRows = 14;
            if (global.gcPage == 2)
            {
                _gcRows = 9;
            }
            if (_gcX >= 10 && _gcX <= 230 && _gcY >= _gcTop && _gcY < (_gcTop + (_gcRows * 13)))
            {
                var _gcRow = floor((_gcY - _gcTop) / 13);
                var _gcRight = false;
                if (_gcX >= 130)
                {
                    _gcRight = true;
                }
                if (global.gcPage == 0)
                {
                    switch (_gcRow)
                    {
                    case 0:
                        global.ghostDbg = !global.ghostDbg;
                        break;
" + modeCases.ToString() + @"
                    case 5:
                        if (_gcRight)
                        {
                            playerLives = min(99, playerLives + 1);
                        }
                        else
                        {
                            playerLives = max(1, playerLives - 1);
                        }
                        global.ghostCheat = true;
                        break;
                    case 6:
                        if (_gcRight)
                        {
                            playerScore = playerScore + global.gcStep;
                        }
                        else
                        {
                            playerScore = max(0, playerScore - global.gcStep);
                        }
                        global.ghostCheat = true;
                        break;
                    case 7:
                        if (global.gcStep == 100)
                        {
                            global.gcStep = 1000;
                        }
                        else if (global.gcStep == 1000)
                        {
                            global.gcStep = 10000;
                        }
                        else if (global.gcStep == 10000)
                        {
                            global.gcStep = 100000;
                        }
                        else
                        {
                            global.gcStep = 100;
                        }
                        break;
                    case 8:
                        if (_gcRight)
                        {
                            global.gcLevel = min(256, global.gcLevel + global.gcLvStep);
                        }
                        else
                        {
                            global.gcLevel = max(1, global.gcLevel - global.gcLvStep);
                        }
                        break;
                    case 9:
                        if (global.gcLvStep == 1)
                        {
                            global.gcLvStep = 5;
                        }
                        else if (global.gcLvStep == 5)
                        {
                            global.gcLvStep = 10;
                        }
                        else if (global.gcLvStep == 10)
                        {
                            global.gcLvStep = 25;
                        }
                        else
                        {
                            global.gcLvStep = 1;
                        }
                        break;
                    case 10:
" + jumpCode + @"
                        break;
                    case 11:
                        global.gcPage = 1;
                        break;
                    case 12:
                        global.gcPage = 2;
                        break;
                    case 13:
                        _gcDoClose = true;
                        break;
                    }
                }
                else if (global.gcPage == 1)
                {
                    switch (_gcRow)
                    {
" + pinCases.ToString() + @"
                    case 12:
                        if (global.gcCoordStep == 1)
                        {
                            global.gcCoordStep = 5;
                        }
                        else
                        {
                            global.gcCoordStep = 1;
                        }
                        break;
                    case 13:
                        global.gcPage = 0;
                        break;
                    }
                }
                else
                {
                    var _gcD = 3;
                    if (_gcRight)
                    {
                        _gcD = 1;
                    }
                    switch (_gcRow)
                    {
" + tpCases + @"
                    }
                }
            }
        }
    }
}
if (_gcDoClose == true)
{
" + closeCode + @"
}
" + holdAll.ToString() + @"
if (running == true)
{
" + stickyAll.ToString() + @"
}
";

// =====================================================================
// 3) MENU DRAWING (appended to o_Game Draw_64 = GUI layer, 240 px wide)
// =====================================================================
string Row(int r, string label, string valueStmt, bool arrows)
{
    int ty = r * 13 + 3;
    string s = "\n" + valueStmt + "\n";
    s += "draw_set_halign(fa_left);\ndraw_set_color(c_white);\n";
    s += "draw_text(18, _t + " + ty + ", \"" + label + "\");\n";
    s += "draw_set_halign(fa_center);\ndraw_set_color(c_yellow);\n";
    s += "draw_text(170, _t + " + ty + ", _v);\n";
    if (arrows)
    {
        s += "draw_set_color(c_aqua);\n";
        s += "draw_text(124, _t + " + ty + ", \"<\");\n";
        s += "draw_text(216, _t + " + ty + ", \">\");\n";
    }
    return s;
}

string ModeStmt(int i)
{
    string s = "_v = \"NATURAL\";\n";
    s += "if (global.gcMode[" + i + "] == 1) { _v = \"SCATTER\"; }\n";
    s += "if (global.gcMode[" + i + "] == 2) { _v = \"CHASE\"; }\n";
    s += "if (global.gcMode[" + i + "] == 3) { _v = \"FRIGHT\"; }\n";
    s += "if (global.gcMode[" + i + "] == 4) { _v = \"EATEN\"; }\n";
    return s;
}

var rows0 = new StringBuilder();
rows0.Append(Row(0, "TARGETS", "_v = \"OFF\";\nif (global.ghostDbg == true) { _v = \"ON\"; }", false));
for (int i = 0; i < 4; i++)
{
    rows0.Append(Row(i + 1, G[i].ToUpper(), ModeStmt(i), true));
}
rows0.Append(Row(5, "LIVES", "_v = string(playerLives);", true));
rows0.Append(Row(6, "SCORE", "_v = string(playerScore);", true));
rows0.Append(Row(7, "SCORE STEP", "_v = string(global.gcStep);", false));
rows0.Append(Row(8, "LEVEL", "_v = string(global.gcLevel);", true));
rows0.Append(Row(9, "LEVEL STEP", "_v = string(global.gcLvStep);", false));
rows0.Append(Row(10, "GO TO LEVEL", "_v = \"TAP\";", false));
rows0.Append(Row(11, "PIN TARGETS", "_v = \"OPEN\";", false));
rows0.Append(Row(12, "TELEPORT", "_v = \"OPEN\";", false));
rows0.Append(Row(13, "CLOSE", "_v = \"TAP\";", false));

var rows1 = new StringBuilder();
for (int i = 0; i < 4; i++)
{
    string nm = G[i].ToUpper();
    rows1.Append(Row(i * 3, nm + " PIN", "_v = \"OFF\";\nif (global.gcPinOn[" + i + "] == 1) { _v = \"ON\"; }", false));
    rows1.Append(Row(i * 3 + 1, nm + " X", "_v = string(global.gcPinX[" + i + "]);", true));
    rows1.Append(Row(i * 3 + 2, nm + " Y", "_v = string(global.gcPinY[" + i + "]);", true));
}
rows1.Append(Row(12, "COORD STEP", "_v = string(global.gcCoordStep);", false));
rows1.Append(Row(13, "BACK", "_v = \"TAP\";", false));

var rows2 = new StringBuilder();
rows2.Append(Row(0, "WHO", "_v = \"PACMAN\";\nif (global.gcTpWho == 1) { _v = \"BLINKY\"; }\nif (global.gcTpWho == 2) { _v = \"PINKY\"; }\nif (global.gcTpWho == 3) { _v = \"INKY\"; }\nif (global.gcTpWho == 4) { _v = \"CLYDE\"; }", true));
rows2.Append(Row(1, "X", "_v = string(global.gcTpX[global.gcTpWho]);", true));
rows2.Append(Row(2, "Y", "_v = string(global.gcTpY[global.gcTpWho]);", true));
rows2.Append(Row(3, "FROM HERE", "_v = \"TAP\";", false));
rows2.Append(Row(4, "TP NOW", "_v = \"TAP\";", false));
rows2.Append(Row(5, "HOLD", "_v = \"OFF\";\nif (global.gcTpHold[global.gcTpWho] == 1) { _v = \"ON\"; }", false));
string faceStmt = "_f = 0;\n";
for (int k = 0; k < 5; k++)
{
    faceStmt += "if (global.gcTpWho == " + k + ") { _f = " + O[k] + ".facing; }\n";
}
faceStmt += "_v = \"RIGHT\";\nif (_f == 1) { _v = \"UP\"; }\nif (_f == 2) { _v = \"LEFT\"; }\nif (_f == 3) { _v = \"DOWN\"; }";
rows2.Append(Row(6, "FACE", faceStmt, true));
rows2.Append(Row(7, "COORD STEP", "_v = string(global.gcCoordStep);", false));
rows2.Append(Row(8, "BACK", "_v = \"TAP\";", false));

string draw =
@"
if (state == ""playing"" && demo == false && variable_global_exists(""gcInit""))
{
    draw_set_font(PacMan);
    draw_set_valign(fa_top);
    draw_set_halign(fa_center);
    draw_set_alpha(1);
    if (variable_global_exists(""ghostCheat""))
    {
        draw_set_color(c_red);
    }
    else
    {
        draw_set_color(c_blue);
    }
    draw_rectangle(166, y + 18, 210, y + 30, true);
    draw_set_color(c_white);
    draw_text(188, y + 21, ""CHEAT"");
    if (global.gcOpen == true)
    {
        var _t = y + 56;
        var _v = """";
        var _f = 0;
        var _rows = 14;
        var _al = 0.92;
        var _title = ""CHEAT MENU"";
        if (global.gcPage == 1)
        {
            _al = 0.6;
            _title = ""PIN TARGETS"";
        }
        if (global.gcPage == 2)
        {
            _rows = 9;
            _al = 0.6;
            _title = ""TELEPORT"";
        }
        draw_set_alpha(_al);
        draw_set_color(c_black);
        draw_rectangle(10, _t - 16, 230, _t + (_rows * 13) + 2, false);
        draw_set_alpha(1);
        draw_set_color(c_blue);
        draw_rectangle(10, _t - 16, 230, _t + (_rows * 13) + 2, true);
        draw_set_color(c_yellow);
        draw_set_halign(fa_center);
        draw_text(120, _t - 12, _title);
        if (global.gcPage == 0)
        {
" + rows0.ToString() + @"
        }
        else if (global.gcPage == 1)
        {
" + rows1.ToString() + @"
        }
        else
        {
" + rows2.ToString() + @"
        }
    }
    draw_set_halign(fa_left);
    draw_set_alpha(1);
    draw_set_color(c_white);
}
";

// =====================================================================
// 4) APPLY EVERYTHING
// =====================================================================
var group = new CodeImportGroup(Data);

// target overlay
group.QueueAppend("gml_Object_o_Blinky_Draw_0", common);
group.QueueAppend("gml_Object_o_Pinky_Draw_0", common);
group.QueueAppend("gml_Object_o_Inky_Draw_0", common + inkyExtra);
group.QueueAppend("gml_Object_o_Clyde_Draw_0", common + clydeExtra);

// per-ghost variables (defaults = natural behaviour, no pin)
group.QueueAppend("gml_Object_o_Enemy_PreCreate_0",
    "\nself.gcForce = 0;\nself.gcByUs = false;\nself.gcPin = false;\nself.gcPinX = 0;\nself.gcPinY = 0;\nself.gcHold = false;\n");
group.QueueAppend("gml_Object_o_Player_PreCreate_0", "\nself.gcHold = false;\n");

// forced scatter/chase: replace the phase test inside sc_settarget
group.QueueFindReplace("gml_GlobalScript_sc_settarget",
    "if ((o_Game.changes % 2) == 0)",
    "if (gcForce == 1 || (gcForce == 0 && (o_Game.changes % 2) == 0))");

// pinned target: applied after scatter/chase/FRIGHTENED (random) targets,
// but never over an eaten ghost (it must still reach the pen).
group.QueueFindReplace("gml_GlobalScript_sc_settarget",
    "if (chomped == true)",
    "if (gcPin == true && chomped == false)\n    {\n        targetTileX = gcPinX;\n        targetTileY = gcPinY;\n    }\n    if (chomped == true)");

// HOLD: a held ghost skips its whole movement/state routine (same guard the game uses for frozen ghosts)
group.QueueFindReplace("gml_GlobalScript_sc_enemy",
    "frozen == false && stunned == false)",
    "frozen == false && stunned == false && gcHold == false)");

// HOLD: a held Pac-Man behaves as if walled (he still reads input, so he can turn)
group.QueueFindReplace("gml_GlobalScript_sc_playermove",
    "var _walled = false;",
    "var _walled = false;\n    if (gcHold == true)\n    {\n        _walled = true;\n    }");

// never save high scores in a session where cheats were used
group.QueueFindReplace("Timeline_lifelost_5",
    "if (demo == false)",
    "if (demo == false && !variable_global_exists(\"ghostCheat\"))");
group.QueueFindReplace("Timeline_lifelostFull_5",
    "if (demo == false)",
    "if (demo == false && !variable_global_exists(\"ghostCheat\"))");

// menu
group.QueueAppend("gml_Object_o_Game_Step_2", step);
group.QueueAppend("gml_Object_o_Game_Draw_64", draw);

group.Import();

ScriptMessage("Overlay v5 (targets + cheat menu + pins + teleport) injected.");
