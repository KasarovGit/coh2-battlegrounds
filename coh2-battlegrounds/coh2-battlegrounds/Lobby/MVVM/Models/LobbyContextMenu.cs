﻿using System;
using System.ComponentModel;
using System.Windows;

using Battlegrounds;
using Battlegrounds.Networking.LobbySystem;

using BattlegroundsApp.Utilities;

namespace BattlegroundsApp.Lobby.MVVM.Models;

public abstract class LobbyContextMenu {

    protected static readonly Func<bool> NeverTrue = () => false;
    protected static readonly Func<LobbyContextAction, Visibility> NeverVisible = _ => Visibility.Collapsed;
    protected static readonly Func<LobbyContextAction, Visibility> AlwaysVisible = _ => Visibility.Visible;
    protected static readonly Func<LobbyContextAction, Visibility> VisibleIfEnabled = x => x.Enabled ? Visibility.Visible : Visibility.Collapsed;

    protected static readonly Func<string> LOCSTR_SHOWPLAYERCARD = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Playercard");
    protected static readonly Func<string> LOCSTR_UNLOCK_SLOT = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Unlock_Slot");
    protected static readonly Func<string> LOCSTR_LOCK_SLOT = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Lock_Slot");
    protected static readonly Func<string> LOCSTR_MOVE_SLOT = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Move_Position");
    protected static readonly Func<string> LOCSTR_KICKPLAYER = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Kick_Player");
    protected static readonly Func<string> LOCSTR_EASYAI = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Add_Easy_Ai");
    protected static readonly Func<string> LOCSTR_STANDARDAI = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Add_Standard_Ai");
    protected static readonly Func<string> LOCSTR_HARDAI = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Add_Hard_Ai");
    protected static readonly Func<string> LOCSTR_EXPERTAI = () => BattlegroundsInstance.Localize.GetString("TeamPlayerCard_Menu_Item_Add_Expert_Ai");

    public record LobbyContextAction(string Title, RelayCommand Click, Func<bool> EnabledTest, Func<LobbyContextAction, Visibility> VisibilityTest) : INotifyPropertyChanged {
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool Enabled {
            get => this.EnabledTest();
            set {
                this.PropertyChanged?.Invoke(this, new(nameof(Enabled)));
            }
        }
        public Visibility Visibility {
            get => this.VisibilityTest(this);
            set {
                this.PropertyChanged?.Invoke(this, new(nameof(Visibility)));
            }
        }
    }

    public LobbyAPI Handle { get; }
    
    public LobbySlot Slot { get; }

    public LobbyContextAction ShowPlayercard { get; }

    public abstract LobbyContextAction KickPlayer { get; }

    public abstract LobbyContextAction LockSlot { get; }

    public abstract LobbyContextAction UnlockSlot { get; }

    public LobbyContextAction MoveToSlot { get; }
    
    public abstract LobbyContextAction AddEasyAI { get; }

    public abstract LobbyContextAction AddStandardAI { get; }

    public abstract LobbyContextAction AddHardAI { get; }

    public abstract LobbyContextAction AddExpertAI { get; }

    public Visibility LastSepVisible 
        => this.LockSlot.Visibility is Visibility.Visible || this.UnlockSlot.Visibility is Visibility.Visible || this.MoveToSlot.Visibility is Visibility.Visible
        ? Visibility.Visible
        : Visibility.Collapsed;

    public int TeamId => this.Slot.Slot.TeamID;

    public int SlotId => this.Slot.Slot.SlotID;

    public LobbyContextMenu(LobbyAPI handle, LobbySlot slot) {
        
        // Set basics
        this.Handle = handle;
        this.Slot = slot;
        
        // Define common actions
        this.ShowPlayercard = new(LOCSTR_SHOWPLAYERCARD(), new(this.ShowPlayercardAction), () => slot.Slot.IsOccupied, AlwaysVisible);
        this.MoveToSlot = new(LOCSTR_MOVE_SLOT(), new(this.MoveSelfToSlotAction), () => !slot.Slot.IsOccupied, VisibleIfEnabled);

    }

    protected void ShowPlayercardAction() {
        // TODO: Implement
    }

    protected void MoveSelfToSlotAction()
        => this.Handle.MoveSlot(this.Handle.Self.ID, this.TeamId, this.SlotId);

}
