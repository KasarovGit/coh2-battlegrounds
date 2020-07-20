﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using Battlegrounds;
using Battlegrounds.Compiler;
using Battlegrounds.Game.Battlegrounds;
using Battlegrounds.Game.Database;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Online;
using Battlegrounds.Online.Services;
using Battlegrounds.Steam;

namespace coh2_battlegrounds_console {
    
    class Program {
        
        static void Main(string[] args) {

            BattlegroundsInstance.LoadInstance();
            BattlegroundsInstance.LocalSteamuser = SteamUser.FromLocalInstall();

            // Important this is done
            DatabaseManager.LoadAllDatabases(null);

            while (!DatabaseManager.DatabaseLoaded) {
                Thread.Sleep(1);
            }

            // Create a dummy company
            CompanyBuilder companyBuilder = new CompanyBuilder().NewCompany(Faction.Soviet)
                .ChangeName("26th Rifle Division")
                .ChangeUser(BattlegroundsInstance.LocalSteamuser.Name)
                .ChangeTuningMod(BattlegroundsInstance.BattleGroundsTuningMod.Guid.ToString());
            UnitBuilder unitBuilder = new UnitBuilder();

            // Basic infantry
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg").SetDeploymentPhase(DeploymentPhase.PhaseInitial).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg").SetDeploymentPhase(DeploymentPhase.PhaseInitial).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg").SetVeterancyRank(2).AddUpgrade("dp-28_lmg_upgrade_bg").SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg").SetVeterancyRank(2).AddUpgrade("dp-28_lmg_upgrade_bg").SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg").SetVeterancyRank(4).AddUpgrade("ppsh-41_sub_machine_gun_upgrade_bg").SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("tank_buster_bg").SetVeterancyRank(1).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("tank_buster_bg").SetVeterancyRank(2).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("shock_troops_bg").SetVeterancyRank(4).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("shock_troops_bg").SetVeterancyRank(3).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("shock_troops_bg").SetVeterancyRank(5).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("nkvd_squad_bg").SetVeterancyRank(2).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("nkvd_squad_bg").SetVeterancyRank(2).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("commissar_squad_bg").SetVeterancyRank(3).SetDeploymentPhase(DeploymentPhase.PhaseA).GetAndReset());

