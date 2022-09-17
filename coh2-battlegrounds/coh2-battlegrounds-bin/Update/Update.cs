﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

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

	public static void UpdateApplication() {

		if (!IsNewVersion()) return;

	}

}
