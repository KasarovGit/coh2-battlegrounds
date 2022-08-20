﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Battlegrounds.Modding.Content.Companies;

namespace BattlegroundsApp.Utilities.Converters; 

public class StringToCompanyTypeIconConverter : IValueConverter {

    private static readonly Dictionary<string, ImageSource> Icons = new() {
        ["ct_infantry"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_infantry.png")),
        ["ct_armoured"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_armoured.png")),
        ["ct_motorized"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_motorized.png")),
        ["ct_mechanized"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_mechanized.png")),
        ["ct_airborne"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_airborne.png")),
        ["ct_artillery"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_artillery.png")),
        ["ct_td"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_td.png")),
        ["ct_engineer"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_engineer.png")),
        ["ct_unspecified"] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_unspecified.png")),
        [string.Empty] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/app/company_types/ct_unspecified.png"))
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is FactionCompanyType t) {
            return GetFromType(t);
        } else if (value is string icoType) {
            return GetFromType(icoType);
        }
        throw new ArgumentException("Invalid converter argument.", nameof(value));
    }

    public static ImageSource GetFromType(string t) => Icons.GetValueOrDefault(t, Icons[string.Empty]);

    public static ImageSource GetFromType(FactionCompanyType t) => Icons.GetValueOrDefault(t.Icon, Icons[string.Empty]);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

}
