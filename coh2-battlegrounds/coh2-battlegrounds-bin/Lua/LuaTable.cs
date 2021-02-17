﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Battlegrounds.Lua {

    /// <summary>
    /// Delegate for handing a key-value paired iteration.
    /// </summary>
    /// <param name="key">The key value used to index the value in table.</param>
    /// <param name="value">The value found at key position.</param>
    /// <returns>A <see cref="LuaNil"/> instance if iteration should continue. Otherwise a single return <see cref="LuaValue"/>.</returns>
    public delegate LuaValue Pairs(LuaValue key, LuaValue value);

    /// <summary>
    /// Representation of a Lua table. Extends <see cref="LuaValue"/> and can be enumerated over.
    /// </summary>
    public sealed class LuaTable : LuaValue, IEnumerable<KeyValuePair<LuaValue, LuaValue>> {

        private Dictionary<LuaValue, LuaValue> m_table;

        /// <summary>
        /// Get the size of the table.
        /// </summary>
        public int Size => this.m_table.Count;

        /// <summary>
        /// Initialize a new <see cref="LuaTable"/> class without entries.
        /// </summary>
        public LuaTable() {
            this.m_table = new Dictionary<LuaValue, LuaValue>();
        }

        public override bool Equals(LuaValue value) {
            if (value is LuaTable table) {
                return this.m_table.All(x => table.Contains(x));
            }
            return false;
        }

        public override bool Equals(object obj) => obj is LuaValue v ? this.Equals(v) : base.Equals(obj);

        public LuaValue this[LuaValue key] {
            get => this.m_table.ContainsKey(key) ? this.m_table[key] : new LuaNil();
            set => this.m_table[key] = value;
        }

        public LuaValue this[string key] {
            get => this[new LuaString(key)];
            set => this[new LuaString(key)] = value;
        }

        /// <summary>
        /// Retrieve an element by <see cref="LuaValue"/> key.
        /// </summary>
        /// <typeparam name="T">Expected <see cref="LuaValue"/> type.</typeparam>
        /// <param name="key">The <see cref="LuaValue"/> key to use when getting value.</param>
        /// <returns>The <see cref="LuaValue"/> identified by <paramref name="key"/>. Otherwise <see langword="default"/>.</returns>
        public T ByKey<T>(LuaValue key) where T : LuaValue {
            if (this[key] is T v)
                return v;
            else
                return default;
        }

        /// <summary>
        /// Retrieve an element by <see cref="string"/> key (Implict <see cref="LuaString"/>).
        /// </summary>
        /// <typeparam name="T">Expected <see cref="LuaValue"/> type.</typeparam>
        /// <param name="key">The <see cref="LuaValue"/> key to use when getting value.</param>
        /// <returns>The <see cref="LuaValue"/> identified by <paramref name="key"/>. Otherwise <see langword="default"/>.</returns>
        public T ByKey<T>(string key) where T : LuaValue {
            if (this[new LuaString(key)] is T v)
                return v;
            else
                return default;
        }

        /// <summary>
        /// Do a pairs run on  the table.
        /// </summary>
        /// <param name="pairs">The delegate to run on each pair iteration.</param>
        /// <returns><see cref="LuaNil"/> if pairs iterator reached the end of the table. Otherwise returned <see cref="LuaValue"/>.</returns>
        public LuaValue Pairs(Pairs pairs) {
            var e = this.GetEnumerator();
            var nil = new LuaNil();
            while (e.MoveNext()) {
                var s = pairs(e.Current.Key, e.Current.Value);
                if (!s.Equals(nil)) {
                    return s;
                }
            }
            return nil;
        }

        public override string Str() => this.GetHashCode().ToString();

        public IEnumerator<KeyValuePair<LuaValue, LuaValue>> GetEnumerator() => ((IEnumerable<KeyValuePair<LuaValue, LuaValue>>)this.m_table).GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.m_table).GetEnumerator();

        public override int GetHashCode() => this.m_table.GetHashCode();
    
    }

}
