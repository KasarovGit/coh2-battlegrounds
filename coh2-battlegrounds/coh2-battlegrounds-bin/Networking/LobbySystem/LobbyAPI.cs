﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Battlegrounds.ErrorHandling.Networking;
using Battlegrounds.Functional;
using Battlegrounds.Networking.Communication.Golang;
using Battlegrounds.Networking.Communication.Connections;
using Battlegrounds.Networking.Server;
using Battlegrounds.Steam;

using Battlegrounds.ErrorHandling.CommonExceptions;
using System.Text;

using static Battlegrounds.Networking.LobbySystem.LobbyAPIStructs;

namespace Battlegrounds.Networking.LobbySystem;

/// <summary>
/// 
/// </summary>
public delegate void LobbyMatchEventHandler();

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="args"></param>
public delegate void LobbyMatchEventHandler<T>(T args);

/// <summary>
/// 
/// </summary>
/// <param name="CancelID"></param>
/// <param name="CancelName"></param>
public record LobbyMatchStartupCancelledEventArgs(ulong CancelID, string CancelName);

/// <summary>
/// Class for interacting with the lobby logic on the server.
/// </summary>
public sealed class LobbyAPI {

    private static readonly TimeSpan __cacheTime = TimeSpan.FromSeconds(3.5);
    private static readonly TimeZoneInfo __thisTimezone = TimeZoneInfo.Local;

    private readonly ServerConnection m_connection;
    private readonly bool m_isHost;
    private volatile uint m_cidcntr;

    private LobbyTeam m_allies;
    private LobbyTeam m_axis;
    private LobbyTeam m_obs;
    private readonly Dictionary<string, string> m_settings;

    public string Title { get; }

    public bool IsHost => this.m_isHost;

    public LobbyTeam Allies => this.m_allies;

    public LobbyTeam Axis => this.m_axis;

    public LobbyTeam Observers => this.m_obs;

    public Dictionary<string, string> Settings => this.m_settings;

    public ServerAPI ServerHandle { get; }

    public SteamUser Self { get; }

    public event Action<LobbyMessage>? OnChatMessage;

    public event Action<ulong, string, string>? OnSystemMessage;

    public event Action? OnLobbySelfUpdate;

    public event Action<LobbyTeam>? OnLobbyTeamUpdate;

    public event LobbyMatchEventHandler<LobbySlot>? OnLobbySlotUpdate;

    public event Action<int, int, LobbyCompany>? OnLobbyCompanyUpdate;

    public event Action<string, string>? OnLobbySettingUpdate;

    public event Action<string>? OnLobbyConnectionLost;

    public event LobbyMatchEventHandler<LobbyMatchStartupCancelledEventArgs>? OnLobbyCancelStartup;

    public event Action? OnLobbyBeginMatch;

    public event Action? OnLobbyLaunchGame;

    public event Action<ServerAPI>? OnLobbyRequestCompany;

    public event Action<ServerAPI>? OnLobbyNotifyGamemode;

    public event Action<ServerAPI>? OnLobbyNotifyResults;

    private Action? OnLobbyCancelReceived;

    public LobbyAPI(bool isHost, string title, SteamUser self, ServerConnection connection, ServerAPI serverAPI) {

        // Store ref to server handle
        this.ServerHandle = serverAPI;

        // Set internal refs
        this.m_connection = connection;
        this.m_isHost = isHost;
        this.m_cidcntr = 100;

        // Store self (id)
        this.Self = self;

        // Store title
        this.Title = title;

        // Add private hook
        this.m_connection.MessageReceived = this.OnMessage;

        // Create get lobby message
        Message msg = new Message() {
            CID = this.m_cidcntr++,
            Content = GoMarshal.JsonMarshal(new RemoteCallMessage() { Method = "GetLobby", Arguments = Array.Empty<string>() }),
            Mode = MessageMode.BrokerCall,
            Sender = this.m_connection.SelfID,
            Target = 0
        };

        // Make pull lobby request
        ContentMessage? reply = this.m_connection.SendAndAwaitReply(msg);
        if (reply is ContentMessage response && GoMarshal.JsonUnmarshal<LobbyRemote>(response.Raw) is LobbyRemote remoteLobby) {

            // Make sure teams are valid
            if (remoteLobby.Teams is null || remoteLobby.Teams.Any(x => x is null)) {
                throw new Exception("Received **NULL** team data.");
            }

            // Set API on team objects
            remoteLobby.Teams.ForEach(x => x.SetAPI(this));

            // Create team data
            this.m_allies = remoteLobby.Teams[0];
            this.m_axis = remoteLobby.Teams[1];
            this.m_obs = remoteLobby.Teams[2];
            this.m_settings = remoteLobby.Settings;

            // Register connection lost
            this.m_connection.OnConnectionLost += _ => {
                this.OnLobbyConnectionLost?.Invoke("LOST");
            };

        } else {

            // Throw exception -> Failed to fully connect.
            throw new Exception("Failed to retrieve light-lobby version.");

        }

    }

