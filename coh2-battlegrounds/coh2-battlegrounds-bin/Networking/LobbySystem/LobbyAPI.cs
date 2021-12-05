﻿using System;
using System.Collections.Generic;

using Battlegrounds.Networking.Communication.Broker;
using Battlegrounds.Networking.Communication.Connections;
using Battlegrounds.Networking.Memory;
using Battlegrounds.Networking.Server;
using Battlegrounds.Steam;

using static Battlegrounds.Networking.LobbySystem.LobbyAPIStructs;

namespace Battlegrounds.Networking.LobbySystem {
    
    public class LobbyAPI {

        private static readonly TimeSpan __cacheTime = TimeSpan.FromSeconds(15);

        private ServerConnection m_connection;
        private bool m_isHost;
        private uint m_cidcntr;

        private ObjectCache<LobbyTeam> m_allies;
        private ObjectCache<LobbyTeam> m_axis;
        private ObjectCache<LobbyTeam> m_obs;
        private ObjectCache<Dictionary<string, string>> m_settings;

        public bool IsHost => this.m_isHost;

        public LobbyTeam Allies => this.m_allies.GetCachedValue(() => this.GetTeam(0));

        public LobbyTeam Axis => this.m_allies.GetCachedValue(() => this.GetTeam(1));

        public LobbyTeam Observers => this.m_allies.GetCachedValue(() => this.GetTeam(2));

        public Dictionary<string, string> Settings => this.m_settings.GetCachedValue(this.GetSettings);

        public ServerAPI ServerHandle { get; }

        public SteamUser Self { get; }

        public event Action<LobbyMessage> OnChatMessage;

        public LobbyAPI(bool isHost, SteamUser self, ServerConnection connection, ServerAPI serverAPI) {

            // Store ref to server handle
            this.ServerHandle = serverAPI;

            // Set internal refs
            this.m_connection = connection;
            this.m_isHost = isHost;
            this.m_cidcntr = 100;

            // Store self (id)
            this.Self = self;

            // Add private hook
            this.m_connection.MessageReceived = this.OnMessage;

            // Create get lobby message
            Message msg = new Message() {
                CID = this.m_cidcntr++,
                Content = BrokerMarshal.JsonMarshal(new RemoteCallMessage() { Method = "GetLobby", Arguments = Array.Empty<string>() }),
                Mode = MessageMode.BrokerCall,
                Sender = this.m_connection.SelfID,
                Target = 0
            };

            // Make pull lobby request
            ContentMessage? reply = this.m_connection.SendAndAwaitReply(msg);
            if (reply is ContentMessage response && BrokerMarshal.JsonUnmarshal<LobbyRemote>(response.Raw) is LobbyRemote remoteLobby) {

                // Create team data
                this.m_allies = new(remoteLobby.Teams[0], __cacheTime);
                this.m_axis = new(remoteLobby.Teams[1], __cacheTime);
                this.m_obs = new(remoteLobby.Teams[2], __cacheTime);
                this.m_settings = new(remoteLobby.Settings, __cacheTime);

            } else {

                // Throw exception -> Failed to fully connect.
                throw new Exception("Failed to retrieve light-lobby version.");

            }

        }

        private void OnMessage(uint cid, ulong sender, ContentMessage message) {

            // Check message type
            if (message.StrMsg == "Message") {

                // Unmarshal
                LobbyMessage lobbyMessage = BrokerMarshal.JsonUnmarshal<LobbyMessage>(message.Raw);
                //lobbyMessage.Timestamp = ... // TODO: Fix

                // Trigger event
                this.OnChatMessage?.Invoke(lobbyMessage);

            }

        }

        public void Disconnect() {
            this.m_connection.Shutdown();
        }

        public LobbyTeam GetTeam(int tid)
            => RemoteCall<LobbyTeam>("GetTeam", tid.ToString());

        public LobbyMember GetLobbyMember(ulong mid)
            => RemoteCall<LobbyMember>("GetLobbyMember", mid.ToString());

        public LobbyCompany GetCompany(ulong mid)
            => RemoteCall<LobbyCompany>("GetCompany", mid.ToString());

        public Dictionary<string, string> GetSettings()
            => RemoteCall<Dictionary<string, string>>("GetSettings");

