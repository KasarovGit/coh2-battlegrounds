﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

using Battlegrounds.Functional;
using Battlegrounds.Game;
using Battlegrounds.Game.Database;
using Battlegrounds.Game.Database.Management;
using Battlegrounds.Game.DataCompany;
using Battlegrounds.Game.Gameplay;
using Battlegrounds.Game.Match;
using Battlegrounds.Game.Match.Analyze;
using Battlegrounds.Game.Match.Composite;
using Battlegrounds.Game.Match.Finalizer;
using Battlegrounds.Game.Match.Startup;
using Battlegrounds.Modding;
using Battlegrounds.Networking.LobbySystem;
using Battlegrounds.Util;
using Battlegrounds.Verification;

using BattlegroundsApp.Lobby.MVVM.Models;
using BattlegroundsApp.LocalData;

namespace BattlegroundsApp.Lobby.MatchHandling;

internal abstract class BasePlayModel {

    // Load refs
    protected readonly ILobbyHandle m_handle;
    protected readonly LobbyChatSpectatorModel m_chat;

    // The strategies to use
    protected IStartupStrategy? m_startupStrategy;
    protected IAnalyzeStrategy? m_matchAnalyzer;
    protected IFinalizeStrategy? m_finalizeStrategy;

    // Local prepared data
    protected SessionInfo m_info;
    protected Company? m_selfCompany;

    // The session handler
    protected MultiplayerSession? m_session;
    protected MatchController? m_controller;

    // handlers for cancel/error cases
    private PrepareCancelHandler? m_prepCancelHandler;

    /// <summary>
    /// Initialise a <see cref="BasePlayModel"/> instance.
    /// </summary>
    /// <param name="handle">The lobby handle to create play instance for.</param>
    /// <param name="lobbyChat">The chat instance to use when communicating progress.</param>
    public BasePlayModel(ILobbyHandle handle, LobbyChatSpectatorModel lobbyChat) {

        // Set base stuff
        this.m_handle = handle;
        this.m_chat = lobbyChat;

    }

    protected void BasePrepare(ModPackage modPackage, PrepareCancelHandler cancelHandler) {

        // Error if not set up
        if (this.m_startupStrategy is null) {
            throw new StartupException("Failed to invoke 'BasePlayModel.BasePrepare'. Please make sure the startup strategy is set.");
        }

        // Error if not set up
        if (this.m_matchAnalyzer is null) {
            throw new StartupException("Failed to invoke 'BasePlayModel.BasePrepare'. Please make sure the analyzer strategy is set.");
        }

        // Error if not set up
        if (this.m_finalizeStrategy is null) {
            throw new StartupException("Failed to invoke 'BasePlayModel.BasePrepare'. Please make sure the finalise strategy is set.");
        }

        // Create settings
        this.CreateMatchInfo(modPackage);

        // Add listeners
        this.m_startupStrategy.StartupFeedback += this.HandleStartupInformation;
        this.m_startupStrategy.StartupCancelled += this.HandleStartupCancel;

        // Create session
        this.m_session = new MultiplayerSession(this.m_handle);

        // Create controller
        this.m_controller = new();
        this.m_controller.SetStartupObjects(this.m_session, this.m_startupStrategy);
        this.m_controller.SetAnalysisObjects(this.m_session, this.m_matchAnalyzer);
        this.m_controller.SetFinalizerObjects(this.m_session, this.m_finalizeStrategy);

        // Set crash/err handler
        this.m_prepCancelHandler = cancelHandler;

    }

