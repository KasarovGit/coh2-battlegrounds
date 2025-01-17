﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Battlegrounds;
using Battlegrounds.Functional;
using Battlegrounds.Game;
using Battlegrounds.Game.Database;
using Battlegrounds.Modding;
using Battlegrounds.Networking.LobbySystem;

using BattlegroundsApp.Lobby.MatchHandling;

namespace BattlegroundsApp.Lobby.MVVM.Models;

public class LobbyHostModel : LobbyModel {
    
    private static readonly string __playabilityAlliesInvalid = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAlliesInvalid");
    private static readonly string __playabilityAlliesNoPlayers = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAlliesNoPlayers");
    private static readonly string __playabilityAxisInvalid = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAxisInvalid");
    private static readonly string __playabilityAxisNoPlayers = BattlegroundsInstance.Localize.GetString("LobbyView_StartMatchAxisNoPlayers");
    private static readonly string __playabilityNoticePersistency = BattlegroundsInstance.Localize.GetString("LobbyView_PersistencyDisabled");

    protected static readonly Func<int, string> LOCSTR_GAMEMODEUPLOAD = x => BattlegroundsInstance.Localize.GetString("LobbyView_UploadGamemode", x);

    private ModPackage? m_package;

    public override LobbyMutButton StartMatchButton { get; }

    public override LobbyDropdown<ScenOp> MapDropdown { get; }

    public override LobbyDropdown<IGamemode> GamemodeDropdown { get; }

    public override LobbyDropdown<IGamemodeOption> GamemodeOptionDropdown { get; }

    public override LobbyDropdown<OnOffOption> WeatherDropdown { get; }

    public override LobbyDropdown<OnOffOption> SupplySystemDropdown { get; }

    public override LobbyDropdown<ModPackageOption> ModPackageDropdown { get; }

    public ImageSource? SelectedMatchScenario { get; set; }

    public LobbyHostModel(ILobbyHandle handle, ILobbyTeam allies, ILobbyTeam axis) : base(handle, allies, axis) {

        // Init buttons
        this.StartMatchButton = new(new(() => this.m_isStarting.IfFalse().Then(this.BeginMatchSetup).Else(this.CancelMatch)), Visibility.Visible) { 
            Title = LOCSTR_START(),
            NotificationVisible = Visibility.Hidden
        };

        // Get scenario list
        var _scenlist = ScenarioList.GetList()
            .Where(x => x.IsVisibleInLobby)
            .OrderBy(x => x.MaxPlayers);
        var scenlist = new List<ScenOp>(_scenlist.Select(x => new ScenOp(x)));

        // Get & set tunning list
        var tunlist = new List<ModPackageOption>();
        ModManager.EachPackage(x => tunlist.Add(new ModPackageOption(x)));

        // Init mod package dropdown
        this.ModPackageDropdown = new(true, Visibility.Visible, new(tunlist), this.ModPackageSelectionChanged);

        // Set default package
        this.m_package = this.ModPackageDropdown.Items[0].ModPackage;

        // Get On & Off collection
        ObservableCollection<OnOffOption> onOfflist = new(new[] { new OnOffOption(true), new OnOffOption(false) });

        // Init dropdowns 
        this.MapDropdown = new(true, Visibility.Visible, new(scenlist), this.MapSelectionChanged);
        this.GamemodeDropdown = new(true, Visibility.Visible, new(), this.GamemodeSelectionChanged);
        this.GamemodeOptionDropdown = new(true, Visibility.Visible, new(), this.GamemodeOptionSelectionChanged);
        this.WeatherDropdown = new(true, Visibility.Visible, onOfflist, this.WeatherSelectionChanged);
        this.SupplySystemDropdown = new(true, Visibility.Visible, onOfflist, this.SupplySystemSelectionChanged);

        // Set dropdown index, cascade effect
        this.MapDropdown.Selected = 0;

        // Set opt-in settings
        this.WeatherDropdown.Selected = 0;
        this.SupplySystemDropdown.Selected = 0;
        this.ModPackageDropdown.Selected = 0;

        // Subscribe to events
        this.m_handle.OnLobbyTeamUpdate += this.OnTeamUpdated;
        this.m_handle.OnLobbySlotUpdate += this.OnSlotUpdated;

        // Set lobby status
        this.m_handle.SetLobbyState(LobbyState.InLobby);

        // Inform others
        if (this.TryGetSelf() is ILobbySlot self && self.Occupant is not null) {
            this.m_handle.MemberState(self.Occupant.MemberID, self.TeamID, self.SlotID, LobbyMemberState.Waiting);
        }

    }

    private async void OnSlotUpdated(ILobbySlot args) {
        await Task.Delay(100);
        Application.Current.Dispatcher.Invoke(() => {
            this.RefreshPlayability();
        });
    }

