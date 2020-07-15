﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Battlegrounds.Functional {
    
    /// <summary>
    /// 
    /// </summary>
    public static class FunctionalArray {
    
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T[] ForEach<T>(this T[] array, Func<T, T> func) {
            T[] t = new T[array.Length];
            for (int i = 0; i < array.Length; i++) {
                t[i] = func(array[i]);
            }
            return t;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="act"></param>
        /// <returns></returns>
        public static T[] ForEach<T>(this T[]array, Action<T> act) {
            for (int i = 0; i < array.Length; i++) {
                act(array[i]);
            }
            return array;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="array"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static V[] Convert<U, V>(this U[] array, Func<U, V> converter) {
            V[] result = new V[array.Length];
            for (int i = 0; i < array.Length; i++) {
                result[i] = converter(array[i]);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="elem"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        public static bool ContainsWithout<T>(this T[] array, T elem, params T[] except) 
            => array.Contains(elem) && !array.Any(x => except.Contains(x));

        public static int IndexOf<T>(this T[] array, Predicate<T> predicate) {
            for (int i = 0; i < array.Length; i++) {
                if (predicate(array[i])) {
                    return i;
                }
            }
            return -1;
        }

    }

}
