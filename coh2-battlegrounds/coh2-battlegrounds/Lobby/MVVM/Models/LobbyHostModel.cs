﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Battlegrounds;
using Battlegrounds.Compiler;
using Battlegrounds.Functional;
//using Battlegrounds.Functional;
using Battlegrounds.Game.Database;
using Battlegrounds.Game.DataCompany;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Game.Match.Play;
using Battlegrounds.Locale;
using Battlegrounds.Modding;
using Battlegrounds.Networking.LobbySystem;
using Battlegrounds.Networking.Server;

using BattlegroundsApp.Lobby.MatchHandling;
using BattlegroundsApp.LocalData;
using BattlegroundsApp.Modals;
using BattlegroundsApp.Modals.Dialogs.MVVM.Models;
using BattlegroundsApp.MVVM;
using BattlegroundsApp.MVVM.Models;
using BattlegroundsApp.Resources;
using BattlegroundsApp.Utilities;

namespace BattlegroundsApp.Lobby.MVVM.Models {

    public class LobbyHostModel : LobbyModel {
        
        private static readonly string __playabilityAlliesInvalid = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAlliesInvalid");
        private static readonly string __playabilityAlliesNoPlayers = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAlliesNoPlayers");
        private static readonly string __playabilityAxisInvalid = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAxisInvalid");
        private static readonly string __playabilityAxisNoPlayers = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAxisNoPlayers");
        protected static readonly Func<string> LOCSTR_START = () => BattlegroundsInstance.Localize.GetString("LobbyView_StartMatch");

        private ModPackage? m_package;

        public override LobbyMutButton StartMatchButton { get; }

        public override LobbyDropdown<ScenOp> MapDropdown { get; }

        public override LobbyDropdown<IGamemode> GamemodeDropdown { get; }

        public override LobbyDropdown<IGamemodeOption> GamemodeOptionDropdown { get; }

        public override LobbyDropdown<OnOffOption> WeatherDropdown { get; }

        public override LobbyDropdown<OnOffOption> SupplySystemDropdown { get; }

        public override LobbyDropdown<ModPackageOption> ModPackageDropdown { get; }

        public ImageSource? SelectedMatchScenario { get; set; }

        public LobbyHostModel(LobbyAPI handle, LobbyAPIStructs.LobbyTeam allies, LobbyAPIStructs.LobbyTeam axis) : base(handle, allies, axis) {

            // Init buttons
            this.StartMatchButton = new(LOCSTR_START(), new(this.BeginMatchSetup), Visibility.Visible);

            // Get scenario list
            var _scenlist = ScenarioList.GetList()
                .Where(x => x.IsVisibleInLobby)
                .OrderBy(x => x.MaxPlayers);
            var scenlist = new List<ScenOp>(_scenlist.Select(x => new ScenOp(x)));

            // Get & set tunning list
            var tunlist = new List<ModPackageOption>();
            ModManager.EachPackage(x => tunlist.Add(new ModPackageOption(x)));

            this.ModPackageDropdown = new(true, Visibility.Visible, new(tunlist), this.ModPackageSelectionChanged);

            this.m_package = this.ModPackageDropdown.Items[0].ModPackage;

            // Get On & Off collection
            ObservableCollection<OnOffOption> onOfflist = new(new[] { new OnOffOption(true), new OnOffOption(false) });

            // Init dropdowns 
            this.MapDropdown = new(true, Visibility.Visible, new(scenlist), this.MapSelectionChanged);
            this.GamemodeDropdown = new(true, Visibility.Visible, new(), this.GamemodeSelectionChanged);
            this.GamemodeOptionDropdown = new(true, Visibility.Visible, new(), this.GamemodeOptionSelectionChanged);
            this.WeatherDropdown = new(true, Visibility.Visible, onOfflist, this.WeatherSelectionChanged);
            this.SupplySystemDropdown = new(true, Visibility.Visible, onOfflist, this.SupplySystemSelectionChanged);

            // Add handlers to remote updates and notifications
            /*this.m_handle.OnLobbySelfUpdate += this.OnSelfChanged;
            this.m_handle.OnLobbyTeamUpdate += this.OnTeamChanged;
            this.m_handle.OnLobbyCompanyUpdate += this.OnCompanyChanged;
            this.m_handle.OnLobbySlotUpdate += this.OnSlotChanged;
            this.m_handle.OnLobbyConnectionLost += this.OnConnectionLost;
            this.m_handle.OnLobbyRequestCompany += this.OnCompanyRequested;
            this.m_handle.OnLobbyNotifyGamemode += this.OnGamemodeReleased;
            this.m_handle.OnLobbyNotifyResults += this.OnResultsReleased;
            this.m_handle.OnLobbyLaunchGame += this.OnLaunchGame;
            this.m_handle.OnLobbyBeginMatch += this.OnMatchBegin;
            this.m_handle.OnLobbyCancelStartup += this.OnMatchStartupCancelled;*/

            // Set dropdown index, cascade effect
            this.MapDropdown.Selected = 0;

            // Set opt-in settings
            this.WeatherDropdown.Selected = 0;
            this.SupplySystemDropdown.Selected = 0;
            this.ModPackageDropdown.Selected = 0;

        }