    protected void CreateMatchInfo(ModPackage package) {

        // Get settings (most up-to-date)
        var settings = this.m_handle.Settings;

        // Collect settings
        string scenario = settings.GetOrDefault("selected_map", string.Empty);
        string gamemode = settings.GetOrDefault("selected_wc", string.Empty);
        string gamemodeValue = settings.GetOrDefault("selected_wco", string.Empty);
        bool enableSupply = settings.GetOrDefault("selected_supply", "0") != "0";
        bool enableWeather = settings.GetOrDefault("selected_daynight", "0") != "0";

        // Try get gamemode value
        if (!int.TryParse(gamemodeValue, out int gamemodeoption)) {
            Trace.WriteLine($"Failed to convert gamemode option {gamemodeoption} into an integer value.", nameof(BasePlayModel));
            gamemodeoption = 0;
        }

        // Create three counters
        ValRef<byte> totalCounter = 0;
        ValRef<byte> alliesCounter = 0;
        ValRef<byte> axisCounter = 0;

        // Get teams (most up-to-date)
        var alliesLobby = this.m_handle.Allies;
        var axisLobby = this.m_handle.Axis;

        // Get allies
        var allies = alliesLobby.Slots.Filter(x => x.State is 1)
            .Map(x => x.Occupant)
            .Map(x => x is null ? throw new StartupException("Invalid allied occupant") : CreateParticipantFromLobbyMember(x, ParticipantTeam.TEAM_ALLIES, totalCounter, alliesCounter));

        // Get axis
        var axis = axisLobby.Slots.Filter(x => x.State is 1)
            .Map(x => x.Occupant)
            .Map(x => x is null ? throw new StartupException("Invalid axis occupant") : CreateParticipantFromLobbyMember(x, ParticipantTeam.TEAM_AXIS, totalCounter, axisCounter));

        // Get scenario
        var scen = ScenarioList.FromFilename(scenario);
        if (scen is null) {
            throw new StartupException($"Failed to fetch scenario {scenario} from scenario list.");
        }

        // Grab gamemode
        var gamemodeInstance = WinconditionList.GetGamemodeByName(package.GamemodeGUID, gamemode);
        if (gamemodeInstance is null) {
            throw new StartupException($"Failed to find gamemode with name '{gamemode}' from wincondition list (mod package = {package.ID}).");
        }

        // Grab tuning
        var tuningInstance = ModManager.GetMod<ITuningMod>(package.TuningGUID);
        if (tuningInstance is null) {
            throw new StartupException($"Failed to fetch tuning mod.");
        }

        // Create plan containers
        SessionPlanEntityInfo[] sessionEntities;
        SessionPlanGoalInfo[] sessionGoals;
        SessionPlanSquadInfo[] sessionSquads;

        // Create plan info
        if (gamemodeInstance.HasPlanning && this.m_handle.PlanningHandle is not null) {

            // Union participants and map by index
            var participants = allies.Concat(axis).ToLookup(x => x.GetID());

            // Grab elements
            var elements = this.m_handle.PlanningHandle.GetPlanningElements(0).Concat(this.m_handle.PlanningHandle.GetPlanningElements(1));

            // Invoke helper functions
            sessionGoals = CreatePlanningGoals(participants, elements);
            sessionEntities = CreatePlanningEntities(participants, elements);
            sessionSquads = CreatePlanningSquads(participants, elements);

        } else {
            
            // Init empty data
            sessionEntities = Array.Empty<SessionPlanEntityInfo>();
            sessionGoals = Array.Empty<SessionPlanGoalInfo>();
            sessionSquads = Array.Empty<SessionPlanSquadInfo>();

        }

        // Create container for additional settings
        var additionalSettings = new Dictionary<string, int>();

        // Add custom settings
        for (int i = 0; i < gamemodeInstance.AuxiliaryOptions.Length; i++) {
            string optionKey = gamemodeInstance.AuxiliaryOptions[i].Name;
            if (int.TryParse(this.m_handle.Settings[optionKey], out int v)) {
                additionalSettings[optionKey] = v;
            }
        }

        // Create info data
        this.m_info = new() {
            FillAI = false,
            SelectedScenario = scen,
            SelectedGamemode = gamemodeInstance,
            SelectedGamemodeOption = gamemodeoption,
            IsOptionValue = true,
            SelectedTuningMod = tuningInstance,
            Allies = allies,
            Axis = axis,
            EnableDayNightCycle = enableWeather,
            EnableSupply = enableSupply,
            Squads = sessionSquads,
            Goals = sessionGoals,
            Entities = sessionEntities,
            IsFixedTeamOrder = gamemodeInstance.RequireFixed,
            ReverseTeamOrder = this.m_handle.AreTeamRolesSwapped(),
            AdditionalOptions = additionalSettings
        };

    }

    protected SessionParticipant CreateParticipantFromLobbyMember(ILobbyMember participant, ParticipantTeam team, ValRef<byte> count, ValRef<byte> index) {
        
        // Update indicies
        byte tIndex = index.Change(x => ++x);
        byte pIndex = count.Change(x => ++x);

        // Add participant based on role
        if (participant.Role is 3) {
            var aiCompany = participant.Company;
            if (aiCompany is null) {
                throw new StartupException("AI startup company was null!");
            }
            var c = aiCompany.IsAuto ? null : PlayerCompanies.FromNameAndFaction(aiCompany.Name, Faction.FromName(aiCompany.Army));
            return new SessionParticipant((AIDifficulty)participant.AILevel, c, team, tIndex, pIndex);
        } else {
            if (participant.MemberID == this.m_handle.Self.ID) {
                if (participant.Company is not ILobbyCompany c) {
                    throw new StartupException("Invalid startup company.");
                }
                this.m_selfCompany = PlayerCompanies.FromNameAndFaction(c.Name, Faction.FromName(participant.Company.Army));
            }
            return new SessionParticipant(participant.DisplayName, participant.MemberID, null, team, tIndex, pIndex);
        }

    }

