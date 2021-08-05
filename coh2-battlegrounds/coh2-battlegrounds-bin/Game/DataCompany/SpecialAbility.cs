﻿using Battlegrounds.Game.Database;
using Battlegrounds.Game.Scar;

namespace Battlegrounds.Game.DataCompany {

    /// <summary>
    /// Special ability category an ability may belong to.
    /// </summary>
    public enum SpecialAbilityCategory {

        /// <summary>
        /// The category is undefined (Will not be compiled).
        /// </summary>
        Undefined,

        /// <summary>
        /// Show up as a "commander" ability (Currently not compiled - CoDiEx 05/08/21).
        /// </summary>
        Default,

        /// <summary>
        /// Be included as an artillery upgrade.
        /// </summary>
        Artillery,

        /// <summary>
        /// Be included as an air support.
        /// </summary>
        AirSupport,

    }

    /// <summary>
    /// Represents a <see cref="SpecialAbility"/> ingame ability. Implements <see cref="IScarValue"/>.
    /// </summary>
    public class SpecialAbility : IScarValue {

        /// <summary>
        /// The <see cref="AbilityBlueprint"/> being granted by the <see cref="SpecialAbility"/>.
        /// </summary>
        public AbilityBlueprint ABP { get; }

        /// <summary>
        /// The <see cref="SpecialAbilityCategory"/> the <see cref="SpecialAbility"/> will belong to.
        /// </summary>
        public SpecialAbilityCategory Category { get; }

        /// <summary>
        /// The amount of uses a player has during each match.
        /// </summary>
        public int MaxUse { get; }

        /// <summary>
        /// Get or set the amount of times this special ability has been used.
        /// </summary>
        public int UsedCount { get; set; }

        /// <summary>
        /// Instantiate a new <see cref="SpecialAbility"/> with predefined <see cref="SpecialAbilityCategory"/> and use count.
        /// </summary>
        /// <param name="blueprint">The ability blueprint.</param>
        /// <param name="category">The category.</param>
        /// <param name="maxUse">The maximum amount of uses each match.</param>
        /// <param name="count">The amount of times this has been used.</param>
        public SpecialAbility(AbilityBlueprint blueprint, SpecialAbilityCategory category, int maxUse, int count = 0) {
            this.ABP = blueprint;
            this.Category = category;
            this.MaxUse = maxUse;
            this.UsedCount = count;
        }

        public string ToScar() => $"{{ abp = {this.ABP.ToScar()}, max_use = {this.MaxUse} }}";

    }

}
