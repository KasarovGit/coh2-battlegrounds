﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

using Battlegrounds.Game.Database;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Modding;
using Battlegrounds.Verification;

namespace Battlegrounds.Game.DataCompany {

    /// <summary>
    /// Represents a Company. Implements <see cref="IChecksumItem"/>.
    /// </summary>
    [JsonConverter(typeof(CompanySerializer))]
    public class Company : IChecksumItem {

        /// <summary>
        /// The maximum size of a company.
        /// </summary>
        public const int MAX_SIZE = 40;

        /// <summary>
        /// The max amount of initially deployed units.
        /// </summary>
        public const int MAX_INITIAL = 5;

        private string m_checksum;
        private string m_lastEditVersion;
        private ushort m_nextSquadId;
        private CompanyType m_companyType;
        private CompanyAvailabilityType m_availabilityType;
        private List<Squad> m_squads;
        private List<Blueprint> m_inventory;
        private List<Modifier> m_modifiers;
        private List<UpgradeBlueprint> m_upgrades;
        private List<SpecialAbility> m_abilities;
        private CompanyStatistics m_companyStatistics;

        /// <summary>
        /// Get the name of the company.
        /// </summary>
        [ChecksumProperty]
        public string Name { get; set; }

        /// <summary>
        /// Get the calculated strength of the company.
        /// </summary>
        public double Strength => this.GetStrength();

        /// <summary>
        /// Get the <see cref="CompanyType"/> that can be used to describe the <see cref="Company"/> characteristics.
        /// </summary>
        [ChecksumProperty]
        public CompanyType Type => this.m_companyType;

        /// <summary>
        /// Get the <see cref="CompanyAvailabilityType"/> that will determine when the company is available.
        /// </summary>
        [ChecksumProperty]
        public CompanyAvailabilityType AvailabilityType => this.m_availabilityType;

        /// <summary>
        /// Get the version that was used to generate this company.
        /// </summary>
        [ChecksumProperty]
        public string AppVersion => this.m_lastEditVersion;

        /// <summary>
        /// Get tthe GUID of the tuning mod used to create this company.
        /// </summary>
        [ChecksumProperty]
        public ModGuid TuningGUID { get; set; }

        /// <summary>
        /// Get the <see cref="Faction"/> this company is associated with.
        /// </summary>
        [ChecksumProperty]
        public Faction Army { get; }

        /// <summary>
        /// Get the display name of who owns the <see cref="Company"/>. This property is used by the compilers. This will be overriden.
        /// </summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Get the <see cref="ImmutableArray{T}"/> representation of the units in the <see cref="Company"/>.
        /// </summary>
        [ChecksumProperty(IsCollection = true)]
        public ImmutableArray<Squad> Units => this.m_squads.ToImmutableArray();

        /// <summary>
        /// Get the <see cref="ImmutableArray{T}"/> representation of a <see cref="Company"/> inventory of stored <see cref="Blueprint"/> objects.
        /// </summary>
        [ChecksumProperty(IsCollection = true)]
        public ImmutableArray<Blueprint> Inventory => this.m_inventory.ToImmutableArray();

        /// <summary>
        /// Get the <see cref="ImmutableArray{T}"/> representation of a <see cref="Company"/>'s upgrade list.
        /// </summary>
        [ChecksumProperty(IsCollection = true)]
        public ImmutableArray<UpgradeBlueprint> Upgrades => this.m_upgrades.ToImmutableArray();

        /// <summary>
        /// Get the <see cref="ImmutableArray{T}"/> representation of a <see cref="Company"/>'s modifier list.
        /// </summary>
        [ChecksumProperty(IsCollection = true)]
        public ImmutableArray<Modifier> Modifiers => this.m_modifiers.ToImmutableArray();

        /// <summary>
        /// Get the <see cref="ImmutableArray{T}"/> representation of a <see cref="Company"/>'s special ability list.
        /// </summary>
        [ChecksumProperty(IsCollection = true)]
        public ImmutableArray<SpecialAbility> Abilities => this.m_abilities.ToImmutableArray();

        /// <summary>
        /// Get the statistics tied to the <see cref="Company"/>.
        /// </summary>
        [ChecksumProperty]
        public CompanyStatistics Statistics => this.m_companyStatistics;

        /// <summary>
        /// Get the calculated company checksum.
        /// </summary>
        public string Checksum => this.m_checksum;

