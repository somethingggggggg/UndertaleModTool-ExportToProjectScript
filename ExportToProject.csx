using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Drawing;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModLib.Decompiler;

int progress = 0;
string projFolder = GetFolder(FilePath) + "Export_Project" + Path.DirectorySeparatorChar;
var context = new DecompileContext(Data, true);
TextureWorker worker = new TextureWorker();
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
string gmxDeclaration = "This Document is generated by GameMaker, if you edit it by hand then you do so at your own risk!";

if (Directory.Exists(projFolder))
{
    ScriptError("A project export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(projFolder);

// --------------- Start exporting ---------------

// Export sprites
UpdateProgressBar(null, "Exporting sprites...", progress++, 8);
await ExportSprites();

// Export backgrounds
UpdateProgressBar(null, "Exporting backgrounds...", progress++, 8);
await ExportBackground();

// Export objects
UpdateProgressBar(null, "Exporting objects...", progress++, 8);
await ExportGameObjects();

// Export rooms
UpdateProgressBar(null, "Exporting rooms...", progress++, 8);
await ExportRooms();

// Export sounds
UpdateProgressBar(null, "Exporting sounds...", progress++, 8);
await ExportSounds();

// Export scripts
UpdateProgressBar(null, "Exporting scripts...", progress++, 8);
await ExportScripts();

// Export fonts
UpdateProgressBar(null, "Exporting fonts...", progress++, 8);
await ExportFonts();

// Generate project file
UpdateProgressBar(null, "Generating project file...", progress++, 8);
ExportProjectFile();

// --------------- Export completed ---------------
worker.Cleanup();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + projFolder);

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
string BoolToString(bool value)
{
    // In the GMX file, -1 is true and 0 is false.
    return value ? "-1" : "0";
}

// --------------- Export Sprite ---------------
async Task ExportSprites()
{
    Directory.CreateDirectory(projFolder + "/sprites/images");
    await Task.Run(() => Parallel.ForEach(Data.Sprites, ExportSprite));
}
void ExportSprite(UndertaleSprite sprite)
{
    // Save the sprite GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("sprite", 
            new XElement("type", "0"),
            new XElement("xorig", sprite.OriginX.ToString()),
            new XElement("yorigin", sprite.OriginY.ToString()),
            new XElement("colkind", sprite.BBoxMode.ToString()),
            new XElement("coltolerance", "0"),
            new XElement("sepmasks", sprite.SepMasks.ToString("D")),
            new XElement("bboxmode", sprite.BBoxMode.ToString()),
            new XElement("bbox_left", sprite.MarginLeft.ToString()),
            new XElement("bbox_right", sprite.MarginRight.ToString()),
            new XElement("bbox_top", sprite.MarginTop.ToString()),
            new XElement("bbox_bottom", sprite.MarginBottom.ToString()),
            new XElement("HTile", "0"),
            new XElement("VTile", "0"),
            new XElement("TextureGroups", 
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", sprite.Width.ToString()),
            new XElement("height", sprite.Height.ToString()),
            new XElement("frames"),
            new XElement("bbox_right", sprite.MarginRight.ToString())
        )
    );

    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            gmx.Element("sprite").Element("frames").Add(
                new XElement(
                    "frame",
                    new XAttribute("index", i.ToString()),
                    "images\\" + sprite.Name.Content + "_" + i + ".png"
                )
            );
        }
    }

    File.WriteAllText(projFolder + "/sprites/" + sprite.Name.Content + ".sprite.gmx", gmx.ToString());

    // Save sprite images
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            // Fix sprite size
            var bitmapNew = new Bitmap((int)sprite.Width, (int)sprite.Height);
            var bitmapOrigin = worker.GetTextureFor(sprite.Textures[i].Texture, Path.GetFileNameWithoutExtension(projFolder + "/sprites/images/" + sprite.Name.Content + "_" + i + ".png"));
            //worker.ExportAsPNG(sprite.Textures[i].Texture, projFolder + "/sprites/images/" + sprite.Name.Content + "_" + i + ".png");
            var g = Graphics.FromImage(bitmapNew);
            g.DrawImage(bitmapOrigin, (int)sprite.Textures[i].Texture.TargetX, (int)sprite.Textures[i].Texture.TargetY);
            bitmapNew.Save(projFolder + "/sprites/images/" + sprite.Name.Content + "_" + i + ".png");
            bitmapNew.Dispose();
            bitmapOrigin.Dispose();
        }
    }
}

