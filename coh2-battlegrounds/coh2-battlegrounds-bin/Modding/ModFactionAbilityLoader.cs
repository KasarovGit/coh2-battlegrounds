﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Battlegrounds.Functional;
using Battlegrounds.Game.Gameplay;

namespace Battlegrounds.Modding {

    public class ModFactionAbilityLoader : JsonConverter<ModPackage.FactionData.FactionAbility> {

        public override ModPackage.FactionData.FactionAbility Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {

            // Lookup table to store values in temp
            Dictionary<string, object> __lookup = new();

            // Read data
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {
                string prop = reader.ReadProperty();
                __lookup[prop] = prop switch {
                    "Blueprint" => reader.GetString(),
                    "LockoutBlueprint" => reader.GetString(),
                    "AbilityCategory" => reader.GetString() switch {
                        "Artillery" => AbilityCategory.Artillery,
                        "AirSupport" => AbilityCategory.AirSupport,
                        "Default" => AbilityCategory.Default,
                        "Undefined" => AbilityCategory.Undefined,
                        _ => throw new FormatException()
                    },
                    "MaxUsePerMatch" => reader.GetInt32(),
                    "RequireOffmap" => reader.GetBoolean(),
                    "OffmapCountEffectivenesss" => reader.GetSingle(),
                    "CanGrantVeterancy" => reader.GetBoolean(),
                    "VeterancyRanks" => JsonSerializer.Deserialize<ModPackage.FactionData.FactionAbility.AbilityVeterancy[]>(ref reader),
                    "VeterancyUsageRequirement" => JsonSerializer.Deserialize<ModPackage.FactionData.FactionAbility.VeterancyRequirement?>(ref reader),
                    "VeterancyExperienceGain" => reader.GetSingle(),
                    _ => throw new NotImplementedException(prop)
                };
            }

            // Return
            return new(__lookup.GetCastValueOrDefault("Blueprint", string.Empty),
                __lookup.GetCastValueOrDefault("LockoutBlueprint", string.Empty),
                __lookup.GetCastValueOrDefault("AbilityCategory", AbilityCategory.Undefined),
                __lookup.GetCastValueOrDefault("MaxUsePerMatch", 0),
                __lookup.GetCastValueOrDefault("RequireOffmap", false),
                __lookup.GetCastValueOrDefault("OffmapCountEffectivenesss", 0.0f),
                __lookup.GetCastValueOrDefault("CanGrantVeterancy", false),
                __lookup.GetCastValueOrDefault("VeterancyRanks", Array.Empty<ModPackage.FactionData.FactionAbility.AbilityVeterancy>()),
                __lookup.GetCastValueOrDefault<string, object, ModPackage.FactionData.FactionAbility.VeterancyRequirement?>("VeterancyUsageRequirement", null),
                __lookup.GetCastValueOrDefault("VeterancyExperienceGain", 0.0f));

        }

        public override void Write(Utf8JsonWriter writer, ModPackage.FactionData.FactionAbility value, JsonSerializerOptions options)
            => throw new NotSupportedException();

    }

}