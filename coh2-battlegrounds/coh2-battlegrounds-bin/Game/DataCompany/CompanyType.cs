﻿using System;
using System.Collections.Generic;
using Battlegrounds.Functional;
using System.Linq;

using Battlegrounds.Lua.Generator.RuntimeServices;

namespace Battlegrounds.Game.DataCompany {

    /// <summary>
    /// Enumerated representation of recognized types that can be used to describe a <see cref="Company"/>.
    /// </summary>
    [LuaEnumBehaviour(true)]
    public enum CompanyType {

        /// <summary>
        /// No specific type is specified
        /// </summary>
        Unspecified,

        /// <summary>
        /// Heavy focus on infantry
        /// </summary>
        Infantry,

        /// <summary>
        /// Infantry and team weapons use motorized transport (Opel Blitz, Zis-6 etc.)
        /// </summary>
        Motorized,

        /// <summary>
        /// Infantry and team weapons use mechanized transport (Halftracks) and light tanks.
        /// </summary>
        Mechanized,

        /// <summary>
        /// Favours the use of heavy and medium tanks.
        /// </summary>
        Armoured,

        /// <summary>
        /// Favours anti-tank units
        /// </summary>
        TankDestroyer,

        /// <summary>
        /// Favours the use of aircraft
        /// </summary>
        Airborne,

        /// <summary>
        /// Favours the use of combat engineers - frontline fortifications
        /// </summary>
        Engineer,

        /// <summary>
        /// Favours the use of artillery
        /// </summary>
        Artillery,

    }

    /// <summary>
    /// 
    /// </summary>
    public enum CompanyAvailabilityType {

        /// <summary>
        /// 
        /// </summary>
        MultiplayerOnly,

        /// <summary>
        /// 
        /// </summary>
        CampaignOnly,

        /// <summary>
        /// 
        /// </summary>
        AnyMode,

    }

    /// <summary>
    /// 
    /// </summary>
    public static class CompanyTypeExtension {

        public static List<CompanyType> CompanyTypes => Enum.GetValues<CompanyType>().Except(CompanyType.Unspecified).ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="companyType"></param>
        /// <returns></returns>
        public static int GetMaxInfantry(this CompanyType companyType) => companyType switch {
            CompanyType.Infantry => 20,
            CompanyType.Motorized => 15,
            CompanyType.Mechanized => 15,
            //CompanyType.Armoured => 10,
            //CompanyType.TankDestroyer => 10,
            CompanyType.Airborne => 14,
            CompanyType.Engineer => 12,
            //CompanyType.Artillery => 10,
            _ => 16
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="companyType"></param>
        /// <returns></returns>
        public static int GetMaxSupportWeapons(this CompanyType companyType) => companyType switch {
            CompanyType.Infantry => 14,
            //CompanyType.Motorized => 12,
            //CompanyType.Mechanized => 12,
            CompanyType.Armoured => 10,
            CompanyType.TankDestroyer => 10,
            //CompanyType.Airborne => 12,
            //CompanyType.Engineer => 12,
            CompanyType.Artillery => 18,
            _ => 10
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="companyType"></param>
        /// <returns></returns>
        public static int GetMaxVehicles(this CompanyType companyType) => companyType switch {
            CompanyType.Infantry => 6,
            //CompanyType.Motorized => 10,
            //CompanyType.Mechanized => 10,
            CompanyType.Armoured => 15,
            CompanyType.TankDestroyer => 12,
            //CompanyType.Airborne => 10,
            //CompanyType.Engineer => 10,
            //CompanyType.Artillery => 10,
            _ => 14
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="companyType"></param>
        /// <returns></returns>
        public static int GetMaxAbilities(this CompanyType companyType) => companyType switch {
            CompanyType.Airborne or CompanyType.Artillery => 6,
            _ => 5
        };

    }

}