            // Transported Infantry
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(3)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(4)
                .AddUpgrade("ppsh-41_sub_machine_gun_upgrade_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("conscript_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(5)
                .AddUpgrade("ppsh-41_sub_machine_gun_upgrade_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("frontoviki_squad_bg")
                .SetTransportBlueprint("m5_halftrack_squad_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(0)
                .AddUpgrade("ppsh-41_sub_machine_gun_upgrade_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseB)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("frontoviki_squad_bg")
                .SetTransportBlueprint("m5_halftrack_squad_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(1)
                .AddUpgrade("ppsh-41_sub_machine_gun_upgrade_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseB)
                .GetAndReset());

            // Support Weapons
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1942_zis-3_76mm_at_gun_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1942_zis-3_76mm_at_gun_squad_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1910_maxim_heavy_machine_gun_squad_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1910_maxim_heavy_machine_gun_squad_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("pm-82_41_mortar_squad_bg")
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());

            // Vehicles
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("t_34_76_squad_bg")
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("t_34_76_squad_bg")
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseA)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("t_34_76_squad_bg")
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseB)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("t_34_85_squad_bg")
                .SetVeterancyRank(4)
                .SetDeploymentPhase(DeploymentPhase.PhaseB)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("t_34_85_squad_bg")
                .SetVeterancyRank(5)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("kv-1_bg")
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("kv-1_bg")
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("kv-1_bg")
                .SetVeterancyRank(3)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());

            // Artillery
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1931_203mm_b-4_howitzer_artillery_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndStay)
                .SetVeterancyRank(1)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());
            companyBuilder.AddUnit(unitBuilder.SetBlueprint("m1931_203mm_b-4_howitzer_artillery_bg")
                .SetTransportBlueprint("zis_6_transport_truck_bg")
                .SetDeploymentMethod(DeploymentMethod.DeployAndExit)
                .SetVeterancyRank(2)
                .SetDeploymentPhase(DeploymentPhase.PhaseC)
                .GetAndReset());

            // Commit changes
            companyBuilder.Commit();

            Company testCompany = companyBuilder.Result;

            SessionInfo sessionInfo = new SessionInfo() {
                SelectedGamemode = WinconditionList.GetWinconditionByName("Victory Points"),
                SelectedGamemodeOption = 1,
                SelectedScenario = ScenarioList.FromFilename("2p_angoville_farms"),
                SelectedTuningMod = new BattlegroundsTuning(),
                Allies = new SessionParticipant[] { new SessionParticipant(BattlegroundsInstance.LocalSteamuser, testCompany, 0, 0) },
                Axis = null,
                FillAI = true,
                DefaultDifficulty = Battlegrounds.Game.AIDifficulty.AI_Hard,
            };

            Session session = Session.CreateSession(sessionInfo);

            SessionCompiler<CompanyCompiler> sessionCompiler = new SessionCompiler<CompanyCompiler>();
            File.WriteAllText("test_session.lua", sessionCompiler.CompileSession(session));

            /*GameMatch m = new GameMatch(session);
            m.LoadMatch($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\my games\\company of heroes 2\\playback\\temp.rec");
            m.EvaluateResult();
            */
            
            SessionManager.PlaySession<SessionCompiler<CompanyCompiler>, CompanyCompiler>(session, (a,b) => { Console.WriteLine(a); }, null, null);

            // Save json
            testCompany.SaveToFile("test_company.json");

            
            /*LobbyHub hub = new LobbyHub();
            if (!hub.CanConnect()) {
                Console.WriteLine("Unable to reach server hub");
            } else {

                var lobbies = hub.GetConnectableLobbies();
                if (lobbies.Count == 0) {
                    hub.User = BattlegroundsInstance.LocalSteamuser;
                    HostTest(hub);
                } else {
                    hub.User = SteamUser.FromID(76561198157626935UL);
                    ClientTest(hub, lobbies.First());
                }

            }
            
            BattlegroundsInstance.SaveInstance();
            */
            Console.ReadLine();

        }

        private static void HostTest(LobbyHub hub) {

            Console.WriteLine("Running hosting test");

            ManagedLobby.Host(hub, "Battlegrounds Test", string.Empty, OnMessageLoop);

        }

        private static void ClientTest(LobbyHub hub, ConnectableLobby lob) {

            Console.WriteLine("Running client test");

            ManagedLobby.Join(hub, lob, string.Empty, OnMessageLoop);

        }

        private static void OnMessageLoop(ManagedLobbyStatus status, ManagedLobby result) {

            static void OnCompanyFileReceived(string from, string name, bool received, byte[] content) {
                Console.WriteLine("Received company data");
                if (received) {
                    File.WriteAllBytes("hello.json", content);
                }
            }

            if (status.Success) {

                Console.WriteLine("Connection was established!");

                result.OnPlayerEvent += (a, b, c) => {
                    if (a == ManagedLobbyPlayerEventType.Message) {
                        Console.WriteLine($"{b}: {c}");
                        Console.WriteLine("Testing launch feature");
                        result.CompileAndStartMatch(x => Console.WriteLine(x));
                        //result.GetCompanyFileFrom(b, OnCompanyFileReceived);
                    } else {
                        string word = (a == ManagedLobbyPlayerEventType.Leave) ? "Left" : (a == ManagedLobbyPlayerEventType.Kicked ? "Was kicked" : "Joined");
                        Console.WriteLine($"{b} {word}");
                    }
                };

                result.OnLocalDataRequested += (a) => {
                    if (a.CompareTo("CompanyData") == 0) {
                        return Company.ReadCompanyFromFile("test_company.json");
                    } else if (a.CompareTo("Gamemode") == 0) {
                        return WinconditionList.GetWinconditionByName("Victory Points");
                    } else {
                        return null;
                    }
                };

                result.OnDataRequest += (a, b, c, d) => {
                    if (c.CompareTo("CompanyData") == 0) {
                        Console.WriteLine("Received request for company data using identifier " + d);
                        result.SendFile(b, "test_company.json", d);
                    } 
                };

                if (!result.IsHost) {
                    result.SendChatMessage("Hello World");
                }

            } else {
                Console.WriteLine(status.Message);
            }


        }

    }

}