        public void RefreshPlayability() {

            // Skip check if not host
            if (!this.m_handle.IsHost) {
                return;
            }

            // Check allies
            var (x1, y1) = this.Allies.CanPlay();
            bool allied = x1 && y1;

            // Check axis
            var (x2, y2) = this.Axis.CanPlay();
            bool axis = x2 && y2;

            // If both playable
            if (allied && axis) {
                this.StartMatchButton.IsEnabled = true;
                this.StartMatchButton.Tooltip = null;
            } else if (!allied) {
                this.StartMatchButton.IsEnabled = false;
                this.StartMatchButton.Tooltip = x1 ? __playabilityAlliesNoPlayers : __playabilityAlliesInvalid;
            } else {
                this.StartMatchButton.IsEnabled = false;
                this.StartMatchButton.Tooltip = x2 ? __playabilityAxisNoPlayers : __playabilityAxisInvalid;
            }

        }

        private void OnSelfChanged() {
            Application.Current.Dispatcher.Invoke(() => {

            });
        }

        private void BeginMatchSetup() {

            // If not host -> bail.
            if (!this.m_handle.IsHost)
                return;

            // Bail if no chat model
            if (this.m_chatModel is null)
                return;

            // Bail if no package defined
            if (this.m_package is null)
                return; // TODO: Show error

            // Disallow exit lobby
            //this.ExitLobby.Enabled = false;

            // Set lobby status here
            this.m_handle.SetLobbyState(LobbyAPIStructs.LobbyState.Starting);

            // Get play model
            var play = PlayModelFactory.GetModel(this.m_handle, this.m_chatModel);

            // prepare
            play.Prepare(this.m_package, this.BeginMatch, x => this.EndMatch(x is IPlayModel y ? y : throw new ArgumentNullException()));

        }

        private void BeginMatch(IPlayModel model) {

            // Set lobby status here
            this.m_handle.SetLobbyState(LobbyAPIStructs.LobbyState.Playing);

            // Play match
            model.Play(this.EndMatch);

        }

        private void EndMatch(IPlayModel model) {

            // Set lobby status here
            this.m_handle.SetLobbyState(LobbyAPIStructs.LobbyState.InLobby);

            // Allow exit lobby
            //this.ExitLobby.Enabled = true;

        }

        private void MapSelectionChanged(int newIndex, int oldIndex) {

            // Get scenario
            var scen = this.MapDropdown.Items[newIndex].Scenario;

            if (!this.m_handle.SetTeamsCapacity(scen.MaxPlayers / 2)) {
                Task.Run(() => {
                    Task.Delay(1);
                    Application.Current.Dispatcher.Invoke(() => {
                        this.MapDropdown.Selected = oldIndex;
                    });
                });
                return;
            }

            // Update label
            this.MapDropdown.LabelContent = scen.Name;

            // Try get image
            this.ScenarioPreview = this.TryGetMapSource(scen);

            // Update gamemode
            this.UpdateGamemodeAndOptionsSelection(scen);

            // Notify change
            this.NotifyProperty(nameof(ScenarioPreview));

            // Update lobby
            this.m_handle.SetLobbySetting("selected_map", scen.RelativeFilename);

        }