// --------------- Export Background ---------------
async Task ExportBackground()
{
    Directory.CreateDirectory(projFolder + "/background/images");
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ExportBackground));
}
void ExportBackground(UndertaleBackground background)
{
    // Save the backgound GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("background", 
            new XElement("istileset", "-1"),
            new XElement("tilewidth", background.Texture.BoundingWidth.ToString()),
            new XElement("tileheight", background.Texture.BoundingHeight.ToString()),
            new XElement("tilexoff", "0"),
            new XElement("tileyoff", "0"),
            new XElement("tilehsep", "0"),
            new XElement("tilevsep", "0"),
            new XElement("HTile", "-1"),
            new XElement("VTile", "-1"),
            new XElement("TextureGroups", 
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", background.Texture.BoundingWidth.ToString()),
            new XElement("height", background.Texture.BoundingHeight.ToString()),
            new XElement("data", "images\\" + background.Name.Content + ".png")
        )
    );

    File.WriteAllText(projFolder + "/background/" + background.Name.Content + ".background.gmx", gmx.ToString());
   
    // Save background images
    worker.ExportAsPNG(background.Texture, projFolder + "/background/images/" + background.Name.Content + ".png");
}
// --------------- Export Object ---------------
async Task ExportGameObjects()
{
    Directory.CreateDirectory(projFolder + "/objects");
    await Task.Run(() => Parallel.ForEach(Data.GameObjects, ExportGameObject));
}
void ExportGameObject(UndertaleGameObject gameObject)
{
    // Save the object GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("object", 
            new XElement("spriteName", gameObject.Sprite is null ? "<undefined>" : gameObject.Sprite.Name.Content),
            new XElement("solid", BoolToString(gameObject.Solid)),
            new XElement("visible", BoolToString(gameObject.Visible)),
            new XElement("depth", gameObject.Depth.ToString()),
            new XElement("persistent", BoolToString(gameObject.Persistent)),
            new XElement("parentName", gameObject.ParentId is null ? "<undefined>" : gameObject.ParentId.Name.Content),
            new XElement("maskName", gameObject.TextureMaskId is null ? "<undefined>" : gameObject.TextureMaskId.Name.Content),
            new XElement("events")
        )
    );

    // Traversing the event type list
    for (int i = 0; i < gameObject.Events.Count; i++)
    {
        // Determine if an event is empty
        if (gameObject.Events[i].Count > 0)
        {
            // Traversing event list
            foreach (var j in gameObject.Events[i])
            {
                var eventsNode = gmx.Element("object").Element("events");

                var eventNode = new XElement("event",
                        new XAttribute("eventtype", i.ToString())
                );

                if (j.EventSubtype == 4)
                {
                    // To get the actual name of the collision object when the event type is a collision event
                    eventNode.Add(new XAttribute("ename", Data.GameObjects[(int)j.EventSubtype].Name.Content));
                }
                else
                {
                    // Get the sub-event number directly
                    eventNode.Add(new XAttribute("enumb", j.EventSubtype.ToString()));
                }

                // Save action
                var actionNode = new XElement("action");

                // Traversing the action list
                foreach (var k in j.Actions)
                {
                    actionNode.Add(
                        new XElement("libid", k.LibID.ToString()),
                        new XElement("id", k.ID.ToString()),
                        new XElement("kind", k.Kind.ToString()),
                        new XElement("userelative", k.LibID.ToString()),
                        new XElement("libid", BoolToString(k.UseRelative)),
                        new XElement("isquestion", BoolToString(k.IsQuestion)),
                        new XElement("useapplyto", BoolToString(k.UseApplyTo)),
                        new XElement("exetype", k.ExeType.ToString()),
                        new XElement("functionname", k.ActionName.Content),
                        new XElement("codestring", ""),
                        new XElement("whoName", "self"),
                        new XElement("relative", BoolToString(k.Relative)),
                        new XElement("isnot", BoolToString(k.IsNot)),
                        new XElement("arguments",
                            new XElement("argument", 
                                new XElement("kind", "1"),
                                new XElement("string", k.CodeId != null ? Decompiler.Decompile(k.CodeId, DECOMPILE_CONTEXT.Value) : "") 
                            )
                        )
                    );
                }
                eventNode.Add(actionNode);
                eventsNode.Add(eventNode);

                // TODO：Physics
            }
        }
    }

    File.WriteAllText(projFolder + "/objects/" + gameObject.Name.Content + ".object.gmx", gmx.ToString());
}

