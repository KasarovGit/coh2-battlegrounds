﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Battlegrounds;
using Battlegrounds.Game.DataCompany;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Verification;

namespace BattlegroundsApp.LocalData {

    public static class PlayerCompanies {

        private static List<Company> __companies;

        public static void LoadAll() {

            try {

                string companyFolder = BattlegroundsInstance.GetRelativePath(BattlegroundsPaths.COMPANY_FOLDER, string.Empty);
                string[] companies = Directory.GetFiles(companyFolder, "*.json");

                if (__companies == null) {
                    __companies = new List<Company>();
                } else {
                    if (__companies.Count == companies.Length) {
                        return;
                    }
                    __companies.Clear();
                }

                foreach (string companypath in companies) {
                    try {
                        Company company = CompanySerializer.GetCompanyFromJson(File.ReadAllText(companypath));
                        if (company?.Name?.Replace(" ", "_")?.CompareTo(Path.GetFileNameWithoutExtension(companypath)) == 0) {
                            __companies.Add(company);
                        } else {
                            Trace.WriteLine($"Failed to verify company \"{companypath}\" (Name Mismatch)", nameof(PlayerCompanies));
                        }
                    } catch (ChecksumViolationException checksumViolation) {
                        Trace.WriteLine($"Failed to verify company \"{companypath}\" ({checksumViolation.Message})", nameof(PlayerCompanies));
                    }
                }

                Trace.WriteLine($"Loaded {__companies.Count} user companies", nameof(PlayerCompanies));

            } catch (Exception ex) {
                Trace.WriteLine(ex, nameof(PlayerCompanies));
            }

        }

        public static List<Company> FindAll(Predicate<Company> predicate) {
            List<Company> matches = new List<Company>();
            foreach (Company c in __companies) {
                if (predicate(c)) {
                    matches.Add(c);
                }
            }
            return matches;
        }

        public static Company FromNameAndFaction(string name, Faction faction) => FindAll(x => x.Name.CompareTo(name) == 0 && x.Army == faction).FirstOrDefault();

        /// <summary>
        /// Save the <see cref="Company"/> instance safely in the users local company storage folder.
        /// </summary>
        /// <remarks>
        /// This will override companies with the same name!
        /// </remarks>
        /// <param name="company">The <see cref="Company"/> instance to save.</param>
        public static void SaveCompany(Company company) {
            string filename = BattlegroundsInstance.GetRelativePath(BattlegroundsPaths.COMPANY_FOLDER, $"{company.Name.Replace(" ", "_")}.json");
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
            File.WriteAllText(filename, CompanySerializer.GetCompanyAsJson(company));
        }

        public static void DeleteCompany(Company company) {
            string filename = BattlegroundsInstance.GetRelativePath(BattlegroundsPaths.COMPANY_FOLDER, $"{company.Name.Replace(" ", "_")}.json");
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
        }

        public static List<Company> GetAllCompanyes() => __companies;

    }

}
