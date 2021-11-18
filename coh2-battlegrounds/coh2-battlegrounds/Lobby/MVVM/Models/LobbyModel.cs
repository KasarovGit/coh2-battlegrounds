﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Battlegrounds;
using Battlegrounds.Functional;
using Battlegrounds.Game.Database;
using Battlegrounds.Modding;
using Battlegrounds.Networking;
using Battlegrounds.Networking.LobbySystem;

using BattlegroundsApp.Lobby.MatchHandling;
using BattlegroundsApp.LocalData;
using BattlegroundsApp.Modals;
using BattlegroundsApp.MVVM;
using BattlegroundsApp.MVVM.Models;
using BattlegroundsApp.Resources;
using BattlegroundsApp.Utilities;

namespace BattlegroundsApp.Lobby.MVVM.Models {

    public class LobbyModel : IViewModel, INotifyPropertyChanged {

        private static readonly ImageSource __mapNotFound = new BitmapImage(new Uri("pack://application:,,,/Resources/ingame/unknown_map.png"));

        private readonly LobbyHandler m_handler;
        private ModPackage m_package;
        private bool m_hasSetDefaults;

        public event PropertyChangedEventHandler PropertyChanged;

        public LobbyButtonModel EditCompany { get; }

        public LobbyButtonModel ExitLobby { get; }

        public LobbyButtonModel StartMatch { get; }

        public ImageSource SelectedMatchScenario { get; set; }

        public LobbyDropdownModel<LobbyScenarioItem> ScenarioSelection { get; }

        public LobbyDropdownModel<LobbyGamemodeItem> GamemodeSelection { get; }

        public LobbyDropdownModel<LobbyGamemodeOptionItem> GamemodeOptionSelection { get; }

        public LobbyDropdownModel<LobbyBinaryOptionItem> WeatherSelection { get; }

        public LobbyDropdownModel<LobbyBinaryOptionItem> SupplySystemSelection { get; }

        public LobbyDropdownModel<LobbyModPackageItem> ModPackageSelection { get; }

        public LobbyTeam Allies { get; }

        public LobbyTeam Axis { get; }

        public ObservableCollection<LobbyCompanyItem> AlliedCompanies { get; }

        public ObservableCollection<LobbyCompanyItem> AxisCompanies { get; }

        public bool SingleInstanceOnly => false;

        private LobbyModel(LobbyHandler handler, ILobbyTeam allies, ILobbyTeam axis) {

            // Set handler
            this.m_handler = handler;

            // Init company lists
            InitCompanyList(this.AlliedCompanies = new(), isAllied: true);
            InitCompanyList(this.AxisCompanies = new(), isAllied: false);

            // Create edit company button
            this.EditCompany = new() {
                Click = new RelayCommand(this.EditSelfCompany),
                Enabled = false,
                Visible = Visibility.Visible,
                Text = new("LobbyView_EditCompany")
            };

            // Create exit lobby button
            this.ExitLobby = new() {
                Click = new RelayCommand(this.LeaveLobby),
                Enabled = false,
                Visible = Visibility.Visible,
                Text = new("LobbyView_LeaveLobby")
            };

            // Create start match button
            this.StartMatch = new() {
                Click = new RelayCommand(this.BeginMatchSetup),
                Enabled = false,
                Visible = Visibility.Visible,
                Text = new("LobbyView_StartMatch")
            };

            // Create package list
            List<LobbyModPackageItem> modPackages = new();
            ModManager.EachPackage(x => modPackages.Add(new(x)));

            // Create mod package dropdown
            this.ModPackageSelection = new(true, this.m_handler.IsHost) {
                Items = new(modPackages),
                OnSelectionChanged = this.OnPackageChanged
            };

            // Set package here
            if (this.m_handler.IsHost) {
                this.m_package = this.ModPackageSelection.Items[0].Package;
            }

            // Create scenario selection dropdown
            this.ScenarioSelection = new(true, this.m_handler.IsHost) {
                Items = new(ScenarioList.GetList()
                    .Where(x => x.IsVisibleInLobby)
                    .Select(x => new LobbyScenarioItem(x))),
                OnSelectionChanged = this.OnScenarioChanged
            };

            // Create gamemode selection dropdown
            this.GamemodeSelection = new(true, this.m_handler.IsHost) {
                Items = new(),
                OnSelectionChanged = OnGamemodeChanged
            };

            // Create gamemode option selection dropdown
            this.GamemodeOptionSelection = new(true, this.m_handler.IsHost) {
                Items = new(),
                OnSelectionChanged = this.OnGamemodeOptionChanged
            };

            // Create weather selection dropdown
            this.WeatherSelection = new(true, this.m_handler.IsHost) {
                Items = LobbyBinaryOptionItem.CreateCollection()
            };

            // Create supply selection dropdown
            this.SupplySystemSelection = new(true, this.m_handler.IsHost) {
                Items = LobbyBinaryOptionItem.CreateCollection()
            };

            // Init dropdown values (if host)
            if (handler.IsHost) {
                this.ScenarioSelection.SetSelection(x => x.Scenario.RelativeFilename == BattlegroundsInstance.LastPlayedMap);
            }

            // Create teams
            this.Allies = new(allies, handler.Lobby) { AvailableCompanies = this.AlliedCompanies };
            this.Axis = new(axis, handler.Lobby) { AvailableCompanies = this.AxisCompanies };

        }

