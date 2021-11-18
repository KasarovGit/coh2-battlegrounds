﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Battlegrounds.Modding;
using Battlegrounds.Networking.LobbySystem;

namespace BattlegroundsApp.Lobby.MatchHandling;

internal class OnlineModel : IPlayModel {

    private readonly LobbyHandler m_handler;

    public OnlineModel(LobbyHandler handler) {
        this.m_handler = handler;
    }

    public void Prepare(ModPackage modPackage, PrepareOverHandler prepareOver, PrepareCancelledHandler prepareCancelled) {

    }

    public void Play(PlayOverHandler matchOver) {

    }

}
