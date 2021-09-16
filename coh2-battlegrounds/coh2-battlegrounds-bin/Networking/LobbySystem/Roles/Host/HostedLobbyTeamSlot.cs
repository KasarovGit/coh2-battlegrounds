﻿namespace Battlegrounds.Networking.LobbySystem.Roles.Host {

    public class HostedLobbyTeamSlot : ILobbyTeamSlot {

        ILobbyParticipant m_participant;
        TeamSlotState m_state;

        public ILobbyTeam SlotTeam { get; }

        public TeamSlotState SlotState => this.m_state;

        public ILobbyParticipant SlotOccupant => this.m_participant;

        public HostedLobbyTeamSlot(ILobbyTeam team) {

            // Set team
            this.SlotTeam = team;

            // Set initial state
            this.m_state = TeamSlotState.Open;
            this.m_participant = null;

        }

        public bool SetOccupant(ILobbyParticipant occupant) {

            // If already occupied, it's not possible
            if (this.m_participant is not null) {
                return false;
            }

            // Check if open, and assign occupant if that is the case
            if (this.m_state is TeamSlotState.Open) {
                this.m_participant = occupant;
                this.m_state = TeamSlotState.Occupied;
                return true;
            }

            // base case is default
            return false;

        }

        public bool SetState(TeamSlotState newState) {

            // If occuped, set as occupied and reutrn file
            if (this.m_participant is not null) {
                this.m_state = TeamSlotState.Occupied;
                return newState != TeamSlotState.Occupied;
            }

            // Set state and return true
            this.m_state = newState;
            return true;

        }

        public void ClearOccupant() {

            // Set values
            this.m_participant = null;
            this.m_state = TeamSlotState.Open;

        }

    }

}
