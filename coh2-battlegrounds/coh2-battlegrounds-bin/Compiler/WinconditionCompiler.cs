﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;

using Battlegrounds.Util;
using Battlegrounds.Compiler.Source;
using Battlegrounds.Functional;
using Battlegrounds.Game.Match;

namespace Battlegrounds.Compiler;

/// <summary>
/// Static helper class for compiling a win condition in a JIT-style.
/// </summary>
public static class WinconditionCompiler {

    /// <summary>
    /// Get the path to where the gamemode archive is saved.
    /// </summary>
    /// <returns>The absolute path to the wincondition archive file.</returns>
    public static string GetArchivePath() {
        string dirpath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\my games\\Company of Heroes 2\\mods\\gamemode\\subscriptions";
        if (!Directory.Exists(dirpath)) { 
            Directory.CreateDirectory(dirpath);
        }
        return dirpath + "\\coh2_battlegrounds_wincondition.sga";
    }

    /// <summary>
    /// Compile a session into a sga archive file.
    /// </summary>
    /// <param name="workdir">The temporary work directory.</param>
    /// <param name="sessionFile">The session file to include</param>
    /// <param name="wincondition">The wincondition to compile.</param>
    /// <param name="source">The wincondition source file locator.</param>
    /// <param name="includeFiles">Additional files to include in the gamemode.</param>
    /// <returns>True of the archive file was created sucessfully. False if any error occured.</returns>
    public static bool CompileToSga(string workdir, string sessionFile, ISession session, IWinconditionSource source, LocaleCompiler locCompiler, params WinconditionSourceFile[] includeFiles) {

        // Verify is win condition source is valid
        if (source is null) {
            Trace.WriteLine("Failed to find a valid source", nameof(WinconditionCompiler));
            return false;
        }

        // Get the files
        var scarFiles = source.GetScarFiles().Union(includeFiles.Where(x => x.Path.EndsWith(".scar", StringComparison.InvariantCulture)));
        var winFiles = source.GetWinFiles();
        var localeFiles = source.GetLocaleFiles("");
        var uiFiles = source.GetUIFiles(session.Gamemode);
        var infoFile = source.GetInfoFile(session.Gamemode);
        var modiconFile = source.GetModGraphic();

        // Fix potential missing '\'
        if (!workdir.EndsWith("\\", false, CultureInfo.InvariantCulture)) {
            workdir += "\\";
        }

        // Create the workspace
        CreateWorkspace(workdir);

        // The archive definition to use when compiling
        TxtBuilder archiveDef = new();

        // Scar/Data TOC section
        archiveDef.AppendLine($"Archive name=\"{session.Gamemode.Guid}\" blocksize=\"262144\"");
        archiveDef.AppendLine("TOCStart name=\"data\" alias=\"data\" path=\"\" relativeroot=\"data\"");
        archiveDef.AppendLine("\tFileSettingsStart defverification=\"sha1_blocks\" defcompression=\"stream_compress\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(rgt|ttf)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"none\" ct=\"store\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(abp)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"none\" ct=\"buffer_compress\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(dds|mua|muax|rpb|sua|tga)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"none\" ct=\"\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(gfx)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"sha1_blocks\" ct=\"\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(ai|events|info|lua|options|scar|scenref|squadai)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"sha1_blocks\" ct=\"buffer_compress\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(scenariomarker|sgb|ter|sme|exclude|path)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"crc_blocks\" ct=\"\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(smf)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"none\" ct=\"store\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(bsc)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"none\" ct=\"buffer_compress\"");
        archiveDef.AppendLine("\tFileSettingsEnd");

        // Add and compile win file(s)
        foreach (var file in winFiles) {
            if (!AddFile(archiveDef, "data\\game\\winconditions\\", workdir, file)) {
                return false;
            }
        }

        // Add the session file
        AddLocalFile(archiveDef, sessionFile, "data\\scar\\winconditions\\auxiliary_scripts\\", workdir);

        // Add and *compile* scar files
        foreach (var file in scarFiles) {
            if (!AddFile(archiveDef, "data\\scar\\winconditions\\", workdir, file)) {
                return false;
            }
        }

        // Add the graphic files
        AddGraphics(archiveDef, workdir, uiFiles);

        // Info TOC section
        archiveDef.AppendLine("TOCEnd");
        archiveDef.AppendLine("TOCStart name=\"info\" alias=\"info\" path=\"\" relativeroot=\"info\"");
        archiveDef.AppendLine("\tFileSettingsStart defverification=\"crc_blocks\" defcompression=\"stream_compress\"");
        archiveDef.AppendLine("\t\tOverride wildcard=\".*\\.(lua)$\" minsize=\"-1\" maxsize=\"-1\" vt=\"crc_blocks\" ct=\"buffer_compress\"");
        archiveDef.AppendLine("\tFileSettingsEnd");

        // Add and compile info file
        if (!AddInfoFiles(archiveDef, workdir, infoFile, modiconFile)) {
            return false;
        }

        // Locale TOC section
        archiveDef.AppendLine("TOCEnd");
        archiveDef.AppendLine("TOCStart name=\"locale\" alias=\"locale\" path=\"\" relativeroot=\"locale\"");
        archiveDef.AppendLine("\tFileSettingsStart defverification=\"crc_blocks\" defcompression=\"stream_compress\"");
        archiveDef.AppendLine("\tFileSettingsEnd");

        // Add and compile locale files
        foreach (var file in localeFiles) {

            // Grab path and log
            string abspath = Path.GetFullPath(workdir + file.Path.Replace("/", "\\"));
            Trace.WriteLine($"Adding locale file [ABS] <{abspath}>", nameof(WinconditionCompiler));

            // Translate the locale file
            locCompiler.TranslateLocale(file.Contents, abspath, session.Names.ToArray());

            // Append win condition file to locale section.
            archiveDef.AppendLine($"\t{abspath}");

        }

        // Add end TOC
        archiveDef.AppendLine("TOCEnd");

        // Generate the path
        string archiveDefTxtPath = workdir + "ArchiveDefinition.txt";

        // Save the archive definition
        archiveDef.Save(archiveDefTxtPath);

        // The output archive
        string outputArchive = GetArchivePath();

        // Call the archive
        if (!Archiver.Archive(archiveDefTxtPath, workdir, outputArchive)) {
            return false;
        }

#if RELEASE
        // Delete the temp_build directory
        Directory.Delete(workdir, true);
#endif

        // Return true
        return true;

    }