    private LobbyTeam? GetTeamInstanceByIndex(int tid) => tid switch {
        0 => this.m_allies,
        1 => this.m_axis,
        2 => this.m_obs,
        _ => throw new IndexOutOfRangeException($"Team ID '{tid}' is out of bounds.")
    };

    private void OnMessage(uint cid, ulong sender, ContentMessage message) {

        // Check if error message and display it
        if (message.MessageType is ContentMessgeType.Error) {
            Trace.WriteLine($"Received Error = {message.StrMsg} to CID({cid})", nameof(LobbyAPI));
            return;
        }

        // If disconnect, handle
        if (message.MessageType is ContentMessgeType.Disconnect) {
            if (message.Who == this.m_connection.SelfID) {
                this.OnLobbyConnectionLost?.Invoke(message.Kick ? "KICK" : "CLOSED");
            } else {
                this.OnSystemMessage?.Invoke(message.Who, message.StrMsg, message.Kick ? "KICK" : "LEFT");
            }
            return;
        }

        // If join, handle
        if (message.MessageType is ContentMessgeType.Join) {
            this.OnSystemMessage?.Invoke(message.Who, message.StrMsg, "JOIN");
            return;
        }

        // Switch on strmsg
        switch (message.StrMsg) {
            case "Message":

                if (GoMarshal.JsonUnmarshal<LobbyMessage>(message.Raw) is LobbyMessage lobbyMessage) {

                    // Get serverzone
                    var serverZone = TimeZoneInfo.FindSystemTimeZoneById(lobbyMessage.Timezone);

                    // Create timestamp
                    var datetime = FromTimestamp(lobbyMessage.Timestamp) + (__thisTimezone.BaseUtcOffset - serverZone.BaseUtcOffset);
                    lobbyMessage.Timestamp = $"{datetime.Hour}:{datetime.Minute}";

                    // Trigger event
                    this.OnChatMessage?.Invoke(lobbyMessage);

                }

                break;
            case "Notify.Company":

                // Get call
                var companyCall = GoMarshal.JsonUnmarshal<RemoteCallMessage>(message.Raw);

                // Get args
                bool parsed = int.TryParse(companyCall.Arguments[0], out int tid);
                parsed &= int.TryParse(companyCall.Arguments[1], out int sid);
                if (!parsed) {
                    Trace.WriteLine($"Failed to parse '{companyCall.Arguments[0]}' or '{companyCall.Arguments[1]}' as integers", nameof(LobbyAPI));
                    return;
                }

                // Get strength
                if (!float.TryParse(companyCall.Arguments[6], out float strength)) {
                    Trace.WriteLine($"Failed to parse '{companyCall.Arguments[6]}' as float32", nameof(LobbyAPI));
                    return;
                }

                // Create company
                var company = new LobbyCompany() {
                    API = this,
                    IsAuto = companyCall.Arguments[2] == "1",
                    IsNone = companyCall.Arguments[3] == "1",
                    Name = companyCall.Arguments[4],
                    Army = companyCall.Arguments[5],
                    Strength = strength,
                    Specialisation = companyCall.Arguments[7]
                };

                // Set company API
                company.SetAPI(this);

                // Invoke handler
                this.OnLobbyCompanyUpdate?.Invoke(tid, sid, company);

                break;
            case "Notify.Team":

                // Try get team
                if (GoMarshal.JsonUnmarshal<LobbyTeam>(message.Raw) is LobbyTeam newTeam) {

                    // Set API
                    newTeam.SetAPI(this);

                    // Refresh self teams
                    if (newTeam.TeamID == 0) {
                        this.m_allies = newTeam;
                    } else if (newTeam.TeamID == 1) {
                        this.m_axis = newTeam;
                    } else {
                        this.m_obs = newTeam;
                    }

                    // Trigger team update
                    this.OnLobbyTeamUpdate?.Invoke(newTeam);

                }

                break;
            case "Notify.Slot":

                // Try get slot
                if (GoMarshal.JsonUnmarshal<LobbySlot>(message.Raw) is LobbySlot newSlot) {

                    // Set API
                    newSlot.SetAPI(this);

                    // Trigger slot update
                    this.OnLobbySlotUpdate?.Invoke(newSlot);

                }

                break;
            case "Notify.Setting":

                // Get call
                var settingCall = GoMarshal.JsonUnmarshal<RemoteCallMessage>(message.Raw);

                // Decode
                var (settingKey, settingValue) = settingCall.Decode<string, string>();

                // Notify
                this.OnLobbySettingUpdate?.Invoke(settingKey, settingValue);

                break;
            case "Notify.Start":
                this.OnLobbyBeginMatch?.Invoke();
                break;
            case "Notify.Cancel":
                this.OnLobbyCancelReceived?.Invoke();
                this.OnLobbyCancelStartup?.Invoke(new(sender, Encoding.UTF8.GetString(message.Raw)));
                break;
            case "Notify.Launch":
                this.OnLobbyLaunchGame?.Invoke();
                break;
            case "Request.Company":
                this.OnLobbyRequestCompany?.Invoke(this.ServerHandle);
                break;
            case "Notify.Gamemode":
                this.OnLobbyNotifyGamemode?.Invoke(this.ServerHandle);
                break;
            case "Notify.Results":
                this.OnLobbyNotifyResults?.Invoke(this.ServerHandle);
                break;
            default:
                Trace.WriteLine($"Unsupported API event: {message.StrMsg}", nameof(LobbyAPI));
                break;
        }

    }

