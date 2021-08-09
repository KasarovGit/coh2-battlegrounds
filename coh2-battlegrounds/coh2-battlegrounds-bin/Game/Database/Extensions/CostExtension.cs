﻿using System.Text.Json;

using Battlegrounds.Functional;
using Battlegrounds.Lua.Generator.RuntimeServices;

namespace Battlegrounds.Game.Database.Extensions {

    /// <summary>
    /// Class representing the cost extension attribute that is common among some <see cref="Blueprint"/> implementations.
    /// </summary>
    public class CostExtension {

        /// <summary>
        /// Manpower cost of the <see cref="Cost"/> data.
        /// </summary>
        [LuaName("manpower")]
        public float Manpower { get; set; }

        /// <summary>
        /// Munitions cost of the <see cref="Cost"/> data.
        /// </summary>
        [LuaName("munitions")]
        public float Munitions { get; set; }

        /// <summary>
        /// Fuel cost of the <see cref="Cost"/> data.
        /// </summary>
        [LuaName("fuel")]
        public float Fuel { get; set; }

        /// <summary>
        /// The amount of time it takes to field something.
        /// </summary>
        [LuaName("fieldtime")]
        public float FieldTime { get; set; }

        public CostExtension() : this(0.0f, 0.0f, 0.0f) { }

        public CostExtension(CostExtension costExtension) {
            this.Munitions = costExtension.Munitions;
            this.Manpower = costExtension.Manpower;
            this.Fuel = costExtension.Fuel;
            this.FieldTime = costExtension.FieldTime;
        }

        public CostExtension(float manpower, float munitions, float fuel, float fieldTime = 0.0f) {
            this.Manpower = manpower;
            this.Munitions = munitions;
            this.Fuel = fuel;
            this.FieldTime = fieldTime;
        }

        public static CostExtension operator +(CostExtension left, CostExtension right)
            => new(left.Manpower + right.Manpower, left.Munitions + right.Munitions, left.Fuel + right.Fuel, left.FieldTime + right.FieldTime);

        public static CostExtension operator *(CostExtension left, CostExtension right)
            => new(left.Manpower * right.Manpower, left.Munitions * right.Munitions, left.Fuel * right.Fuel, left.FieldTime * right.FieldTime);

        public static CostExtension operator *(CostExtension left, float right)
            => new(left.Manpower * right, left.Munitions * right, left.Fuel * right, left.FieldTime * right);

        public static CostExtension operator *(float left, CostExtension right)
            => right * left;

        public static CostExtension FromJson(ref Utf8JsonReader reader) {
            float[] values = new float[4];
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject) {
                string prop = reader.ReadProperty();
                values[prop switch {
                    "Manpower" => 0,
                    "Munition" => 1,
                    "Fuel" => 2,
                    "FieldTime" => 3,
                    _ => throw new System.Exception()
                }] = reader.GetSingle();
            }
            return new(values[0], values[1], values[2], values[3]);
        }

    }

}
