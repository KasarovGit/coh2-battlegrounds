﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Battlegrounds.Networking.LobbySystem;

namespace BattlegroundsApp.Lobby.MVVM.Models;
public class LobbyParticipantSlotModel : LobbySlot {

    public override Visibility IsCompanySelectorVisible => this.Slot.IsSelf() ? Visibility.Visible : Visibility.Collapsed;

    public LobbyParticipantSlotModel(LobbyAPIStructs.LobbySlot teamSlot, LobbyTeam team) : base(teamSlot, team) {
    


    }

    protected override void OnLobbyCompanyChanged(int newValue) {
        if (this.Slot.API is null) {
            return;
        }
        throw new NotImplementedException();
    }
}