    protected static SessionPlanEntityInfo[] CreatePlanningEntities(Dictionary<ulong, SessionParticipant> participants, ILobbyPlanElement[] planElements) {

        // Grab planning entities
        var planEntities = planElements.Filter(x => x.IsEntity && x.ObjectiveType is PlanningObjectiveType.None);

        // Create array
        var entities = new SessionPlanEntityInfo[planEntities.Length];

        // Loop over all entities
        for (int i = 0; i < planEntities.Length; i++) {

            // Get participant
            var p = participants[planEntities[i].ElementOwnerId];

            // Create entity
            entities[i] = new() {
                TeamOwner = (int)p.TeamIndex,
                TeamMemberOwner = p.PlayerIndexOnTeam,
                Blueprint = BlueprintManager.FromBlueprintName<EntityBlueprint>(planEntities[i].Blueprint),
                Spawn = planEntities[i].SpawnPosition,
                Lookat = planEntities[i].LookatPosition,
                IsDirectional = planEntities[i].IsDirectional
            };

        }

        // Return
        return entities;

    }

    protected static SessionPlanSquadInfo[] CreatePlanningSquads(Dictionary<ulong, SessionParticipant> participants, ILobbyPlanElement[] planElements) {

        // Grab planning squads
        var planSquads = planElements.Filter(x => !x.IsEntity && x.ObjectiveType is PlanningObjectiveType.None);

        // Create array
        var squads = new SessionPlanSquadInfo[planSquads.Length];

        // Loop over all squads
        for (int i = 0; i < planSquads.Length; i++) {

            // Get participant
            var p = participants[planSquads[i].ElementOwnerId];

            // Create squad
            squads[i] = new() {
                TeamOwner = (int)p.TeamIndex,
                TeamMemberOwner = p.PlayerIndexOnTeam,
                SpawnId = planSquads[i].CompanyId,
                Spawn = planSquads[i].SpawnPosition,
                Lookat = planSquads[i].LookatPosition
            };

        }

        // Return
        return squads;

    }

    protected static SessionPlanGoalInfo[] CreatePlanningGoals(Dictionary<ulong, SessionParticipant> participants, ILobbyPlanElement[] planElements) {

        // Grab all goals
        var planGoals = planElements.Filter(x => x.ObjectiveType is not PlanningObjectiveType.None);

        // Create goals array
        var goals = new SessionPlanGoalInfo[planGoals.Length];

        // Loop over all goals
        for (int i = 0; i < planGoals.Length; i++) {

            // get participant
            var p = participants[planGoals[i].ElementOwnerId];

            // Create goal
            goals[i] = new() {
                ObjectiveTeam = (byte)p.TeamIndex,
                ObjectivePlayer = p.PlayerIndexOnTeam,
                ObjectiveType = (byte)planGoals[i].ObjectiveType,
                ObjectiveIndex = (byte)planGoals[i].ObjectiveOrder,
                ObjectivePosition = planGoals[i].SpawnPosition
            };

        }

        // Return goals
        return goals;

    }

    protected void HandleStartupCancel(IStartupStrategy sender, object? caller, string reason) {
        this.m_chat.SystemMessage(reason, Colors.Red);
        this.m_handle.NotifyError("startup", reason);
        this.m_prepCancelHandler?.Invoke(this);
    }

    protected void HandleStartupInformation(IStartupStrategy sender, object? caller, string message) {
        this.m_chat.SystemMessage(message, Colors.DarkGray);
        this.m_handle.NotifyMatch("startup", message);
    }

    protected void OnCompanySerialized(Company company) {

        // Run through a sanitizer
        try {

            // Save the company
            PlayerCompanies.SaveCompany(company);

        } catch (ChecksumViolationException checksumViolation) {

            // Log checksum violation
            Trace.WriteLine(checksumViolation, nameof(BasePlayModel));

        } catch (OperationCanceledException cancelledException) {

            // Log checksum violation
            Trace.WriteLine(cancelledException, nameof(BasePlayModel));

        }

    }

}
