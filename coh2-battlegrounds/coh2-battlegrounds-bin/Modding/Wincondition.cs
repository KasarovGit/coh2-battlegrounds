﻿using System;

namespace Battlegrounds.Modding {

    /// <summary>
    /// Readonly struct representing an option available in a <see cref="IWinconditionMod"/>.
    /// </summary>
    public readonly struct WinconditionOption : IGamemodeOption {

        /// <summary>
        /// The display title of the option.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The backing value of the option.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Initialize a new <see cref="WinconditionOption"/> with a display name and backing value.
        /// </summary>
        /// <param name="title">The name to display when picking the option.</param>
        /// <param name="val">The integer backing value that is used to represent the option in code.</param>
        public WinconditionOption(string title, int val) {
            this.Title = title;
            this.Value = val;
        }

        public void Deconstruct(out string title, out int value) {
            title = this.Title;
            value = this.Value;
        }

        public override string ToString() => this.Title;

    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Wincondition : IGamemode {

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public ModGuid Guid { get; }

        /// <summary>
        /// 
        /// </summary>
        public IGamemodeOption[] Options { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public int DefaultOptionIndex { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayName { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public string DisplayShortDescription { get; init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="guid"></param>
        public Wincondition(string name, Guid guid) : this(name, ModGuid.FromGuid(guid)) {}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="guid"></param>
        public Wincondition(string name, ModGuid guid) {
            this.Name = name;
            this.Guid = guid;
            this.DefaultOptionIndex = 0;
            this.DisplayName = name;
        }

        public override string ToString() => this.DisplayName;

    }

}