// --------------- Export Room ---------------
async Task ExportRooms()
{
    Directory.CreateDirectory(projFolder + "/rooms");
    await Task.Run(() => Parallel.ForEach(Data.Rooms, ExportRoom));
}
void ExportRoom(UndertaleRoom room)
{
    // Save the room GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("room", 
            new XElement("caption", room.Caption.Content),
            new XElement("width", room.Width.ToString()),
            new XElement("height", room.Height.ToString()),
            new XElement("vsnap", "32"),
            new XElement("hsnap", "32"),
            new XElement("isometric", "0"),
            new XElement("speed", room.Speed.ToString()),
            new XElement("persistent", BoolToString(room.Persistent)),
            new XElement("colour", room.BackgroundColor.ToString()),
            new XElement("code", room.CreationCodeId is null ? "" : Decompiler.Decompile(room.CreationCodeId, context)),
            new XElement("enableViews", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews))),
            new XElement("clearViewBackground", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ShowColor))),
            new XElement("clearDisplayBuffer", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ClearDisplayBuffer)))
        )
    );
    // TODO：MakerSettings

    // Room backgrounds
    var backgroundsNode = new XElement("backgrounds");
    foreach (var i in room.Backgrounds)
    {
        var backgroundNode = new XElement("background",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("foreground", BoolToString(i.Foreground)),
            new XAttribute("name", i.BackgroundDefinition is null ? "" : i.BackgroundDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("htiled", i.TileX.ToString()),
            new XAttribute("vtiled", i.TileY.ToString()),
            new XAttribute("hspeed", i.SpeedX.ToString()),
            new XAttribute("vspeed", i.SpeedY.ToString()),
            new XAttribute("stretch", "0")
        );
        backgroundsNode.Add(backgroundNode);
    }
    gmx.Element("room").Add(backgroundsNode);

    // Room views
    var viewsNode = new XElement("views");
    foreach (var i in room.Views)
    {
        var viewNode = new XElement("view",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("objName", i.ObjectId is null ? "<undefined>" : i.ObjectId.Name.Content),
            new XAttribute("xview", i.ViewX.ToString()),
            new XAttribute("yview", i.ViewY.ToString()),
            new XAttribute("wview", i.ViewHeight.ToString()),
            new XAttribute("xport", i.PortX.ToString()),
            new XAttribute("yport", i.PortY.ToString()),
            new XAttribute("wport", i.PortWidth.ToString()),
            new XAttribute("hport", i.PortHeight.ToString()),
            new XAttribute("hborder", i.BorderX.ToString()),
            new XAttribute("vborder", i.BorderY.ToString()),
            new XAttribute("hspeed", i.SpeedX.ToString()),
            new XAttribute("vspeed", i.SpeedY.ToString())
        );
        viewsNode.Add(viewNode);
    }
    gmx.Element("room").Add(viewsNode);

    // Room instances
    var instancesNode = new XElement("instances");
    foreach (var i in room.GameObjects)
    {
        var instanceNode = new XElement("instance",
            new XAttribute("objName", i.ObjectDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("name", "inst_" + i.InstanceID.ToString("X")),
            new XAttribute("locked", "0"),
            new XAttribute("code", i.CreationCode != null ? Decompiler.Decompile(i.CreationCode, DECOMPILE_CONTEXT.Value) : ""),
            new XAttribute("scaleX", i.ScaleX.ToString()),
            new XAttribute("scaleY", i.ScaleY.ToString()),
            new XAttribute("colour", i.Color.ToString()),
            new XAttribute("rotation", i.Rotation.ToString())
        );
        instancesNode.Add(instanceNode);
    }
    gmx.Element("room").Add(instancesNode);

    // Room tiles
    var tilesNode = new XElement("tiles");
    foreach (var i in room.Tiles)
    {
        var tileNode = new XElement("tile",
            new XAttribute("bgName", i.BackgroundDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("w", i.Width.ToString()),
            new XAttribute("h", i.Height.ToString()),
            new XAttribute("xo", i.SourceX.ToString()),
            new XAttribute("yo", i.SourceY.ToString()),
            new XAttribute("id", i.InstanceID.ToString()),
            new XAttribute("name", "inst_" + i.InstanceID.ToString("X")),
            new XAttribute("depth", i.TileDepth.ToString()),
            new XAttribute("locked", "0"),
            new XAttribute("colour", i.Color.ToString()),
            new XAttribute("scaleX", i.ScaleX.ToString()),
            new XAttribute("scaleY", i.ScaleY.ToString())
        );
        tilesNode.Add(tileNode);
    }
    gmx.Element("room").Add(tilesNode);

    // TODO：Room physics

    File.WriteAllText(projFolder + "/rooms/" + room.Name.Content + ".room.gmx", gmx.ToString());
}

// --------------- Export Sound ---------------
async Task ExportSounds()
{
    Directory.CreateDirectory(projFolder + "/sound/audio");
    await Task.Run(() => Parallel.ForEach(Data.Sounds, ExportSound));
}
void ExportSound(UndertaleSound sound)
{
    // Save the sound GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("sound", 
            new XElement("kind", Path.GetExtension(sound.File.Content) == ".ogg" ? "3" : "0"),
            new XElement("extension", Path.GetExtension(sound.File.Content)),
            new XElement("origname", "sound\\audio\\" + sound.File.Content),
            new XElement("effects", sound.Effects.ToString()),
            new XElement("volume", sound.Volume.ToString()),
            new XElement("pan", "0"),
            new XElement("bitRates", "192"),
            new XElement("sampleRates", 
                new XElement("sampleRate", "44100")
            ),
            new XElement("types", 
                new XElement("type", "1")
            ),
            new XElement("bitDepths", 
                new XElement("bitDepth", "16")
            ),
            new XElement("preload", "-1"),
            new XElement("data", Path.GetFileName(sound.File.Content)),
            new XElement("compressed", Path.GetExtension(sound.File.Content) == ".ogg" ? "1" : "0"),
            new XElement("streamed", Path.GetExtension(sound.File.Content) == ".ogg" ? "1" : "0"),
            new XElement("uncompressOnLoad", "0"),
            new XElement("audioGroup", "0")
        )
    );

    File.WriteAllText(projFolder + "/sound/" + sound.Name.Content + ".sound.gmx", gmx.ToString());

    // Save sound files
    if (sound.AudioFile != null)
        File.WriteAllBytes(projFolder + "/sound/audio/" + sound.File.Content, sound.AudioFile.Data);
}

