﻿using System;
using System.Collections.Generic;
using System.Linq;

using Battlegrounds.Functional;
using Battlegrounds.Game.Database;
using Battlegrounds.Game.Database.Management;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Modding;

namespace Battlegrounds.Game.DataCompany {

    /// <summary>
    /// Builder class for building a <see cref="Squad"/> instance with serial-style methods. Can be cleared for re-use.
    /// </summary>
    public class UnitBuilder {

        ushort m_overrideIndex = ushort.MaxValue;
        bool m_hasOverrideIndex = false;

        byte m_vetrank;
        float m_vetexperience;
        bool m_isCrew;
        string m_customName;
        ModGuid m_modGuid;
        TimeSpan m_combatTime;
        SquadBlueprint m_blueprint;
        SquadBlueprint m_transportBlueprint;
        DeploymentMethod m_deploymentMethod;
        DeploymentPhase m_deploymentPhase;
        UnitBuilder m_crewBuilder;

        readonly HashSet<UpgradeBlueprint> m_upgrades;
        readonly HashSet<SlotItemBlueprint> m_slotitems;
        readonly HashSet<Modifier> m_modifiers;

        /// <summary>
        /// Get the current blueprint of the unit.
        /// </summary>
        public SquadBlueprint Blueprint => this.m_blueprint;

        /// <summary>
        /// Get the override index of the unit (0 if none)
        /// </summary>
        public ushort OverrideIndex => this.m_overrideIndex;

        /// <summary>
        /// New basic <see cref="UnitBuilder"/> instance of for building a <see cref="Squad"/>.
        /// </summary>
        public UnitBuilder() {
            this.m_modGuid = ModGuid.BaseGame;
            this.m_blueprint = null;
            this.m_transportBlueprint = null;
            this.m_crewBuilder = null;
            this.m_upgrades = new HashSet<UpgradeBlueprint>();
            this.m_slotitems = new HashSet<SlotItemBlueprint>();
            this.m_modifiers = new HashSet<Modifier>();
            this.m_deploymentMethod = DeploymentMethod.None;
            this.m_deploymentPhase = DeploymentPhase.PhaseNone;
            this.m_combatTime = TimeSpan.Zero;
            this.m_isCrew = false;
            this.m_customName = string.Empty;
        }

        /// <summary>
        /// New <see cref="UnitBuilder"/> instance based on the settings of an already built <see cref="Squad"/> instance.
        /// </summary>
        /// <param name="squad">The <see cref="Squad"/> instance to copy the unit data from.</param>
        /// <param name="overrideIndex">Should the built squad keep the index from <paramref name="squad"/>.</param>
        /// <remarks>This will not modify the <see cref="Squad"/> instance.</remarks>
        public UnitBuilder(Squad squad, bool overrideIndex = true) {

            this.m_hasOverrideIndex = overrideIndex;
            this.m_overrideIndex = squad.SquadID;
            this.m_modifiers = squad.Modifiers.ToHashSet();
            this.m_upgrades = squad.Upgrades.Select(x => x as UpgradeBlueprint).ToHashSet();
            this.m_slotitems = squad.SlotItems.Select(x => x as SlotItemBlueprint).ToHashSet();
            this.m_vetexperience = squad.VeterancyProgress;
            this.m_vetrank = squad.VeterancyRank;
            this.m_blueprint = squad.SBP;
            this.m_transportBlueprint = squad.SupportBlueprint as SquadBlueprint;
            this.m_deploymentPhase = squad.DeploymentPhase;
            this.m_deploymentMethod = squad.DeploymentMethod;
            this.m_modGuid = squad.SBP?.PBGID.Mod ?? ModGuid.BaseGame;
            this.m_combatTime = squad.CombatTime;
            this.m_isCrew = squad.IsCrew;
            this.m_customName = squad.CustomName;
            if (squad.Crew != null) {
                this.m_crewBuilder = new UnitBuilder(squad.Crew, overrideIndex);
            }

        }

        /// <summary>
        /// Set the tuning pack GUID this unit should be based on.
        /// </summary>
        /// <param name="guid">The GUID (in coh2 string format).</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public UnitBuilder SetModGUID(string guid) {
            this.m_modGuid = ModGuid.FromGuid(guid);
            return this;
        }

