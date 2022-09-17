﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Net;

namespace Battlegrounds.Update;

public static class Update {

	// 1. Check for new version
	// 2. If new version => download newest version
	// 3. Close application
	// 4. Install newest version

	private static readonly HttpClient _httpClient = new HttpClient();

	private static readonly Release _latestRelease = ProcessLatestRelease().Result;

	private static async Task<Release> ProcessLatestRelease() {

		_httpClient.DefaultRequestHeaders.Accept.Clear();
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "Battlegrounds Mod Launcher");

		var streamTask = await _httpClient.GetStreamAsync("https://api.github.com/repos/JustCodiex/coh2-battlegrounds/releases/latest");
        var release = JsonSerializer.Deserialize<Release>(streamTask);

		return release!;

	}

	private static bool IsNewVersion() {

		var latestVersion = new Version(Regex.Replace(_latestRelease.TagName, @"[-]?[a-zA-Z]+", ""));
        var assemblyVersion = new Version(BattlegroundsInstance.Version.ApplicationVersion);

		if (latestVersion.CompareTo(assemblyVersion) > 0) return true; 

        return false;

	}

	private static void DownloadNewVersion() {

		var downloadName = _latestRelease.Assets[0].Name;
		var downloadUrl = _latestRelease.Assets[0].InstallerDownloadUrl;

		using (var client = new WebClient()) {
			client.DownloadFile(downloadUrl, downloadName);
		}

	}

	private static bool RunInstallMSI() {

        ProcessStartInfo msiexec_bin = new ProcessStartInfo() {
            UseShellExecute = false,
            FileName = "msiexec.exe",
            Arguments = $"/package {Environment.CurrentDirectory}\\{_latestRelease.Assets[0].Name} /passive /norestart",
        };

        // Trigger compile
        Process? msi_install = Process.Start(msiexec_bin);
        if (msi_install is null) {
            Trace.WriteLine("[Installer] Failed to create MSIExec process", nameof(Update));
            return false;
        }

		msi_install.WaitForExit();

		return true;

    }

	public static void UpdateApplication() {

		if (!IsNewVersion()) return;

		DownloadNewVersion();

        Trace.WriteLine("[Installer] Installing new verison");
		if (!RunInstallMSI()) return;

	}

}