    private static DateTime FromTimestamp(string stamp) {
        string[] s = stamp.Split(':');
        int h = int.Parse(s[0]);
        int m = int.Parse(s[1]);
        return DateTime.Today.AddHours(h).AddMinutes(m);
    }

    public void Disconnect() {
        this.m_connection.Shutdown();
    }

    public LobbyTeam GetTeam(int tid, bool RefreshInternalReference = true) {

        // Assert range
        if (tid is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(tid), tid, $"Team ID must be in range 0 <= tid <= 2");

        // Get team
        var team = this.RemoteCall<LobbyTeam>("GetTeam", tid);
        if (team is null) {
            throw new ObjectNotFoundException("Failed to find lobby team instance.");
        }
        
        // Invoke team update
        this.OnLobbyTeamUpdate?.Invoke(team);

        // Update private field
        if (RefreshInternalReference) {
            switch (tid) {
                case 0:
                    this.m_allies = team;
                    break;
                case 1:
                    this.m_axis = team;
                    break;
                case 2:
                    this.m_obs = team;
                    break;
                default:
                    break;
            }
        }

        // Return team
        return team;

    }

    public LobbyMember GetLobbyMember(ulong mid)
        => this.RemoteCall<LobbyMember>("GetLobbyMember", mid) ?? throw new ObjectNotFoundException("Failed to get valid lobby member instance.");

    public LobbyCompany GetCompany(ulong mid)
        => this.RemoteCall<LobbyCompany>("GetCompany", mid) ?? throw new ObjectNotFoundException("Failed to get valid company instance.");

    public Dictionary<string, string> GetSettings()
        => this.RemoteCall<Dictionary<string, string>>("GetSettings") ?? new Dictionary<string, string>();

    public uint GetPlayerCount(bool humansOnly = false)
        => this.RemoteCall<uint>("GetPlayerCount", EncBool(humansOnly));

    private static string EncBool(bool b) => b ? "1" : "0";

    public void SetCompany(int tid, int sid, LobbyCompany company) {

        // Convert to str
        string strength = company.Strength.ToString(CultureInfo.InvariantCulture);
        string auto = EncBool(company.IsAuto);
        string none = EncBool(company.IsNone);

        // Invoke remotely
        this.RemoteVoidCall("SetCompany", tid, sid, auto, none, company.Name, company.Army, strength, company.Specialisation);

        // Trigger self update
        this.OnLobbyCompanyUpdate?.Invoke(tid, sid, company);

    }

    public void MoveSlot(ulong mid, int tid, int sid)
        => this.RemoteVoidCall("MoveSlot", mid, tid, sid);

    public void AddAI(int tid, int sid, int difficulty, LobbyCompany company) {
    
        // Make sure we can
        if (!this.m_isHost) {
            throw new InvokePermissionAccessDeniedException("Cannot invoke remote method that requires host-privellige");
        }

        // Get invariant strength
        var inv = company.Strength.ToString(CultureInfo.InvariantCulture);

        // Call AI
        RemoteVoidCall("AddAI", tid, sid, difficulty, EncBool(company.IsAuto), EncBool(company.IsNone), company.Name, company.Army, inv, company.Specialisation);

    }

    public void RemoveOccupant(int tid, int sid) {
        
        // Trigger remote call
        this.RemoteVoidCall("RemoveOccupant", tid, sid);

    }