        private static void InitCompanyList(ObservableCollection<LobbyCompanyItem> container, bool isAllied) {
            var companies = PlayerCompanies.FindAll(x => x.Army.IsAllied == isAllied);
            if (companies.Count > 0) {
                companies.ForEach(x => container.Add(new(x)));
            } else {
                container.Add(new(0));
            }
        }

        private void EditSelfCompany() {
            Trace.WriteLine("Editing own company in lobby is currently not implemented!");
        }

        private void LeaveLobby() {

            // Show leave modal
            App.ViewManager.GetModalControl().ShowModal(ModalDialog.CreateModal("Leave Lobby", "Are you sure you'd like to leave?", (sender, success, value) => {
                if (success && value == ModalDialogResult.Confirm) {

                    // Leave lobby
                    Task.Run(this.m_handler.BrokerHandler.Disconnect);

                    // Go back to browser view
                    App.ViewManager.SetDisplay(AppDisplayState.LeftRight, typeof(LeftMenu), typeof(LobbyBrowserViewModel));

                }
            }));

        }

        private void BeginMatchSetup() {

            // If not host -> bail.
            if (!this.m_handler.IsHost) {
                return;
            }

            // Get play model
            var play = PlayModelFactory.GetModel(this.m_handler);

            // prepare
            play.Prepare(this.m_package, this.BeginMatch, this.CancelMatch);

        }

        private void BeginMatch(IPlayModel model) {

            // Play match
            model.Play(this.EndMatch);

        }

        private void EndMatch(IPlayModel model) {

        }

        private void CancelMatch(IPlayModel model) {

        }

        private int OnPackageChanged(int current, int next, LobbyModPackageItem item) {

            // Bail if item is null
            if (item is null) {
                return next;
            }

            // Set package
            this.m_package = item.Package;

            // Update lobby
            if (!this.m_handler.Lobby.SetLobbySetting("selected_tuning", item.Package.ID)) {
                Trace.WriteLine("Failed to set 'selected_tuning'", nameof(LobbyModel));
            }

            // Return selected
            return next;

        }

        private void TrySetMapSource(Scenario scenario) {

            // Set to default case
            this.SelectedMatchScenario = __mapNotFound;

            // Get Path
            string fullpath = Path.GetFullPath($"bin\\gfx\\map_icons\\{scenario.RelativeFilename}_map.tga");

            // Check if file exists
            if (File.Exists(fullpath)) {
                try {
                    this.SelectedMatchScenario = TgaImageSource.TargaBitmapSourceFromFile(fullpath);
                } catch (BadImageFormatException bife) {
                    Trace.WriteLine(bife, nameof(this.TrySetMapSource));
                }
            } else {
                fullpath = Path.GetFullPath($"usr\\mods\\map_icons\\{scenario.RelativeFilename}_map.tga");
                if (File.Exists(fullpath)) {
                    try {
                        this.SelectedMatchScenario = TgaImageSource.TargaBitmapSourceFromFile(fullpath);
                    } catch (BadImageFormatException bife) {
                        Trace.WriteLine(bife, nameof(this.TrySetMapSource));
                    }
                } else {
                    Trace.WriteLine($"Failed to locate file: {fullpath}", nameof(this.TrySetMapSource));
                }
            }

            // Inform view the map selection was changed
            this.PropertyChanged?.Invoke(this, new(nameof(this.SelectedMatchScenario)));

        }