        private void GamemodeSelectionChanged(int newIndex, int oldIndex) {

            // Get gamemode options
            var options = this.GamemodeDropdown.Items[newIndex].Options;

            // Clear available options
            this.GamemodeOptionDropdown.Items.Clear();

            // Hide options
            if (options is null || options.Length is 0) {

                // Set visibility to hidden
                this.GamemodeOptionDropdown.Visibility = Visibility.Hidden;

                // Set setting to empty (Hidden)
                this.m_handle.SetLobbySetting("selected_wco", string.Empty);

            } else {

                // Update options
                _ = options.ForEach(x => this.GamemodeOptionDropdown.Items.Add(x));

                // TODO: Set gamemode option that was last selected
                this.GamemodeOptionDropdown.Selected = 0;
                // Update label
                this.GamemodeOptionDropdown.LabelContent = this.GamemodeOptionDropdown.Items[0].Title;

                // Set visibility to visible
                this.GamemodeOptionDropdown.Visibility = Visibility.Visible;

            }

            // Update lobby
            this.m_handle.SetLobbySetting("selected_wc", this.GamemodeDropdown.Items[newIndex].Name);

        }

        private void GamemodeOptionSelectionChanged(int newIndex, int oldIndex) {

            // Bail
            if (newIndex == -1) {
                this.m_handle.SetLobbySetting("selected_wco", string.Empty);
                return;
            }
            
            // Update lobby
            this.m_handle.SetLobbySetting("selected_wco", this.GamemodeOptionDropdown.Items[newIndex].Value.ToString(CultureInfo.InvariantCulture));

        }

        private void WeatherSelectionChanged(int newIndex, int oldIndex) {

            // Update label
            this.WeatherDropdown.LabelContent = this.WeatherDropdown.Items[newIndex].IsOn.ToString();
        
            // Update lobby
            this.m_handle.SetLobbySetting("selected_daynight", this.WeatherDropdown.Items[newIndex].IsOn ? "1" : "0");

        }

        private void SupplySystemSelectionChanged(int newIndex, int oldIndex) {

            // Update label
            this.SupplySystemDropdown.LabelContent = this.SupplySystemDropdown.Items[newIndex].IsOn.ToString();

            // Update lobby
            this.m_handle.SetLobbySetting("selected_supply", this.SupplySystemDropdown.Items[newIndex].IsOn ? "1" : "0");

        }

        private void ModPackageSelectionChanged(int newIndex, int oldIndex) {

            // Set package
            this.m_package = this.ModPackageDropdown.Items[newIndex].ModPackage;

            // Update label
            this.ModPackageDropdown.LabelContent = this.ModPackageDropdown.Items[newIndex].ModPackage.PackageName;

            // Update lobby
            this.m_handle.SetLobbySetting("selected_tuning", this.ModPackageDropdown.Items[newIndex].ModPackage.ID);

        }

        private void UpdateGamemodeAndOptionsSelection(Scenario scenario) {

            // Bail if no package defined
            if (this.m_package is null) {
                return;
            }

            // Get available gamemodes
            var guid = this.m_package.GamemodeGUID;
            List<IGamemode> gamemodes =
                (scenario.Gamemodes.Count > 0 ? WinconditionList.GetGamemodes(guid, scenario.Gamemodes) : WinconditionList.GetGamemodes(guid)).ToList();

            // Update if there's any change in available gamemodes 
            if (this.GamemodeDropdown.Items.Count != gamemodes.Count || gamemodes.Any(x => !this.GamemodeDropdown.Items.Contains(x))) {

                // Clear current gamemode selection
                this.GamemodeDropdown.Items.Clear();
                gamemodes.ForEach(x => this.GamemodeDropdown.Items.Add(x));

                // TODO: Set gamemode that was last selected
                this.GamemodeDropdown.Selected = 0;

                // Update label
                this.GamemodeDropdown.LabelContent = this.GamemodeDropdown.Items[0].Name;

            }

        }

    }

}