        /// <summary>
        /// Set the tuning pack GUID this unit should be based on.
        /// </summary>
        /// <param name="guid">The GUID (in coh2 string format).</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public UnitBuilder SetModGUID(ModGuid guid) {
            this.m_modGuid = guid;
            return this;
        }

        /// <summary>
        /// Copy the current <see cref="UnitBuilder"/> instance values into a new <see cref="UnitBuilder"/> instance.
        /// </summary>
        /// <returns>A cloned instance of the <see cref="UnitBuilder"/> instance.</returns>
        public virtual UnitBuilder Clone()
            => new UnitBuilder(this.Build(0), this.m_hasOverrideIndex);

        /// <summary>
        /// Set the veterancy rank of the <see cref="Squad"/> instance being built.
        /// </summary>
        /// <param name="level">The veterancy rank in byte-range to set.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetVeterancyRank(byte level) {
            this.m_vetrank = level;
            return this;
        }

        /// <summary>
        /// Set the veterancy progress of the <see cref="Squad"/> instance being built.
        /// </summary>
        /// <param name="experience">The veterancy progress to set.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetVeterancyExperience(float experience) {
            this.m_vetexperience = experience;
            return this;
        }

        /// <summary>
        /// Set the <see cref="SquadBlueprint"/> the <see cref="Squad"/> instance being built will use.
        /// </summary>
        /// <param name="sbp">The <see cref="SquadBlueprint"/> to set.</param>
        /// <remarks>This must be set before certain other methods.</remarks>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetBlueprint(SquadBlueprint sbp) {
            this.m_blueprint = sbp;
            return this;
        }

        /// <summary>
        /// Set the <see cref="SquadBlueprint"/> the <see cref="Squad"/> instance being built will use.
        /// </summary>
        /// /// <remarks>
        /// This must be called before certain other methods.
        /// </remarks>
        /// <param name="localBPID">The local property bag ID given to the blueprint.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetBlueprint(ushort localBPID) {
            if (localBPID == BlueprintManager.InvalidLocalBlueprint) {
                throw new ArgumentNullException(nameof(localBPID), "Cannot set unit blueprint to null!");
            }
            this.m_blueprint = BlueprintManager.GetCollection<SquadBlueprint>().FilterByMod(this.m_modGuid).Single(x => x.ModPBGID == localBPID);
            if (this.m_blueprint is null) {
                throw new ArgumentException($"Failed to find blueprint with mod PBGID {localBPID}", nameof(localBPID));
            }
            return this;
        }

        /// <summary>
        /// Set the <see cref="SquadBlueprint"/> the <see cref="Squad"/> instance being built will use.
        /// </summary>
        /// <remarks>
        /// This must be called before certain other methods.
        /// </remarks>
        /// <param name="sbpName">The blueprint name to use when finding the <see cref="Blueprint"/>.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetBlueprint(string sbpName) {
            this.m_blueprint = BlueprintManager.FromBlueprintName<SquadBlueprint>(sbpName);
            return this;
        }

        /// <summary>
        /// Set the transport <see cref="SquadBlueprint"/> of the <see cref="Squad"/> instance being built will use when entering the battlefield.
        /// </summary>
        /// <remarks>
        /// This must be called before certain other methods.
        /// </remarks>
        /// <param name="sbp">The <see cref="SquadBlueprint"/> to set.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetTransportBlueprint(SquadBlueprint sbp) {
            this.m_transportBlueprint = sbp;
            return this;
        }

        /// <summary>
        /// Set the transport <see cref="SquadBlueprint"/> of the <see cref="Squad"/> instance being built will use when entering the battlefield.
        /// </summary>
        /// <remarks>
        /// This must be called before certain other methods.
        /// </remarks>
        /// <param name="localBPID">The local property bag ID given to the blueprint.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetTransportBlueprint(ushort localBPID) {
            if (localBPID == BlueprintManager.InvalidLocalBlueprint) {
                this.m_transportBlueprint = null;
                return this;
            }
            this.m_transportBlueprint = BlueprintManager.GetCollection<SquadBlueprint>()
                .FilterByMod(this.m_modGuid)
                .Single(x => x.ModPBGID == localBPID);
            if (this.m_transportBlueprint is null) {
                throw new ArgumentException($"Failed to find blueprint with mod PBGID {localBPID}", nameof(localBPID));
            }
            return this;
        }

        /// <summary>
        /// Set the transport <see cref="SquadBlueprint"/> of the <see cref="Squad"/> instance being built will use when entering the battlefield.
        /// </summary>
        /// <remarks>
        /// This must be called before certain other methods.
        /// </remarks>
        /// <param name="sbpName">The blueprint name to use when finding the <see cref="Blueprint"/>.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetTransportBlueprint(string sbpName) {
            this.m_transportBlueprint = BlueprintManager.FromBlueprintName(sbpName, BlueprintType.SBP) as SquadBlueprint;
            return this;
        }

        /// <summary>
        /// Set the <see cref="DeploymentMethod"/> to use when the <see cref="Squad"/> instance being built is deployed.
        /// </summary>
        /// <param name="method">The <see cref="DeploymentMethod"/> to use when deploying.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetDeploymentMethod(DeploymentMethod method) {
            if (this.m_transportBlueprint == null && method >= DeploymentMethod.DeployAndExit) {
                throw new InvalidOperationException();
            }
            this.m_deploymentMethod = method;
            return this;
        }

        /// <summary>
        /// Set the <see cref="DeploymentPhase"/> the <see cref="Squad"/> instance being built may be deployed in.
        /// </summary>
        /// <param name="phase">The <see cref="DeploymentPhase"/> to set.</param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder SetDeploymentPhase(DeploymentPhase phase) {
            this.m_deploymentPhase = phase;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upb"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddUpgrade(UpgradeBlueprint upb) {
            this.m_upgrades.Add(upb);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upbs"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddUpgrade(UpgradeBlueprint[] upbs) {
            upbs.ForEach(x => this.m_upgrades.Add(x));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upb"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddUpgrade(string upb) {
            this.m_upgrades.Add(BlueprintManager.FromBlueprintName(upb, BlueprintType.UBP) as UpgradeBlueprint);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upbs"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddUpgrade(string[] upbs) {
            upbs.ForEach(x => this.AddUpgrade(x));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ibp"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddSlotItem(SlotItemBlueprint ibp) {
            this.m_slotitems.Add(ibp);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ibp"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddSlotItem(string ibp) {
            this.m_slotitems.Add(BlueprintManager.FromBlueprintName(ibp, BlueprintType.IBP) as SlotItemBlueprint);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ibps"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddSlotItem(string[] ibps) {
            ibps.ForEach(x => this.AddSlotItem(x));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifier"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder AddModifier(Modifier modifier) {
            this.m_modifiers.Add(modifier);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ubp"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder RemoveUpgrade(UpgradeBlueprint ubp) {
            this.m_upgrades.Remove(ubp);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ibp"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder RemoveSlotItem(SlotItemBlueprint ibp) {
            this.m_slotitems.Remove(ibp);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modifier"></param>
        /// <returns>The modified instance the method is invoked with.</returns>
        public virtual UnitBuilder RemoveModifier(Modifier modifier) {
            this.m_modifiers.Remove(modifier);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customName"></param>
        public virtual UnitBuilder SetCustomName(string customName) {
            this.m_customName = customName;
            return this;
        }

        /// <summary>
        /// Create or get a new <see cref="UnitBuilder"/> instance representing the <see cref="Squad"/> crew of the current <see cref="UnitBuilder"/> instance.
        /// </summary>
        /// <returns>The vehicle crew <see cref="UnitBuilder"/> instance for  the (vehicle/crewable) <see cref="UnitBuilder"/> instance.</returns>
        /// <exception cref="InvalidOperationException"/>
        public virtual UnitBuilder CreateAndGetCrew(Func<UnitBuilder, UnitBuilder> builder) {
            if (this.m_blueprint == null) {
                throw new InvalidOperationException("Attempt to create a crew for a unit without a blueprint.");
            }
            if (!this.m_blueprint.HasCrew) {
                throw new InvalidOperationException("Attempt to create a crew for a unit that does not support crews.");
            }
            if (this.m_crewBuilder is null) {
                var crewDefaultBP = this.m_blueprint.GetCrewBlueprint();
                var crewBuilder = new UnitBuilder().SetModGUID(this.m_modGuid);
                if (crewDefaultBP is not null) {
                    crewBuilder.SetBlueprint(crewDefaultBP);
                }
                this.m_crewBuilder = builder?.Invoke(crewBuilder) ?? crewBuilder;
            }
            this.m_crewBuilder.m_isCrew = true;
            return this;
        }

        public virtual UnitBuilder SetCrew(Squad squad, bool overrideIndex = true) {
            if (this.m_blueprint == null) {
                throw new InvalidOperationException("Attempt to create a crew for a unit without a blueprint.");
            }
            if (!this.m_blueprint.HasCrew) {
                throw new InvalidOperationException("Attempt to create a crew for a unit that does not support crews.");
            }
            this.m_crewBuilder = new(squad, overrideIndex);
            return this;
        }

        /// <summary>
        /// Build the <see cref="Squad"/> instance using the data collected with the <see cref="UnitBuilder"/>. The ID will be copied from the original <see cref="Squad"/> if possible.
        /// </summary>
        /// <param name="ID">The unique ID to use when creating the <see cref="Squad"/> instance.</param>
        /// <returns>A <see cref="Squad"/> instance with all the parameters defined by the <see cref="UnitBuilder"/>.</returns>
        public virtual Squad Build(ushort ID) {

            if (this.m_hasOverrideIndex) {
                ID = this.m_overrideIndex;
            }

            Squad squad = new Squad(ID, null, this.m_blueprint);
            squad.SetName(this.m_customName);
            squad.SetDeploymentMethod(this.m_transportBlueprint, this.m_deploymentMethod, this.m_deploymentPhase);
            squad.SetVeterancy(this.m_vetrank, this.m_vetexperience);
            squad.SetCombatTime(this.m_combatTime);
            squad.SetIsCrew(this.m_isCrew);

            if (this.m_blueprint?.HasCrew ?? false && this.m_crewBuilder is null) {
                this.CreateAndGetCrew(null);
            }

            if (this.m_crewBuilder is not null) {
                squad.SetCrew(this.m_crewBuilder.Build((ushort)(ID + 1)));
            }

            this.m_upgrades.ToArray().ForEach(x => squad.AddUpgrade(x));
            this.m_slotitems.ToArray().ForEach(x => squad.AddSlotItem(x));
            this.m_modifiers.ToArray().ForEach(x => squad.AddModifier(x));

            return squad;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public virtual UnitBuilder SetCombatTime(TimeSpan span) {
            this.m_combatTime = span;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isCrew"></param>
        /// <returns></returns>
        public virtual UnitBuilder SetIsCrew(bool isCrew) {
            this.m_isCrew = isCrew;
            return this;
        }

        /// <summary>
        /// Reset all the values set by the <see cref="UnitBuilder"/>.
        /// </summary>
        public virtual void Reset() {

            this.m_blueprint = null;
            this.m_crewBuilder = null;
            this.m_deploymentMethod = DeploymentMethod.None;
            this.m_deploymentPhase = DeploymentPhase.PhaseNone;
            this.m_hasOverrideIndex = false;
            this.m_modifiers.Clear();
            this.m_overrideIndex = 0;
            this.m_slotitems.Clear();
            this.m_transportBlueprint = null;
            this.m_upgrades.Clear();
            this.m_vetexperience = 0;
            this.m_vetrank = 0;
            this.m_customName = null;
            this.m_combatTime = TimeSpan.Zero;

        }

        /// <summary>
        /// Clone self and resets the current instance.
        /// </summary>
        /// <returns>The cloned instance.</returns>
        public UnitBuilder GetAndReset() {
            UnitBuilder clone = this.Clone();
            this.Reset();
            return clone;
        }

    }

}
