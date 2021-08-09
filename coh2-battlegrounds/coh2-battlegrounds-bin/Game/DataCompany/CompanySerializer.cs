﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Battlegrounds.Functional;
using Battlegrounds.Game.Database;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Modding;
using Battlegrounds.Verification;

namespace Battlegrounds.Game.DataCompany {

    /// <summary>
    /// Class for serializing a <see cref="Company"/> to and from json.
    /// </summary>
    public class CompanySerializer : JsonConverter<Company> {

        public override Company Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {

            // Open object
            reader.Read();

            // Read company name (if not there, return null)
            string name = ReadProperty(ref reader, nameof(Company.Name));
            if (name is null) {
                return null;
            }

            // Read company army (if not there, return null)
            string army = ReadProperty(ref reader, nameof(Company.Army));
            if (army is null) {
                return null;
            }

            // Create builder instance
            CompanyBuilder builder = new CompanyBuilder()
                .NewCompany(Faction.FromName(army))
                .ChangeName(name)
                .ChangeTuningMod(ModGuid.FromGuid(ReadProperty(ref reader, nameof(Company.TuningGUID))))
                .ChangeAppVersion(ReadProperty(ref reader, nameof(Company.AppVersion)));

            // Read checksum
            string checksum = ReadProperty(ref reader, nameof(Company.Checksum));

            // Read type(s)
            builder.ChangeType(Enum.Parse<CompanyType>(ReadProperty(ref reader, nameof(Company.Type))))
                .ChangeAvailability(Enum.Parse<CompanyAvailabilityType>(ReadProperty(ref reader, nameof(Company.AvailabilityType))));

            // Read statistics
            var stats = ReadPropertyThroughSerialisation<CompanyStatistics>(ref reader, nameof(Company.Statistics));

            // Create helper dictionary
            var arrayTypes = new Dictionary<string, Type>() {
                [nameof(Company.Abilities)] = typeof(Ability[]),
                [nameof(Company.Units)] = typeof(Squad[]),
                [nameof(Company.Upgrades)] = typeof(UpgradeBlueprint[]),
                [nameof(Company.Modifiers)] = typeof(Modifier[]),
            };

            // Read arrays
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {

                string property = reader.GetString();
                var inputType = arrayTypes[property];

                // Make sure we're reading an array
                if (reader.Read() && reader.TokenType is JsonTokenType.StartArray) {

                    // Read values and store them
                    Array values = JsonSerializer.Deserialize(ref reader, inputType) as Array;
                    switch (property) {
                        case nameof(Company.Units):
                            for (int i = 0; i < values.Length; i++) {
                                builder.AddAndCommitUnit(new UnitBuilder(values.GetValue(i) as Squad, false));
                            }
                            break;
                        case nameof(Company.Abilities):
                            for (int i = 0; i < values.Length; i++) {
                                builder.AddAndCommitAbility(values.GetValue(i) as Ability);
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                } else {
                    break;
                }

            }

            // Commit
            builder.Commit();

            // Verify checksum and return if success; otherwise throw checksum violation error
            Company result = builder.Result;
            result.UpdateStatistics(x => stats); // This will set the stats
            result.CalculateChecksum();
            return result.VerifyChecksum(checksum) ? result : throw new ChecksumViolationException();

        }

        private static string ReadProperty(ref Utf8JsonReader reader, string property)
            => reader.GetString() == property && reader.Read() ? reader.ReadProperty() : null;

        private static T ReadPropertyThroughSerialisation<T>(ref Utf8JsonReader reader, string property)
            => reader.GetString() == property && reader.Read() ? JsonSerializer.Deserialize<T>(ref reader) : default;

        public override void Write(Utf8JsonWriter writer, Company value, JsonSerializerOptions options) {

            // Recalculate the checksum
            value.CalculateChecksum();

            // Begin object
            writer.WriteStartObject();

            // Write all the data
            writer.WriteString(nameof(Company.Name), value.Name);
            writer.WriteString(nameof(Company.Army), value.Army.Name);
            writer.WriteString(nameof(Company.TuningGUID), value.TuningGUID.GUID);
            writer.WriteString(nameof(Company.AppVersion), value.AppVersion);
            writer.WriteString(nameof(Company.Checksum), value.Checksum);
            writer.WriteString(nameof(Company.Type), value.Type.ToString());
            writer.WriteString(nameof(Company.AvailabilityType), value.AvailabilityType.ToString());

            // Write statistics
            writer.WritePropertyName(nameof(Company.Statistics));
            JsonSerializer.Serialize(writer, value.Statistics, options);

            // Write Units
            writer.WritePropertyName(nameof(Company.Units));
            JsonSerializer.Serialize(writer, value.Units, options);

            // Write abilities
            if (value.Abilities.Length > 0) {
                writer.WritePropertyName(nameof(Company.Abilities));
                JsonSerializer.Serialize(writer, value.Abilities, options);
            }

            // Write inventory
            if (value.Inventory.Length > 0) {
                writer.WritePropertyName(nameof(Company.Inventory));
                JsonSerializer.Serialize(writer, value.Inventory, options);
            }

            // Write upgrades
            if (value.Upgrades.Length > 0) {
                writer.WritePropertyName(nameof(Company.Upgrades));
                JsonSerializer.Serialize(writer, value.Upgrades, options);
            }

            // Write modifiers
            if (value.Modifiers.Length > 0) {
                writer.WritePropertyName(nameof(Company.Modifiers));
                JsonSerializer.Serialize(writer, value.Modifiers, options);
            }

            // Close company object
            writer.WriteEndObject();

        }

        /// <summary>
        /// Get a company in its json format.
        /// </summary>
        /// <param name="company">The company to serialise.</param>
        /// <param name="indent">Should the json be indent formatted.</param>
        /// <returns>The json string data.</returns>
        public static string GetCompanyAsJson(Company company, bool indent = true)
            => JsonSerializer.Serialize(company, new JsonSerializerOptions() { WriteIndented = indent });

        /// <summary>
        /// Get a company from raw json data.
        /// </summary>
        /// <param name="rawJsonData">The raw json data to parse.</param>
        /// <returns>The company built from the <paramref name="rawJsonData"/>. Will be <see langword="null"/> if deserialization fails.</returns>
        public static Company GetCompanyFromJson(string rawJsonData)
            => JsonSerializer.Deserialize<Company>(rawJsonData);

    }

}