    private async void OnTeamUpdated(ILobbyTeam args) {
        await Task.Delay(100);
        Application.Current.Dispatcher.Invoke(() => {
            this.RefreshPlayability();
        });
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
            var notVis = this.AllowsPersitency();
            this.StartMatchButton.IsEnabled = true;
            this.StartMatchButton.Tooltip = notVis ? null : __playabilityNoticePersistency;
            this.StartMatchButton.NotificationVisible = !notVis ? Visibility.Visible : Visibility.Hidden;
        } else if (!allied) {
            this.StartMatchButton.IsEnabled = false;
            this.StartMatchButton.Tooltip = x1 ? __playabilityAlliesInvalid : __playabilityAlliesNoPlayers;
            this.StartMatchButton.NotificationVisible = Visibility.Visible;
        } else {
            this.StartMatchButton.IsEnabled = false;
            this.StartMatchButton.Tooltip = x2 ? __playabilityAxisInvalid : __playabilityAxisNoPlayers;
            this.StartMatchButton.NotificationVisible = Visibility.Visible;
        }

    }

    private bool AllowsPersitency() {
        uint totalPlayers = this.m_handle.GetPlayerCount(false);
        uint totalHumans = this.m_handle.GetPlayerCount(true);
        uint totalAI = totalPlayers  - totalHumans;
        int hardsum = this.m_handle.Allies.Slots.Count(x => x.IsAI(AIDifficulty.AI_Hard)) + this.m_handle.Axis.Slots.Count(x => x.IsAI(AIDifficulty.AI_Hard));
        return hardsum == totalAI;
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

        // Bail if not ready
        if (!this.IsReady()) {
            // TODO: Inform user
            return;
        }

        // Do on a worker thread
        Task.Run(() => {

            // Get status from other participants
            if (!this.m_handle.ConductPoll("ready_check", 1.5)) {
                Trace.WriteLine("Someone didn't report back positively!", nameof(LobbyHostModel));
                return;
            }

            // Set starting flag
            this.m_isStarting = true;

            // Set lobby status here
            this.m_handle.SetLobbyState(LobbyState.Starting);

            // Get play model
            var play = PlayModelFactory.GetModel(this.m_handle, this.m_chatModel, this.UploadGamemodeCallback);

            // prepare
            play.Prepare(this.m_package, this.BeginMatch, x => this.EndMatch(x is IPlayModel y ? y : throw new ArgumentNullException()));

        });

    }

    private void BeginMatch(IPlayModel model) {

        // Set lobby status here
        this.m_handle.SetLobbyState(LobbyState.Playing);

        // Play match
        model.Play(this.GameLaunching, this.EndMatch);

    }

    private void UploadGamemodeCallback(int curr, int exp, bool cancelled) {

        // Calculate percentage
        int p = Math.Min(100, (int)(curr / (double)exp * 100.0));

        // Notify users of upload status
        this.m_handle.NotifyMatch("upload_status", p.ToString());

        // Update visually
        Application.Current.Dispatcher.Invoke(() => {
            if (p == 100) {
                this.StartMatchButton.Title = LOCSTR_PLAYING();
            } else {
                this.StartMatchButton.Title = LOCSTR_GAMEMODEUPLOAD(p);
            }
        });

    }

    private void GameLaunching() {

        // Notify
        Application.Current.Dispatcher.Invoke(() => {
            this.StartMatchButton.IsEnabled = false;
            this.StartMatchButton.Title = LOCSTR_PLAYING();
        });

    }

    private void EndMatch(IPlayModel model) {

        // Set lobby status here
        this.m_handle.SetLobbyState(LobbyState.InLobby);

        // Re-enable
        Application.Current.Dispatcher.Invoke(() => {
            this.StartMatchButton.IsEnabled = true;
            this.StartMatchButton.Title = LOCSTR_START();
        });

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
        this.ScenarioPreview = LobbySettingsLookup.TryGetMapSource(scen);

        // Update gamemode
        this.UpdateGamemodeAndOptionsSelection(scen);

        // Notify change
        this.NotifyProperty(nameof(ScenarioPreview));

        // Update lobby
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_MAP, scen.RelativeFilename);

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
            this.m_handle.SetLobbySetting(LobbyConstants.SETTING_GAMEMODEOPTION, string.Empty);

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
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_GAMEMODE, this.GamemodeDropdown.Items[newIndex].Name);

    }

    private void GamemodeOptionSelectionChanged(int newIndex, int oldIndex) {

        // Bail
        if (newIndex == -1) {
            this.m_handle.SetLobbySetting(LobbyConstants.SETTING_GAMEMODEOPTION, string.Empty);
            return;
        }
        
        // Update lobby
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_GAMEMODEOPTION, this.GamemodeOptionDropdown.Items[newIndex].Value.ToString(CultureInfo.InvariantCulture));

    }

    private void WeatherSelectionChanged(int newIndex, int oldIndex) {

        // Update label
        this.WeatherDropdown.LabelContent = this.WeatherDropdown.Items[newIndex].IsOn.ToString();
    
        // Update lobby
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_WEATHER, this.WeatherDropdown.Items[newIndex].IsOn ? "1" : "0");

    }

    private void SupplySystemSelectionChanged(int newIndex, int oldIndex) {

        // Update label
        this.SupplySystemDropdown.LabelContent = this.SupplySystemDropdown.Items[newIndex].IsOn.ToString();

        // Update lobby
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_LOGISTICS, this.SupplySystemDropdown.Items[newIndex].IsOn ? "1" : "0");

    }

    private void ModPackageSelectionChanged(int newIndex, int oldIndex) {

        // Set package
        this.m_package = this.ModPackageDropdown.Items[newIndex].ModPackage;

        // Update label
        this.ModPackageDropdown.LabelContent = this.ModPackageDropdown.Items[newIndex].ModPackage.PackageName;

        // Update lobby
        this.m_handle.SetLobbySetting(LobbyConstants.SETTING_MODPACK, this.ModPackageDropdown.Items[newIndex].ModPackage.ID);

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
