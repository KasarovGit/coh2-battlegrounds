﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace BattlegroundsApp.Utilities.Converters {
    public class CaseConverter : IValueConverter {

        public CharacterCasing Case { get; set; }
        public CaseConverter() {
            Case = CharacterCasing.Upper;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            var str = value as string;
            if (str != null) {
                switch (Case) {
                    case CharacterCasing.Lower:
                        return str.ToLower();
                    case CharacterCasing.Normal:
                        return str;
                    case CharacterCasing.Upper:
                        return str.ToUpper();
                    default:
                        return str;
                }
            }

            return string.Empty;

        }
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }
}
