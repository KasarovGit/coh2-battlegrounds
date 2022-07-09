﻿using System;

using Battlegrounds.Functional;
using Battlegrounds.Game;
using Battlegrounds.Networking.Communication.Golang;
using Battlegrounds.Networking.LobbySystem.json;
using Battlegrounds.Networking.Remoting;
using Battlegrounds.Util;

namespace Battlegrounds.Networking.LobbySystem;

/// <summary>
/// 
/// </summary>
public sealed class OnlineLobbyPlanner : ILobbyPlanningHandle {

    private readonly RemoteCall<ILobbyHandle> m_remote;

    /// <summary>
    /// 
    /// </summary>
    public ILobbyHandle Handle { get; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsDefender => this.Handle.AreTeamRolesSwapped() ? this.Team is 0 : this.Team is 1;

    /// <summary>
    /// 
    /// </summary>
    public bool IsAttacker => !this.IsDefender;

    /// <summary>
    /// 
    /// </summary>
    public int TeamSize => (this.Team switch {
        0 => this.Handle.Allies,
        1 => this.Handle.Axis,
        _ => this.Handle.Observers
    }).Slots.Filter(x => x.IsOccupied).Length;

    /// <summary>
    /// 
    /// </summary>
    public byte Team => this.Handle.GetSelfTeam();

    public event LobbyEventHandler<ILobbyPlanElement>? PlanElementAdded;
    public event LobbyEventHandler<int>? PlanElementRemoved;

    internal OnlineLobbyPlanner(ILobbyHandle handle, RemoteCall<ILobbyHandle> remote) {

        // Set handle
        this.Handle = handle;

        // Subscribe
        this.Handle.Subscribe("Notify.PlanElementAdd", this.OnElementAdd);
        this.Handle.Subscribe("Notify.PlanElementRemove", this.OnElementRemove);

        // Set connection
        this.m_remote = remote;

    }

    private void OnElementRemove(ContentMessage msg) {

        // Grab element id
        int elemId = msg.Raw.ConvertBigEndian(BitConverter.ToInt32);

        // Notify
        this.PlanElementRemoved?.Invoke(elemId);

    }

    private void OnElementAdd(ContentMessage msg) {

        // Try unmarshall
        if (GoMarshal.JsonUnmarshal<JsonPlanElement>(msg.Raw) is JsonPlanElement planElem) {
            
            // Set handles
            planElem.SetHandle(this.Handle);
            planElem.Remote = this.m_remote;

            // Invoke event
            this.PlanElementAdded?.Invoke(planElem);

        }

    }

    public void ClearPlan() {
        if (this.Handle.IsHost) {
            this.m_remote.Call("ClearPlan");
        }
    }

    public int CreatePlanningObjective(ulong owner, PlanningObjectiveType objectiveType, int objectiveOrder, GamePosition objectivePosition)
        => this.m_remote.Call<int>("CreatePlanningObjective", owner, (byte)objectiveOrder, objectiveOrder, objectivePosition);

    public int CreatePlanningSquad(ulong owner, string blueprint, ushort companyId, GamePosition spawn, GamePosition? lookat = null) => lookat switch {
        GamePosition look => this.m_remote.Call<int>("CreatePlanningSquadLookat", owner, blueprint, companyId, spawn, look),
        _ => this.m_remote.Call<int>("CreatePlanningSquad", owner, blueprint, companyId, spawn)
    };

    public int CreatePlanningStructure(ulong owner, string blueprint, bool directional, GamePosition origin, GamePosition? lookat = null) => lookat switch {
        GamePosition look => this.m_remote.Call<int>("CreatePlanningStructureLookat", owner, blueprint, directional, origin, look),
        _ => this.m_remote.Call<int>("CreatePlanningStructure", owner, blueprint, directional, origin)
    };

    public ILobbyPlanElement? GetPlanElement(int planElementId) => this.m_remote.Call<ILobbyPlanElement>("GetPlanElement", planElementId);

    public ILobbyPlanElement[] GetPlanningElements(byte teamIndex)
        => this.m_remote.Call<ILobbyPlanElement[]>("GetPlanElements", teamIndex) ?? Array.Empty<ILobbyPlanElement>();

    public void RemovePlanElement(int planElementId)
        => this.m_remote.Call("RemovePlanElement", planElementId);

}