// --------------- Export Script ---------------
async Task ExportScripts()
{
    Directory.CreateDirectory(projFolder + "/scripts/");
    await Task.Run(() => Parallel.ForEach(Data.Scripts, ExportScript));
}
void ExportScript(UndertaleScript script)
{
    // Save GML files
    File.WriteAllText(projFolder + "/scripts/" + script.Name.Content + ".gml", (script.Code != null ? Decompiler.Decompile(script.Code, DECOMPILE_CONTEXT.Value) : ""));
}

// --------------- Export Font ---------------
async Task ExportFonts()
{
    Directory.CreateDirectory(projFolder + "/fonts/");
    await Task.Run(() => Parallel.ForEach(Data.Fonts, ExportFont));
}
void ExportFont(UndertaleFont font)
{
    // Save the font GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("font", 
            new XElement("name", font.Name.Content),
            new XElement("size", font.EmSize.ToString()),
            new XElement("bold", BoolToString(font.Bold)),
            new XElement("renderhq", "-1"),
            new XElement("italic", BoolToString(font.Italic)),
            new XElement("charset", font.Charset.ToString()),
            new XElement("aa", font.AntiAliasing.ToString()),
            new XElement("includeTTF", "0"),
            new XElement("TTFName", ""),
            new XElement("texgroups", 
                new XElement("texgroup", "0")
            ),
            new XElement("ranges", 
                new XElement("range0", font.RangeStart.ToString() + "," + font.RangeEnd.ToString())
            ),
            new XElement("glyphs"),
            new XElement("kerningPairs"),
            new XElement("image", font.Name.Content + ".png")
        )
    );

    var glyphsNode = gmx.Element("font").Element("glyphs");
    foreach (var i in font.Glyphs)
    {
        var glyphNode = new XElement("glyph");
        glyphNode.Add(new XElement("character", i.Character.ToString()));
        glyphNode.Add(new XElement("x", i.SourceX.ToString()));
        glyphNode.Add(new XElement("y", i.SourceY.ToString()));
        glyphNode.Add(new XElement("w", i.SourceWidth.ToString()));
        glyphNode.Add(new XElement("h", i.SourceHeight.ToString()));
        glyphNode.Add(new XElement("shift", i.Shift.ToString()));
        glyphNode.Add(new XElement("offset", i.Offset.ToString()));
        glyphsNode.Add(glyphNode);
    }

    File.WriteAllText(projFolder + "/fonts/" + font.Name.Content + ".font.gmx", gmx.ToString());

    // Save font textures
    worker.ExportAsPNG(font.Texture, projFolder + "/fonts/" + font.Name.Content + ".png");
}

