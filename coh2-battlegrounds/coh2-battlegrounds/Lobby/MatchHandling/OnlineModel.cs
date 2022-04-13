﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Battlegrounds.Game.Match.Analyze;
using Battlegrounds.Game.Match.Finalizer;
using Battlegrounds.Game.Match.Play.Factory;
using Battlegrounds.Game.Match.Startup;
using Battlegrounds.Modding;
using Battlegrounds.Networking.LobbySystem;

using BattlegroundsApp.Lobby.MVVM.Models;

namespace BattlegroundsApp.Lobby.MatchHandling;

internal class OnlineModel : BasePlayModel, IPlayModel {

    public OnlineModel(LobbyAPI handler, LobbyChatSpectatorModel lobbyChat) : base(handler, lobbyChat) {

        // Startup strategy
        this.m_startupStrategy = new OnlineStartupStrategy {
            LocalCompanyCollector = () => this.m_selfCompany,
            SessionInfoCollector = () => this.m_info,
            PlayStrategyFactory = new OverwatchStrategyFactory(),
        };

        // Analysis strategy
        this.m_matchAnalyzer = new OnlineMatchAnalyzer { };

        // Finalizer strategy
        this.m_finalizeStrategy = new MultiplayerFinalizer {
            CompanyHandler = this.OnCompanySerialized,
        };

    }

    public void Prepare(ModPackage modPackage, PrepareOverHandler prepareOver, PrepareCancelHandler prepareCancelled) {

        // Invoke async so we can wait on others without freezing
        Task.Run(() => {

            // Invoke base
            this.BasePrepare(modPackage, prepareCancelled);

            // Trigger prepare over on GUI thread
            Application.Current.Dispatcher.Invoke(() => {
                prepareOver?.Invoke(this);
            });

        });

    }

    public void Play(PlayOverHandler matchOver) {

        // Ensure controller exists
        if (this.m_controller is null)
            throw new InvalidOperationException("Cannot invoke 'Play' until a controller instance has been defined.");

        // Set on complete handler
        this.m_controller.Complete += m => GameCompleteHandler(m, matchOver);
        this.m_controller.Error += (o, r) => GameErrorHandler(r, matchOver);

        // Begin match
        this.m_controller.Control();

    }

    private void GameErrorHandler(string r, PlayOverHandler matchOver) {

        // Log error
        this.m_chat.SystemMessage($"Match Error - {r}", Colors.Red);

        // Invoke over event in lobby model.
        Application.Current.Dispatcher.Invoke(() => {
            matchOver.Invoke(this);
        });

    }

    private void GameCompleteHandler(IAnalyzedMatch match, PlayOverHandler handler) {

        // do stuff with match?
        if (match.IsFinalizableMatch) {
            this.m_chat.SystemMessage("Match over - Invalid match.", Colors.DarkGray);
        } else {
            this.m_chat.SystemMessage("Match over - Match saved.", Colors.DarkGray);
        }

        // Invoke over event in lobby model.
        Application.Current.Dispatcher.Invoke(() => {
            handler.Invoke(this);
        });

    }

}
