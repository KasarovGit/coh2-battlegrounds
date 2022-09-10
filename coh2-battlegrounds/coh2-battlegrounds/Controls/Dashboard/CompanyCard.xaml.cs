﻿using Battlegrounds.Functional;
using Battlegrounds.Game.DataCompany;
using Battlegrounds.Game.Gameplay;

using BattlegroundsApp.Utilities.Converters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BattlegroundsApp.Controls.Dashboard;

/// <summary>
/// Interaction logic for CompanyCard.xaml
/// </summary>
public partial class CompanyCard : UserControl {

    private static readonly Dictionary<Faction, ImageSource> Flags = new() {
        [Faction.Soviet] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/ingame/soviet.png")),
        [Faction.Wehrmacht] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/ingame/german.png")),
        [Faction.America] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/ingame/aef.png")),
        [Faction.OberkommandoWest] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/ingame/west_german.png")),
        [Faction.British] = new BitmapImage(new Uri("pack://application:,,,/coh2-battlegrounds;component/Resources/ingame/british.png"))
    };

    // Ragnar thinks code duplication is a good thing, because he doesn't do refactoring ( Greetings from CoDiEx :P )
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

    public static readonly DependencyProperty CompanyProperty 
        = DependencyProperty.Register(nameof(Company), typeof(Company), typeof(CompanyCard),
                                      new FrameworkPropertyMetadata(null,
                                      (a, b) => a.Cast<CompanyCard>(x => x.Company = b.NewValue as Company)));

    public Company? Company {
        get => this.GetValue(CompanyProperty) as Company;
        set {
            this.SetValue(CompanyProperty, value);
            this.TrySetCompanyData();
        }
    }

    public CompanyCard() {
        this.InitializeComponent();
        this.TrySetCompanyData();
    }

    private void TrySetCompanyData() => this.TrySetData(Company ?? null);

    private void TrySetData(Company company) {

        // Do nothing if company is null
        if (company is null) {
            companyName.Text = "No Company Data";
            winRateValue.Text = "N/A";
            infantryKillsValue.Text = "N/A";
            vehicleKillsValue.Text = "N/A";
            killDeathRatioValue.Text = "N/A";
            return;
        }

        // Set company faction
        factionIcon.Source = Flags[company.Army];

        // Set company name
        companyName.Text = company.Name;

        // Set company type ( For real... :P  ) 
        typeIcon.Source = StringToCompanyTypeIconConverter.GetFromType(company.Type); /* Icons.GetValueOrDefault(company.Type.ToString(), Icons[string.Empty]);*/

        // Set company data
        winRateValue.Text = company.Statistics.WinRate is 0 ? "N/A" : company.Statistics.WinRate.ToString();
        infantryKillsValue.Text = "N/A"; // TODO : This data is not currently being tracked
        vehicleKillsValue.Text = "N/A"; // TODO : This data is not currently being tracked
        killDeathRatioValue.Text = "N/A"; // TODO : This data is not currently being tracked


    }

}