    public void LockSlot(int tid, int sid)
        => this.RemoteVoidCall("LockSlot", tid, sid);

    public void UnlockSlot(int tid, int sid)
        => this.RemoteVoidCall("UnlockSlot", tid, sid);

    public void GlobalChat(ulong mid, string msg)
        => this.RemoteVoidCall("GlobalChat", mid, msg);

    public void TeamChat(ulong mid, string msg)
        => this.RemoteVoidCall("TeamChat", mid, msg);

    public void SetLobbySetting(string setting, string value) {
        if (this.m_isHost) {
            this.RemoteVoidCall("SetLobbySetting", setting, value);
        }
    }

    public void SetLobbyState(LobbyState state) {
        if (this.m_isHost) {
            this.RemoteVoidCall("SetLobbyState", state switch {
                LobbyState.Playing => "SERVERSTATUS_PLAYING",
                LobbyState.InLobby => "SERVERSTATUS_IN_LOBBY",
                LobbyState.Starting => "SERVERSTATUS_STARTING",
                LobbyState.None => "SERVERSTATUS_NO_STATUS",
                _ => throw new ArgumentException($"Invalid lobby state value 'LobbyState.{state}'", nameof(state))
            });
        }
    }

    public bool SetTeamsCapacity(int newCapacity) {
        if (this.m_isHost) {                
            return this.RemoteCall<bool>("SetTeamsCapacity", newCapacity);
        }
        return true;
    }

    public bool StartMatch(double cancelTime) {

        // Send start match command
        this.RemoteVoidCall("StartMatch");

        // Flag marking whether the match was cancelled by others
        bool wasCancelled = false;

        // Set cancel listener
        this.OnLobbyCancelReceived = () => wasCancelled = true;

        // TODO: Wait for remote input (5s)

        // Return false
        return !wasCancelled;

    }

    public void CancelMatch() {

        // Send cancel match command
        this.RemoteVoidCall("CancelStartMatch");

    }

    public void LaunchMatch() {
        if (this.m_isHost) {
            this.RemoteVoidCall("LaunchGame");
        }
    }

    public void RequestCompanyFile(params ulong[] members) {
        if (members.Length == 0) {
            members = this.Allies.Slots.Concat(this.Axis.Slots).Filter(x => x.IsOccupied && !x.IsSelf()).Map(x => x.Occupant?.MemberID ?? 0);
        }
        for (int i = 0; i < members.Length; i++) {
            this.RemoteVoidCall("GetCompanyFile", members[i]);
        }
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

    private static T BitConvert<T>(byte[] raw, Func<byte[], int, T> func) {
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(raw);
        }
        return func(raw, 0);
    }

    private T? RemoteCall<T>(string method, params object[] args) {
        
        // Create message
        Message msg = new Message() {
            CID = this.m_cidcntr++,
            Content = GoMarshal.JsonMarshal(new RemoteCallMessage() { Method = method, Arguments = args.Map(x => x.ToString()).NotNull() }),
            Mode = MessageMode.BrokerCall,
            Sender = this.m_connection.SelfID,
            Target = 0
        };

        // Send and await response
        if (this.m_connection.SendAndAwaitReply(msg) is ContentMessage response) {
            if (response.MessageType == ContentMessgeType.Error) {
                string e = response.StrMsg is null ? "Received error message from server with no description" : response.StrMsg;
                throw new Exception(e);
            }
            if (response.StrMsg == "Primitive") {
                // It feels nasty using 'dynamic'
                return (T)(dynamic)(response.DotNetType switch {
                    nameof(Boolean) => response.Raw[0] == 1,
                    nameof(UInt32) => BitConvert(response.Raw, BitConverter.ToUInt32),
                    _ => throw new NotImplementedException($"Support for primitive response of type '{response.DotNetType}' not implemented.")
                });
            } else {
                if (GoMarshal.JsonUnmarshal<T>(response.Raw) is T settings) {
                    if (settings is IAPIObject apiobj) {
                        apiobj.SetAPI(this);
                    }
                    return settings;
                }
            }
        }
        
        // TODO: Warning
        return default;

    }

    private void RemoteVoidCall(string method, params object[] args) {
        
        // Create message
        Message msg = new Message() {
            CID = this.m_cidcntr++,
            Content = GoMarshal.JsonMarshal(new RemoteCallMessage() { Method = method, Arguments = args.Map(x => x.ToString()).NotNull() }),
            Mode = MessageMode.BrokerCall,
            Sender = this.m_connection.SelfID,
            Target = 0
        };
        
        // Send but ignore response
        this.m_connection.SendMessage(msg);

    }

}