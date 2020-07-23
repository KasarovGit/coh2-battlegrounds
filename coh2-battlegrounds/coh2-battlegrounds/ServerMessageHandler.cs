﻿using Battlegrounds;
using Battlegrounds.Game;
using Battlegrounds.Game.Battlegrounds;
using Battlegrounds.Game.Database;
using Battlegrounds.Online.Services;
using coh2_battlegrounds;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace BattlegroundsApp {

    internal static class ServerMessageHandler {

        private static ManagedLobby __LobbyInstance;

        public static ManagedLobby CurrentLobby => __LobbyInstance;

        private static void OnPlayerEvent(ManagedLobbyPlayerEventType type, string from, string message) {
            var mainWindow = MainWindow.Instance;
            Trace.WriteLine(type + ": " + from + " " + message);
            mainWindow.Dispatcher.Invoke(() => {
                switch (type) {
                    case ManagedLobbyPlayerEventType.Join: {
                            string joinMessage = $"[Lobby] {from} has joined.\n";
                            mainWindow.chatBox.Text = mainWindow.chatBox.Text + joinMessage;

                            mainWindow.AddPlayer(from);

                            break;
                        }
                    case ManagedLobbyPlayerEventType.Leave: {
                            string leaveMessage = $"[Lobby] {from} has left.\n";
                            mainWindow.chatBox.Text = mainWindow.chatBox.Text + leaveMessage;

                            mainWindow.RemovePlayer(from);

                            break;
                        }
                    case ManagedLobbyPlayerEventType.Kicked: {
                            string kickMessage = $"[Lobby] {from} has been kicked.\n";
                            mainWindow.chatBox.Text = mainWindow.chatBox.Text + kickMessage;

                            mainWindow.RemovePlayer(from);

                            break;
                        }
                    case ManagedLobbyPlayerEventType.Message: {
                            string messageMessage = $"{from}: {message}\n";
                            mainWindow.chatBox.Text = mainWindow.chatBox.Text + messageMessage;

                            break;
                        }
                    case ManagedLobbyPlayerEventType.Meta: {
                            string metaMessage = $"{from}: {message}";
                            Console.WriteLine(metaMessage);
                            break;
                        }
                    default: {
                            Console.WriteLine("Something went wrong.");
                            break;
                        }
                }
            });
        }

        public static object OnLocalDataRequest(string type) {

            if (type.CompareTo("CompanyData") == 0) {
                return Company.ReadCompanyFromFile("test_company.json");
            } else if (type.CompareTo("MatchInfo") == 0) {
                return new SessionInfo() { // should probably be redirected to Mainwindow and let it set up this (when considering players and settings)
                    SelectedGamemode = WinconditionList.GetWinconditionByName("Victory Point"),
                    SelectedGamemodeOption = 1,
                    SelectedScenario = ScenarioList.FromFilename("2p_angoville_farms"),
                    SelectedTuningMod = new BattlegroundsTuning(),
                    Allies = new SessionParticipant[] { new SessionParticipant("CoDiEx", null, 0, 0) }, // We'll have to solve this later
                    Axis = new SessionParticipant[] { new SessionParticipant("Ragnar", null, 0, 0) },
                    FillAI = false,
                    DefaultDifficulty = AIDifficulty.AI_Hard,
                };
            } else {
                return null;
            }

        }

        public static void OnDataRequest(bool isFileRequest, string asker, string requestedData, int id) {
            if (requestedData.CompareTo("CompanyData") == 0) {
                __LobbyInstance.SendFile(asker, "test_company.json", id);
            }
        }

        public static void StartMatchCommandReceived() {

            // Statrt the game
            if (!CoH2Launcher.Launch()) {
                Trace.WriteLine("Failed to launch Company of Heroes 2...");
            }

        }

        public static void OnFileReceived(string sender, string filename, bool received, byte[] content, int id) {

            // Did we receive the battlegrounds .sga
            if (received && filename.CompareTo("coh2_battlegrounds_wincondition.sga") == 0) {

                // Path to the sga file we'll write to
                string sgapath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\my games\\Company of Heroes 2\\mods\\gamemode\\coh2_battlegrounds_wincondition.sga";

                // Delete file if it already exists
                if (File.Exists(sgapath)) {
                    File.Delete(sgapath);
                }

                // Write all byte content
                File.WriteAllBytes(sgapath, content);

                MainWindow.Instance.chatBox.Text += "[Lobby] Received wincondition from host.";

                // Write a log message
                Trace.WriteLine("Received and saved .sga");

            } else {
                // TODO: Handle other cases
            }

        }

        public static void OnServerResponse(ManagedLobbyStatus status, ManagedLobby result) {
            if (status.Success) {

                __LobbyInstance = result;

                __LobbyInstance.OnPlayerEvent += OnPlayerEvent;
                __LobbyInstance.OnLocalDataRequested += OnLocalDataRequest;
                __LobbyInstance.OnDataRequest += OnDataRequest;
                __LobbyInstance.OnStartMatchReceived += StartMatchCommandReceived;
                __LobbyInstance.OnFileReceived += OnFileReceived;

                MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.OnLobbyEnter(__LobbyInstance));

                Trace.WriteLine("Server responded with OK");

            } else {
                Trace.WriteLine(status.Message);
            }
        }

        public static void LeaveLobby() {
            if (__LobbyInstance != null) {
                __LobbyInstance.Leave(); // Async... will need a callback for this when done.
                //__LobbyInstance = null;
            }
        }

    }

}
