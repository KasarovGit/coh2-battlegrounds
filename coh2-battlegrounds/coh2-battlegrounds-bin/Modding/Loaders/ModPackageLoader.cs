﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Battlegrounds.ErrorHandling.CommonExceptions;
using Battlegrounds.Functional;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Modding.Content;

namespace Battlegrounds.Modding.Loaders;

/// <summary>
/// Class for loading <see cref="ModPackage"/> instances through their JSON representation.
/// </summary>
public class ModPackageLoader : JsonConverter<ModPackage> {

    public override ModPackage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {

        // Lookup table to store values in temp
        Dictionary<string, object> __lookup = new();

        // Read data
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {
            string prop = reader.ReadProperty() ?? throw new ObjectPropertyNotFoundException();
            __lookup[prop] = prop switch {
                "ID" => reader.GetString()?.ToLowerInvariant() ?? string.Empty,
                "Name" => reader.GetString() ?? string.Empty,
                "TuningGUID" => reader.GetString() ?? string.Empty,
                "GamemodeGUID" => reader.GetString() ?? string.Empty,
                "AssetGUID" => reader.GetString() ?? string.Empty,
                "ParadropUnits" => reader.GetStringArray(),
                "VerificationUpgrade" => reader.GetString() ?? string.Empty,
                "AllowSupplySystem" => reader.GetBoolean(),
                "AllowWeatherSystem" => reader.GetBoolean(),
                "LocaleFiles" => JsonSerializer.Deserialize<ModPackage.ModLocale[]>(ref reader) ?? Array.Empty<ModPackage.ModLocale>(),
                "FactionData" => JsonSerializer.Deserialize<FactionData[]>(ref reader) ?? Array.Empty<FactionData>(),
                "CustomOptions" => JsonSerializer.Deserialize<ModPackage.CustomOptions[]>(ref reader) ?? Array.Empty<ModPackage.CustomOptions>(),
                "Gamemodes" => JsonSerializer.Deserialize<Gamemode[]>(ref reader) ?? Array.Empty<Gamemode>(),
                "Towing" => ReadTowdata(ref reader),
                "TeamWeaponCaptureSquads" => JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(ref reader) ?? new(),
                _ => throw new NotImplementedException(prop)
            };
        }

        // Get GUIDs
        var tuningGUID = __lookup.ContainsKey("TuningGUID") ? ModGuid.FromGuid(__lookup["TuningGUID"] as string ?? string.Empty) : ModGuid.BaseGame;
        var gamemodeGUID = __lookup.ContainsKey("GamemodeGUID") ? ModGuid.FromGuid(__lookup["GamemodeGUID"] as string ?? string.Empty) : ModGuid.BaseGame;
        var assetGUID = __lookup.ContainsKey("AssetGUID") ? ModGuid.FromGuid(__lookup["AssetGUID"] as string ?? string.Empty) : ModGuid.BaseGame;

        // Get factions
        Dictionary<Faction, FactionData> factions = __lookup.GetCastValueOrDefault("FactionData", Array.Empty<FactionData>())
            .ToDictionary(x => Faction.FromName(x.Faction));

        // Get tow data
        (bool hastow, string istow, string istowing) = __lookup.GetCastValueOrDefault("Towing", (false, string.Empty, string.Empty));

        // Get ID
        string packageID = __lookup.GetCastValueOrDefault("ID", string.Empty);
        if (string.IsNullOrEmpty(packageID)) {
            throw new InvalidDataException("Expected package ID but found none. Cannot read the mod package!");
        }

        // Return mod package
        ModPackage package = new() {
            ID = packageID,
            PackageName = __lookup.GetCastValueOrDefault("Name", packageID),
            TuningGUID = tuningGUID,
            GamemodeGUID = gamemodeGUID,
            AssetGUID = assetGUID,
            FactionSettings = factions,
            IsTowEnabled = hastow,
            IsTowedUpgrade = istow,
            IsTowingUpgrade = istowing,
            VerificationUpgrade = __lookup.GetCastValueOrDefault("VerificationUpgrade", string.Empty),
            ParadropUnits = __lookup.GetCastValueOrDefault("ParadropUnits", Array.Empty<string>()),
            LocaleFiles = __lookup.GetCastValueOrDefault("LocaleFiles", Array.Empty<ModPackage.ModLocale>()),
            AllowSupplySystem = __lookup.GetCastValueOrDefault("AllowSupplySystem", false),
            AllowWeatherSystem = __lookup.GetCastValueOrDefault("AllowWeatherSystem", false),
            Gamemodes = __lookup.GetCastValueOrDefault("Gamemodes", Array.Empty<Gamemode>()),
            TeamWeaponCaptureSquads = __lookup.GetCastValueOrDefault("TeamWeaponCaptureSquads", new Dictionary<string, Dictionary<string, string>>())
        };

        // Set faction data owners
        package.FactionSettings.Values.ForEach(x => x.Package = package);

        // Return the package
        return package;

    }

    private static (bool, string, string) ReadTowdata(ref Utf8JsonReader reader) {
        object[] data = { false, string.Empty, string.Empty };
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {
            string prop = reader.ReadProperty() ?? throw new ObjectPropertyNotFoundException();
            data[prop switch {
                "IsEnabled" => 0,
                "IsTowed" => 1,
                "IsTowing" => 2,
                _ => throw new NotImplementedException(prop)
            }] = reader.TokenType is JsonTokenType.String ? (reader.GetString() ?? string.Empty) : reader.GetBoolean();
        }
        return ((bool)data[0], data[1] as string ?? string.Empty, data[2] as string ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, ModPackage value, JsonSerializerOptions options) => throw new NotSupportedException();

}
