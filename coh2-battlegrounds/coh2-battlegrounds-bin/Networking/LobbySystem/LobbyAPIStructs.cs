﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlegrounds.Networking.LobbySystem {

    /// <summary>
    /// 
    /// </summary>
    public static class LobbyAPIStructs {

        public interface IAPIObject {
            public LobbyAPI API { get; set; }
            public void SetAPI(LobbyAPI api);
        }

        public class LobbyCompany : IAPIObject {
            public bool IsAuto { get; set; }
            public bool IsNone { get; set; }
            public string Name { get; set; }
            public string Army { get; set; }
            public float Strength { get; set; }
            public string Specialisation { get; set; }
            public LobbyAPI API { get; set; }
            public void SetAPI(LobbyAPI api) {
                this.API = api;
            }
        }

        public class LobbyMember : IAPIObject {

            public ulong MemberID { get; set; }
            public string DisplayName { get; set; }
            public int Role { get; set; }
            public int AILevel { get; set; }
            public LobbyCompany Company { get; set; }
            public LobbyAPI API { get; set; }
            public void SetAPI(LobbyAPI api) {
                this.API = api;
                this.Company.API = api;
            }
        }

        public class LobbySlot : IAPIObject {

            public int SlotID { get; set; }
            public byte State { get; set; }
            public LobbyMember Occupant { get; set; }
            public LobbyAPI API { get; set; }

            public bool IsSelf() {
                if (this.Occupant is LobbyMember mem) {
                    return mem.MemberID == this.API.Self.ID;
                }
                return false;
            }

            public bool IsAI() {
                if (this.Occupant is LobbyMember mem) {
                    return mem.Role == 3;
                }
                return false;
            }
            public void SetAPI(LobbyAPI api) {
                this.API = api;
                this.Occupant?.SetAPI(api);
            }

        }

        public class LobbyTeam : IAPIObject {

            public LobbySlot[] Slots { get; set; }
            public int TeamID { get; set; }
            public int Capacity { get; set; }
            public LobbyAPI API { get; set; }
            public void SetAPI(LobbyAPI api) {
                this.API = api;
                for (int i = 0; i < this.Slots.Length; i++) {
                    this.Slots[i].SetAPI(api);
                }
            }
        }

        public class LobbyMessage {
            public string Timestamp { get; set; }
            public string Sender { get; set; }
            public string Message { get; set; }
            public string Channel { get; set; }
            public string Colour { get; set; }
        }

        public class LobbyRemote {
            public ulong HostID { get; set; }
            public LobbyTeam[] Teams { get; set; }
            public Dictionary<string, string> Settings { get; set; }
        }

    }

}
