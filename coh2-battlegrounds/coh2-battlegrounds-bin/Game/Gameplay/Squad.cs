﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

using Battlegrounds.Game.Database;
using Battlegrounds.Json;

namespace Battlegrounds.Game.Gameplay {

    /// <summary>
    /// The method in which to deploy a <see cref="Squad"/>.
    /// </summary>
    public enum DeploymentMethod {

        /// <summary>
        /// No special method is defined (units walks unto the battlefield)
        /// </summary>
        None,

        /// <summary>
        /// The unit is transported unto the battlefield using the support blueprint. The transport will then leave the battlefield.
        /// </summary>
        DeployAndExit,

        /// <summary>
        /// The unit will be transported unto the battlefield using the support blueprint.
        /// </summary>
        DeployAndStay,

        /// <summary>
        /// The unit is dropped from the skies using a parachute.
        /// </summary>
        Paradrop,

        /// <summary>
        /// The unit is deployed using a glider.
        /// </summary>
        Glider,

    }

    /// <summary>
    /// Representation of a Squad. Implements <see cref="IJsonObject"/>.
    /// </summary>
    public class Squad : IJsonObject {

        [JsonIgnoreIfValue((byte)0)]private byte m_veterancyRank;
        [JsonIgnoreIfValue(0.0f)] private float m_veterancyProgress;

        [JsonIgnoreIfValue(false)] private bool m_isCrewSquad;
        [JsonIgnoreIfValue(DeploymentMethod.None)] private DeploymentMethod m_deployMode;
        [JsonIgnoreIfNull] private Squad m_crewSquad;
        [JsonReference(typeof(BlueprintManager))][JsonIgnoreIfNull] private Blueprint m_deployBp;

        [JsonReference(typeof(BlueprintManager))][JsonIgnoreIfEmpty] private HashSet<Blueprint> m_upgrades;
        [JsonReference(typeof(BlueprintManager))][JsonIgnoreIfEmpty] private HashSet<Blueprint> m_slotItems;
        [JsonIgnoreIfEmpty] private HashSet<Modifier> m_modifiers;

        /// <summary>
        /// The unique squad ID used to identify the <see cref="Squad"/>.
        /// </summary>
        public ushort SquadID { get; }

        /// <summary>
        /// The player who (currently) owns the <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public Player PlayerOwner { get; }

        /// <summary>
        /// The (crew if squad is a vehicle) <see cref="Database.Blueprint"/> the <see cref="Squad"/> is a type of.
        /// </summary>
        [JsonReference(typeof(BlueprintManager))]
        [JsonIgnoreIfNull]
        public Blueprint Blueprint { get; }

        /// <summary>
        /// The squad or entity <see cref="Database.Blueprint"/> to support this squad.
        /// </summary>
        [JsonIgnore]
        public Blueprint SupportBlueprint => this.m_deployBp;

        /// <summary>
        /// Deploy the unit and exit when deployed.
        /// </summary>
        [JsonIgnore]
        public DeploymentMethod DeploymentMethod => this.m_deployMode;

        /// <summary>
        /// The squad data for the crew.
        /// </summary>
        [JsonIgnore]
        public Squad Crew => this.m_crewSquad;

        /// <summary>
        /// Is the <see cref="Squad"/> the crew for another <see cref="Squad"/> instance.
        /// </summary>
        [JsonIgnore]
        public bool IsCrew => this.m_isCrewSquad;

        /// <summary>
        /// The <see cref="Blueprint"/> in a <see cref="SquadBlueprint"/> form.
        /// </summary>
        /// <exception cref="InvalidCastException"/>
        [JsonIgnore]
        public SquadBlueprint SBP => this.Blueprint as SquadBlueprint;

        /// <summary>
        /// The achieved veterancy rank of a <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public byte VeterancyRank => this.m_veterancyRank;

        /// <summary>
        /// The current veterancy progress of a <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public float VeterancyProgress => this.m_veterancyProgress;

        /// <summary>
        /// The current upgrades applied to a <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public ImmutableArray<Blueprint> Upgrades => m_upgrades.ToImmutableArray();

        /// <summary>
        /// The current slot items carried by the <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public ImmutableArray<Blueprint> SlotItems => m_slotItems.ToImmutableArray();

