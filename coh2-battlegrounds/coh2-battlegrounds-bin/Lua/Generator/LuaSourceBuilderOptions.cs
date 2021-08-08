﻿using System;
using System.Globalization;

namespace Battlegrounds.Lua.Generator {

    /// <summary>
    /// Class representing options data for a <see cref="LuaSourceBuilder"/>. This class cannot be inherited.
    /// </summary>
    public sealed class LuaSourceBuilderOptions {

        /// <summary>
        /// Get the default lua format provider
        /// </summary>
        public IFormatProvider FormatProvider { get; }

        /// <summary>
        /// Get or set if the source builder should write a semicolon when syntax allows. (Defualt: <see langword="true"/>).
        /// </summary>
        public bool WriteSemicolon { get; set; }

        /// <summary>
        /// Get or set if the source builder should write a trailining comma when building tables. (Defualt: <see langword="false"/>).
        /// </summary>
        public bool WriteTrailingComma { get; set; }

        /// <summary>
        /// Get or set the max amount of characters allowed in a table for it to be written in a single line. (Defualt: 64).
        /// </summary>
        public int SingleLineTableLength { get; set; }

        /// <summary>
        /// Get or set the string symbolising a line break. (Default: <see cref="Environment.NewLine"/>).
        /// </summary>
        public string NewLine { get; set; }

        /// <summary>
        /// Get or set if null values should be explicitly saved as nil values in tables. (Default: <see langword="false"/>).
        /// </summary>
        public bool ExplicitNullAsNilValues { get; set; }

        /// <summary>
        /// Initialsie a new default <see cref="LuaSourceBuilderOptions"/> instance.
        /// </summary>
        public LuaSourceBuilderOptions() {
            this.WriteSemicolon = true;
            this.WriteTrailingComma = false;
            this.SingleLineTableLength = 64;
            this.NewLine = Environment.NewLine;
            this.ExplicitNullAsNilValues = false;
            this.FormatProvider = CultureInfo.GetCultureInfo("en-US");
        }

    }

}
