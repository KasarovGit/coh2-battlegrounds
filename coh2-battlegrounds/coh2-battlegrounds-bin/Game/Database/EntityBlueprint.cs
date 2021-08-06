﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Battlegrounds.Functional;
using Battlegrounds.Game.Database.Extensions;
using Battlegrounds.Game.Database.Management;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Modding;

namespace Battlegrounds.Game.Database {

    /// <summary>
    /// Representation of a <see cref="Blueprint"/> with <see cref="Entity"/> specific values. Inherits from <see cref="Blueprint"/>. This class cannot be inheritted.
    /// </summary>
    [JsonConverter(typeof(EntityBlueprintConverter))]
    public sealed class EntityBlueprint : Blueprint, IUIBlueprint {

        /// <summary>
        /// The unique PropertyBagGroupdID assigned to this blueprint.
        /// </summary>
        public override BlueprintUID PBGID { get; }

        public override BlueprintType BlueprintType => BlueprintType.EBP;

        public override string Name { get; }

        public CostExtension Cost { get; }

        public UIExtension UI { get; }

        public DriverExtension Drivers { get; }

        public Faction Faction { get; }

        public string[] Abilities { get; }

        public string[] Hardpoints { get; }

        public string[] Upgrades { get; }

        public string[] AppliedUpgrades { get; }

        public int UpgradeCapacity { get; }

        public float Health { get; }

        public EntityBlueprint(string name, BlueprintUID pbgid, Faction faction,
            CostExtension cost, UIExtension ui, DriverExtension driverExtension,
            string[] abilities, string[] upgrades, string[] appliedUpgrades, int upgradeMax, string[] hardpoints, float health) {
            this.Name = name;
            this.PBGID = pbgid;
            this.UI = ui;
            this.Cost = cost;
            this.Faction = faction;
            this.Abilities = abilities;
            this.Hardpoints = hardpoints;
            this.Health = health;
            this.Drivers = driverExtension;
            this.UpgradeCapacity = upgradeMax;
            this.Upgrades = upgrades;
            this.AppliedUpgrades = appliedUpgrades;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="faction"></param>
        /// <returns></returns>
        public SquadBlueprint GetVehicleSquadBlueprint(Faction faction) {
            
            // Bail if no driver extension
            if (this.Drivers is null) {
                return null;
            }

            // Set default faction if none
            if (faction is null) {
                faction = this.Faction;
            }

            // Get driver from driver extension
            if (this.Drivers.GetCaptureSquad(faction) is SquadBlueprint sbp) {
                return sbp;
            } else if (faction == this.Faction) {
                return null;
            }

            // Get driver of null
            return this.Drivers.GetCaptureSquad(this.Faction);

        }

    }

    public class EntityBlueprintConverter : JsonConverter<EntityBlueprint> {

        public override EntityBlueprint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            Dictionary<string, object> __lookup = new();
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {
                string prop = reader.ReadProperty();
                __lookup[prop] = prop switch {
                    "Cost" => CostExtension.FromJson(ref reader),
                    "Display" => UIExtension.FromJson(ref reader),
                    "Army" => reader.GetString(),
                    "PBGID" => reader.GetUInt64(),
                    "Name" => reader.GetString(),
                    "ModGUID" => reader.GetString(),
                    "Health" => reader.GetSingle(),
                    "Abilities" => reader.GetStringArray(),
                    "Hardpoints" => reader.GetStringArray(),
                    "Drivers" => DriverExtension.FromJson(ref reader),
                    "UpgradeCapacity" => reader.GetInt32(),
                    "Upgrades" => reader.GetStringArray(),
                    "AppliedUpgrades" => reader.GetStringArray(),
                    _ => throw new NotImplementedException(prop)
                };
            }
            Faction fac = __lookup.GetValueOrDefault("Army", "NULL") is "NULL" ? null : Faction.FromName(__lookup.GetValueOrDefault("Army", "NULL"));
            ModGuid modguid = __lookup.ContainsKey("ModGUID") ? ModGuid.FromGuid(__lookup["ModGUID"] as string) : ModGuid.BaseGame;
            BlueprintUID pbgid = new BlueprintUID(__lookup.GetValueOrDefault("PBGID", 0ul), modguid);
            return new(__lookup.GetValueOrDefault("Name", string.Empty),
                pbgid,
                fac,
                __lookup.GetValueOrDefault("Cost", new CostExtension()),
                 __lookup.GetValueOrDefault("Display", new UIExtension()),
                 __lookup.GetValueOrDefault("Drivers", new DriverExtension(Array.Empty<DriverExtension.Entry>())),
                 __lookup.GetValueOrDefault("Abilities", Array.Empty<string>()),
                 __lookup.GetValueOrDefault("Upgrades", Array.Empty<string>()),
                 __lookup.GetValueOrDefault("AppliedUpgrades", Array.Empty<string>()),
                 __lookup.GetValueOrDefault("UpgradeCapacity", 0),
                 __lookup.GetValueOrDefault("Hardpoints", Array.Empty<string>()),
                  (float)__lookup.GetValueOrDefault("Health", 0.0f));
        }

        public override void Write(Utf8JsonWriter writer, EntityBlueprint value, JsonSerializerOptions options) => writer.WriteStringValue(value.Name);

    }

}