        /// <summary>
        /// The current modifiers applied to the <see cref="Squad"/>.
        /// </summary>
        [JsonIgnore]
        public ImmutableArray<Modifier> Modifiers => m_modifiers.ToImmutableArray();

        /// <summary>
        /// Create a basic <see cref="Squad"/> instance without any identifying values.
        /// </summary>
        public Squad() {
            this.SquadID = 0;
            this.PlayerOwner = null;
            this.Blueprint = null;
            this.m_slotItems = new HashSet<Blueprint>();
            this.m_upgrades = new HashSet<Blueprint>();
            this.m_modifiers = new HashSet<Modifier>();
            this.m_deployMode = DeploymentMethod.None;
            this.m_crewSquad = null;
        }

        /// <summary>
        /// Create new <see cref="Squad"/> instance with a unique squad ID, a <see cref="Player"/> owner and a <see cref="Database.Blueprint"/>.
        /// </summary>
        /// <param name="squadID">The unique squad ID used to identify the squad</param>
        /// <param name="owner">The <see cref="Player"/> who owns the squad</param>
        /// <param name="squadBlueprint">The <see cref="Database.Blueprint"/> the squad is an instance of</param>
        public Squad(ushort squadID, Player owner, Blueprint squadBlueprint) {
            this.SquadID = squadID;
            this.PlayerOwner = owner;
            this.Blueprint = squadBlueprint;
            this.m_slotItems = new HashSet<Blueprint>();
            this.m_upgrades = new HashSet<Blueprint>();
            this.m_modifiers = new HashSet<Modifier>();
            this.m_deployMode = DeploymentMethod.None;
            this.m_crewSquad = null;
        }

        /// <summary>
        /// Set the veterancy of the <see cref="Squad"/>. The rank and progress is not checked with the blueprint - any veterancy level can be achieved here.
        /// </summary>
        /// <param name="rank">The rank (or level) the squad has achieved.</param>
        /// <param name="progress">The current progress towards the next veterancy level</param>
        public void SetVeterancy(byte rank, float progress = 0.0f) {
            this.m_veterancyRank = rank;
            this.m_veterancyProgress = progress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transportBlueprint"></param>
        /// <param name="deployAndExit"></param>
        public void SetDeploymentMethod(Blueprint transportBlueprint, DeploymentMethod deployMode) {
            this.m_deployMode = deployMode;
            this.m_deployBp = transportBlueprint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="crew"></param>
        public void SetCrew(Squad crew) {
            this.m_crewSquad = crew;
            this.m_crewSquad.SetIsCrew(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isCrew"></param>
        public void SetIsCrew(bool isCrew) => m_isCrewSquad = isCrew;

        /// <summary>
        /// Add an upgrade to the squad
        /// </summary>
        /// <param name="upgradeBP">The upgrade blueprint to add</param>
        public void AddUpgrade(Blueprint upgradeBP) => this.m_upgrades.Add(upgradeBP);

        /// <summary>
        /// Add a slot item to the squad
        /// </summary>
        /// <param name="slotItemBP">The slot item blueprint to add</param>
        public void AddSlotItem(Blueprint slotItemBP) => this.m_slotItems.Add(slotItemBP);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifier"></param>
        public void AddModifier(Modifier modifier) => this.m_modifiers.Add(modifier);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifierName"></param>
        public void RemoveModifier(string modifierName) => this.m_modifiers.RemoveWhere(x => x.Name.CompareTo(modifierName) == 0);

        /// <summary>
        /// Calculate the actual cost of a <see cref="Squad"/>.
        /// </summary>
        /// <returns>The cost of the squad.</returns>
        public Cost GetCost() {

            Cost c = new Cost(SBP.Cost.Manpower, SBP.Cost.Munitions, SBP.Cost.Fuel, SBP.Cost.FieldTime);
            c = this.m_upgrades.Select(x => (x as UpgradeBlueprint).Cost).Aggregate(c, (a, b) => a + b);

            if (this.m_deployBp is SquadBlueprint sbp) {
                c += sbp.Cost * (this.DeploymentMethod == DeploymentMethod.DeployAndExit ? 0.15f : 0.25f);
            }

            // TODO: More here

            return c;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string ToJsonReference() => this.SquadID.ToString();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"{this.SBP.Name}${this.SquadID}";
        
    }

}