    private static void CreateWorkspace(string workdir) {

        // Clear the directory we're going to work in
        if (Directory.Exists(workdir)) {
            Directory.Delete(workdir, true);
        }

        // Create directories
        Directory.CreateDirectory(workdir);
        Directory.CreateDirectory($"{workdir}data\\");
        Directory.CreateDirectory($"{workdir}data\\game");
        Directory.CreateDirectory($"{workdir}data\\game\\winconditions");
        Directory.CreateDirectory($"{workdir}data\\scar");
        Directory.CreateDirectory($"{workdir}data\\scar\\winconditions");
        Directory.CreateDirectory($"{workdir}data\\scar\\winconditions\\auxiliary_scripts");
        Directory.CreateDirectory($"{workdir}data\\scar\\winconditions\\ui_api");
        Directory.CreateDirectory($"{workdir}data\\ui\\");
        Directory.CreateDirectory($"{workdir}data\\ui\\Assets\\");
        Directory.CreateDirectory($"{workdir}data\\ui\\Assets\\Textures\\");
        Directory.CreateDirectory($"{workdir}data\\ui\\Bin\\");
        Directory.CreateDirectory($"{workdir}info");
        Directory.CreateDirectory($"{workdir}locale");
        Directory.CreateDirectory($"{workdir}locale\\english");

    }

    private static bool AddFile(TxtBuilder builder, string rpath, string workdir, WinconditionSourceFile sourceFile, bool useBytes = false, Encoding? encoding = null) {

        string relpath = rpath + sourceFile.Path;
        string abspath = Path.GetFullPath(workdir + relpath.Replace("/", "\\"));

        if (sourceFile.Contents == null || sourceFile.Contents.Length == 0) {
            Trace.WriteLine($"Error file [ABS] <{abspath}>", nameof(WinconditionCompiler));
            return false;
        }

        Trace.WriteLine($"Adding file [ABS] <{abspath}>", nameof(WinconditionCompiler));

        builder.AppendLine($"\t{abspath}");

        if (File.Exists(abspath)) {
            File.Delete(abspath);
        }

        if (useBytes) {
            File.WriteAllBytes(abspath, sourceFile.Contents);
        } else {
            if (encoding is null) {
                encoding = Encoding.UTF8;
            }
            File.WriteAllText(abspath, encoding.GetString(sourceFile.Contents));
        }

        return true;

    }

    private static void AddLocalFile(TxtBuilder builder, string localfile, string relpath, string workdir) {

        // Get path to copy file to
        string copyFile = Path.GetFullPath($"{workdir}{relpath}{ Path.GetFileName(localfile)}");

        // Add the local file
        builder.AppendLine($"\t{copyFile}");

        // Copy file
        File.Copy(Path.GetFullPath(localfile), copyFile);

    }

    private static void AddGraphics(TxtBuilder builder, string workdir, WinconditionSourceFile[] uiFiles) {

        // Create paths
        string ddsPath = Path.GetFullPath($"{workdir}data\\ui\\Assets\\Textures\\");
        string gfxPath = Path.GetFullPath($"{workdir}data\\ui\\Bin\\");

        // Loop through gfx files and add them
        for (int i = 0; i < uiFiles.Length; i++) {
            if (uiFiles[i].Path.EndsWith(".dds")) {
                string ddspath = $"{ddsPath}{Path.GetFileName(uiFiles[i].Path)}";
                File.WriteAllBytes(ddspath, uiFiles[i].Contents);
                builder.AppendLine($"\t{ddspath}");
            } else if (uiFiles[i].Path.EndsWith(".gfx")) {
                string gfxpath = $"{gfxPath}{Path.GetFileName(uiFiles[i].Path)}";
                File.WriteAllBytes(gfxpath, uiFiles[i].Contents);
                builder.AppendLine($"\t{gfxpath}");
            } else {
                Trace.Write($"Skipping graphics file \"{uiFiles[i].Path}\"", nameof(WinconditionCompiler));
            }
        }

    }

    private static bool AddInfoFiles(TxtBuilder builder, string workdir, WinconditionSourceFile infoFile, WinconditionSourceFile iconFile) {

        if (!AddFile(builder, string.Empty, workdir, infoFile)) {
            return false;
        }

        return AddFile(builder, string.Empty, workdir, iconFile);

    }

}