        /// <summary>
        /// New empty <see cref="Company"/> instance.
        /// </summary>
        public Company(Faction faction) {
            this.Army = faction;
            this.m_squads = new List<Squad>();
            this.m_inventory = new List<Blueprint>();
            this.m_modifiers = new List<Modifier>();
            this.m_upgrades = new List<UpgradeBlueprint>();
            this.m_abilities = new List<SpecialAbility>();
            this.m_companyType = CompanyType.Unspecified;
            this.m_companyStatistics = new CompanyStatistics();
            this.m_lastEditVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public ushort AddSquad(UnitBuilder builder) {
            ushort id = this.m_nextSquadId++;
            Squad squad = builder.Build(id);
            if (squad.Crew != null) {
                this.m_nextSquadId++;
            }
            this.m_squads.Add(squad);
            return id;
        }

        /// <summary>
        /// Get the company <see cref="Squad"/> by its company index.
        /// </summary>
        /// <param name="squadID">The index of the squad to get</param>
        /// <returns>The squad with squad id matching requested squad ID or null.</returns>
        public Squad GetSquadByIndex(ushort squadID) {
            Squad s = this.m_squads.FirstOrDefault(x => x.SquadID == squadID);
            if (s == null) { s = this.m_squads.FirstOrDefault(x => x.Crew.SquadID == squadID)?.Crew; }
            return s;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="squadId"></param>
        public bool RemoveSquad(ushort squadId)
            => this.m_squads.RemoveAll(x => x.SquadID == squadId) == 1;

        /// <summary>
        /// Reset the squads of the company.
        /// </summary>
        public void ResetSquads() {
            this.m_squads.Clear();
            this.m_nextSquadId = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blueprint"></param>
        public void AddInventoryItem(Blueprint blueprint) => this.m_inventory.Add(blueprint);

        /// <summary>
        /// Reset most of the company data, including the company inventory.
        /// </summary>
        public void ResetCompany() => this.ResetCompany(true);

        /// <summary>
        /// Reset most of the company data.
        /// </summary>
        /// <param name="resetInventory"></param>
        public void ResetCompany(bool resetInventory) {
            this.ResetSquads();
            this.m_abilities.Clear();
            if (resetInventory) {
                this.m_inventory.Clear();
            }
            this.Name = string.Empty;
            this.m_checksum = string.Empty;
        }

        /// <summary>
        /// Get the complete checksum in string format.
        /// </summary>
        /// <returns>The string representation of the checksum.</returns>
        private string GetChecksum() => new Checksum(this).GetCheckksum().ToString("X8");

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool VerifyAppVersion() => this.m_lastEditVersion.CompareTo(this.m_lastEditVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()) == 0;

        /// <summary>
        /// Verify the checksum of the object.
        /// </summary>
        /// <returns>true if the checksum is valid. False if the checksum does not match.</returns>
        public bool VerifyChecksum() {

            // Backup and reset checksum
            string checksum = this.m_checksum;
            this.m_checksum = string.Empty;

            // Calculate checksum
            string newChecksum = this.GetChecksum();
            bool result = newChecksum.CompareTo(checksum) == 0;

            // Restore checksum
            this.m_checksum = checksum;

            // Return result
            return result;

        }

        public bool VerifyChecksum(string checksum)
            => this.m_checksum == checksum;

        public void CalculateChecksum()
            => this.m_checksum = this.GetChecksum();

        public void SetType(CompanyType type) => this.m_companyType = type;

        public void SetAvailability(CompanyAvailabilityType companyAvailability) => this.m_availabilityType = companyAvailability;

        /// <summary>
        /// Calculate the strength of the company
        /// </summary>
        /// <returns></returns>
        public double GetStrength() {

            // Calculate total strength
            double total = 1.0;

            // Sum experience (weighted to max rank and squad count)
            total += this.m_squads.Aggregate(0.0, (a, b) => a + (b.VeterancyRank / 5.0) / this.m_squads.Count);

            // Modify by win/loss rate
            total *= 1.0 + (this.m_companyStatistics.WinRate - this.m_companyStatistics.LossRate);

            // Clamp to 0
            if (total < 0.0) {
                total = 0.0;
            }

            // Return total
            return total;

        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => this.Name;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateFunction"></param>
        public void UpdateStatistics(Func<CompanyStatistics, CompanyStatistics> updateFunction) => this.m_companyStatistics = updateFunction(this.m_companyStatistics);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="squadIndex"></param>
        /// <param name="squad"></param>
        public void ReplaceSquad(ushort squadIndex, Squad squad) {
            int arrIndex = this.m_squads.FindIndex(x => x.SquadID == squadIndex);
            if (arrIndex != -1) {
                this.m_squads[arrIndex] = squad;
            } else {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        public void SetAppVersion(string version) => this.m_lastEditVersion = version;

    }

}