// --------------- Generate project file ---------------
void ExportProjectFile()
{
    // Write all resource indexes to project.gmx
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("assets")
    );

    // Write sound indexes
    var soundsNode = new XElement("sounds",
        new XAttribute("name", "sound")
    );
    foreach (var i in Data.Sounds)
    {
        var soundNode = new XElement("sound", "sound\\" + i.Name.Content);
        soundsNode.Add(soundNode);
    }
    gmx.Element("assets").Add(soundsNode);

    // Write sprite indexes
    var spritesNode = new XElement("sprites",
        new XAttribute("name", "sprites")
    );
    foreach (var i in Data.Sprites)
    {
        var spriteNode = new XElement("sprite", "sprites\\" + i.Name.Content);
        spritesNode.Add(spriteNode);
    }
    gmx.Element("assets").Add(spritesNode);

    // Write background indexes
    var backgroundsNode = new XElement("backgrounds",
        new XAttribute("name", "background")
    );
    foreach (var i in Data.Backgrounds)
    {
        var backgroundNode = new XElement("background", "background\\" + i.Name.Content);
        backgroundsNode.Add(backgroundNode);
    }
    gmx.Element("assets").Add(backgroundsNode);

    // Write script indexes
    var scriptsNode = new XElement("scripts",
        new XAttribute("name", "scripts")
    );
    foreach (var i in Data.Scripts)
    {
        var scriptNode = new XElement("script", "scripts\\" + i.Name.Content + ".gml");
        scriptsNode.Add(scriptNode);
    }
    gmx.Element("assets").Add(scriptsNode);

    // Write font indexes
    var scriptsNode = new XElement("scripts",
        new XAttribute("name", "scripts")
    );
    foreach (var i in Data.Scripts)
    {
        var scriptNode = new XElement("script", "scripts\\" + i.Name.Content + ".gml");
        scriptsNode.Add(scriptNode);
    }
    gmx.Element("assets").Add(scriptsNode);

    File.WriteAllText(projFolder + "Export_Project.project.gmx", gmx.ToString());
///////////////////////////////////////////
    xmlWriter.WriteStartElement("sounds");
    xmlWriter.WriteAttributeString("name", "sound");
    foreach (var i in Data.Sounds)
    {
        xmlWriter.WriteStartElement("sound");
        xmlWriter.WriteString("sound\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("sprites");
    xmlWriter.WriteAttributeString("name", "sprites");
    foreach (var i in Data.Sprites)
    {
        xmlWriter.WriteStartElement("sprite");
        xmlWriter.WriteString("sprites\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("backgrounds");
    xmlWriter.WriteAttributeString("name", "background");
    foreach (var i in Data.Backgrounds)
    {
        xmlWriter.WriteStartElement("background");
        xmlWriter.WriteString("background\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("scripts");
    xmlWriter.WriteAttributeString("name", "scripts");
    foreach (var i in Data.Scripts)
    {
        xmlWriter.WriteStartElement("script");
        xmlWriter.WriteString("scripts\\" + i.Name.Content + ".gml");
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("fonts");
    xmlWriter.WriteAttributeString("name", "fonts");
    foreach (var i in Data.Fonts)
    {
        xmlWriter.WriteStartElement("font");
        xmlWriter.WriteString("fonts\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("objects");
    xmlWriter.WriteAttributeString("name", "objects");
    foreach (var i in Data.GameObjects)
    {
        xmlWriter.WriteStartElement("object");
        xmlWriter.WriteString("objects\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteStartElement("rooms");
    xmlWriter.WriteAttributeString("name", "rooms");
    foreach (var i in Data.Rooms)
    {
        xmlWriter.WriteStartElement("room");
        xmlWriter.WriteString("rooms\\" + i.Name.Content);
        xmlWriter.WriteEndElement();
    }
    xmlWriter.WriteEndElement();

    xmlWriter.WriteEndElement();
    xmlWriter.WriteEndDocument();
    xmlWriter.Close();
}

void WriteIndexes(string elementName, string attributeName, List dataList, string oneName, string fileName, XElement rootNode)
{
    var datasNode = new XElement(
        new XAttribute(elementName, attributeName)
    );
    foreach (var i in dataList)
    {
        var dataNode = new XElement(oneName, fileName);
        datasNode.Add(dataNode);
    }
    rootNode.Add(datasNode);
}