        private int OnScenarioChanged(int current, int next, LobbyScenarioItem item) {

            // If item is not null
            if (item is not null) {

                // Update team capacity and go back to previous selection if capacity change fails
                if (!this.m_handler.Lobby.SetTeamsCapacity(item.Scenario.MaxPlayers / 2)) {
                    return current;
                }

                // Update map
                _ = Application.Current.Dispatcher.BeginInvoke(() => this.TrySetMapSource(item.Scenario));

                // Update gamemode (and option)
                this.UpdateGamemodeAndOption(item.Scenario);

                // Update lobby
                if (!this.m_handler.Lobby.SetLobbySetting("selected_map", item.Scenario.RelativeFilename)) {
                    Trace.WriteLine("Failed to set 'selected_map'", nameof(LobbyModel));
                }

            }

            // Return selected value
            return next;

        }

        private void UpdateGamemodeAndOption(Scenario scenario) {

            // Get available gamemodes
            var guid = this.m_package.GamemodeGUID;
            List<LobbyGamemodeItem> available = (scenario.Gamemodes.Count > 0 ? WinconditionList.GetGamemodes(guid, scenario.Gamemodes) : WinconditionList.GetGamemodes(guid))
                .Select(x => new LobbyGamemodeItem(x)).ToList();

            // Update if there's any change in available gamemodes 
            if (this.GamemodeSelection.Items.Count != available.Count || available.Any(x => !this.GamemodeSelection.Items.Contains(x))) {

                // Clear current gamemode selection
                this.GamemodeSelection.Items.Clear();
                available.ForEach(x => this.GamemodeSelection.Items.Add(x));

                // Set default if not set
                if (!this.m_hasSetDefaults) {
                    this.GamemodeSelection.SetSelection(x => x.Gamemode.Name == BattlegroundsInstance.LastPlayedGamemode);
                } else {
                    this.GamemodeSelection.SetSelection(_ => true);
                }

            }

        }

        private int OnGamemodeChanged(int current, int next, LobbyGamemodeItem item) {

            // Get options
            var options = item.Gamemode.Options;

            // Clear available options
            this.GamemodeOptionSelection.Items.Clear();

            // Hide options
            if (options is null || options.Length is 0) {

                // Set options to hidden
                this.GamemodeOptionSelection.IsVisible = false;

            } else {

                // Update options
                _ = options.ForEach(x => this.GamemodeOptionSelection.Items.Add(new(x)));

                // Set default
                if (!this.m_hasSetDefaults) {
                    this.GamemodeOptionSelection.SetSelection(x => x.Option.Value == BattlegroundsInstance.LastPlayedGamemodeSetting);
                    this.m_hasSetDefaults = true;
                } else {
                    var defaultOption = item.Gamemode.Options[item.Gamemode.DefaultOptionIndex];
                    this.GamemodeOptionSelection.SetSelection(x => x.Option == defaultOption);
                }

                // Set options to hidden
                this.GamemodeOptionSelection.IsVisible = true;

            }

            // Update lobby
            if (!this.m_handler.Lobby.SetLobbySetting("selected_wc", item.Gamemode.Name)) {
                Trace.WriteLine("Failed to set 'selected_wc'", nameof(LobbyModel));
            }

            // Return selected
            return next;

        }

        private int OnGamemodeOptionChanged(int current, int next, LobbyGamemodeOptionItem item) {

            // Update lobby
            if (!this.m_handler.Lobby.SetLobbySetting("selected_wco", item.Option.Value.ToString(CultureInfo.InvariantCulture))) {
                Trace.WriteLine("Failed to set 'selected_wco'", nameof(LobbyModel));
            }

            // Return selected
            return next;

        }

        private void OnHostedLobbyValueChanged(ILobby sender, ObservableValueChangedEventArgs eventArgs) { // Invoked when the hosted lobby values changed
            switch (eventArgs.Property) {
                case nameof(ILobby.Capacity):
                    break;
                default:
                    break;
            }
        }

        private void OnRemoteLobbyValueChanged(ILobby sender, ObservableValueChangedEventArgs eventArgs) { // Invoked when the remote lobby values changed
            switch (eventArgs.Property) {
                case nameof(ILobby.Capacity):
                    break;
                default:
                    break;
            }
        }

        public static LobbyModel CreateModelAsHost(LobbyHandler handler) {

            // Create model
            LobbyModel model = new(handler, handler.Lobby.Allies, handler.Lobby.Axis);
            handler.Lobby.ValueChanged += model.OnHostedLobbyValueChanged;

            // Return model
            return model;

        }

        public static LobbyModel CreateModelAsParticipant(LobbyHandler handler) {

            // Create model
            LobbyModel model = new(handler, null, null);
            handler.Lobby.ValueChanged += model.OnRemoteLobbyValueChanged;

            // Return model
            return model;

        }

    }

}