        public uint GetPlayerCount()
            => RemoteCall<uint>("GetPlayerCount");

        private static string EncBool(bool b) => b ? "1" : "0";

        public void SetCompany(ulong mid, LobbyCompany company)
            => RemoteVoidCall("SetCompany", mid.ToString(), EncBool(company.IsAuto), EncBool(company.IsNone), company.Name, company.Army, company.Strength.ToString(), company.Specialisation);

        public void SetAICompany(int tid, int sid, LobbyCompany company)
            => throw new NotImplementedException();

        public void MoveSlot(ulong mid, int tid, int sid)
            => RemoteVoidCall("MoveSlot", mid.ToString(), tid.ToString(), sid.ToString());

        public void AddAI(int tid, int sid, int difficulty, LobbyCompany company)
            => RemoteVoidCall("AddAI", tid.ToString(), sid.ToString(), difficulty.ToString() ,EncBool(company.IsAuto), EncBool(company.IsNone), company.Name, company.Army, company.Strength.ToString(), company.Specialisation);

        public void RemoveOccupant(int tid, int sid)
            => RemoteVoidCall("RemoveOccupant", tid.ToString(), sid.ToString());

        public void LockSlot(int tid, int sid)
            => RemoteVoidCall("LockSlot", tid.ToString(), sid.ToString());

        public void UnlockSlot(int tid, int sid)
            => RemoteVoidCall("UnlockSlot", tid.ToString(), sid.ToString());

        public void GlobalChat(ulong mid, string msg)
            => RemoteVoidCall("GlobalChat", mid.ToString(), msg);

        public void TeamChat(ulong mid, string msg)
            => RemoteVoidCall("TeamChat", mid.ToString(), msg);

        public void SetLobbySetting(string setting, string value) {
            if (this.m_isHost) {
                RemoteVoidCall("SetLobbySetting", setting, value);
            }
        }

        public bool SetTeamsCapacity(int newCapacity) {
            if (this.m_isHost) {                
                return this.RemoteCall<bool>("SetTeamsCapacity", newCapacity.ToString());
            }
            return false;
        }

        public bool StartMatch(double cancelTime) {
            return false;
        }

        public void LaunchMatch() {
            if (this.m_isHost) {
                this.RemoteVoidCall("LaunchGame");
            }
        }

        public void RequestCompanyFiles(params ulong[] members) {

        }


        public void ReleaseGamemode() {
            if (this.m_isHost) {
                this.RemoteVoidCall("ReleaseGamemode");
            }
        }

        public void ReleaseResults() {
            if (this.m_isHost) {
                this.RemoteVoidCall("ReleaseResults");
            }
        }

        private T RemoteCall<T>(string method, params string[] args) {
            
            // Create message
            Message msg = new Message() {
                CID = this.m_cidcntr++,
                Content = BrokerMarshal.JsonMarshal(new RemoteCallMessage() { Method = method, Arguments = args }),
                Mode = MessageMode.BrokerCall,
                Sender = this.m_connection.SelfID,
                Target = 0
            };

            // Send and await response
            if (this.m_connection.SendAndAwaitReply(msg) is ContentMessage response) {
                if (response.MessageType == ContentMessgeType.Error) {
                    throw new Exception(response.StrMsg);
                }
                if (response.StrMsg == "Primitive") {
                    Type t = Type.GetType(response.DotNetType);
                    return (T)(dynamic)Convert.ChangeType(response.Raw, t);
                } else {
                    if (BrokerMarshal.JsonUnmarshal<T>(response.Raw) is T settings) {
                        if (settings is IAPIObject apiobj) {
                            apiobj.API = this;
                        }
                        return settings;
                    }
                }
            }
            
            // TODO: Warning
            return default;

        }

        private void RemoteVoidCall(string method, params string[] args) {
            
            // Create message
            Message msg = new Message() {
                CID = this.m_cidcntr++,
                Content = BrokerMarshal.JsonMarshal(new RemoteCallMessage() { Method = method, Arguments = args }),
                Mode = MessageMode.BrokerCall,
                Sender = this.m_connection.SelfID,
                Target = 0
            };
            
            // Send but ignore response
            this.m_connection.SendMessage(msg);

        }

    }

}
