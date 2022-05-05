﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Battlegrounds.Networking.LobbySystem.json;

public class JsonLobbySlot : ILobbySlot {

    private ILobbyHandle? m_handle;

    public int SlotID { get; }
    public int TeamID { get; }
    public byte State { get; }

    [JsonConverter(typeof(JsonLobbyMemberConverter))]
    public ILobbyMember? Occupant { get; }

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Occupant))]
    public bool IsOccupied => this.State == 1;

    public ILobbyHandle Handle => this.m_handle ?? throw new InvalidOperationException();

    public JsonLobbySlot(int slotID, int teamID, byte state, ILobbyMember? Occupant) { 
        this.SlotID = slotID;
        this.TeamID = teamID;
        this.State = state;
        this.Occupant = Occupant;
    }

    public bool IsSelf() {
        if (this.m_handle is null) {
            return false;
        }
        if (this.Occupant is ILobbyMember mem) {
            return mem.MemberID == this.m_handle.Self.ID;
        }
        return false;
    }

    public bool IsAI() {
        if (this.Occupant is ILobbyMember mem) {
            return mem.Role == 3;
        }
        return false;
    }

    public void TrySetCompany(ILobbyCompany company) {
        if (this.IsOccupied) {
            //this.Occupant.Company = company;
        }
    }

    public void SetHandle(ILobbyHandle handle) {
        this.m_handle = handle;
        this.Occupant?.SetHandle(handle);
    }

}

public class JsonLobbySlotConverter : JsonConverter<ILobbySlot> {
    public override ILobbySlot? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<JsonLobbySlot>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, ILobbySlot value, JsonSerializerOptions options) {
        if (value is not JsonLobbySlot json)
            throw new InvalidOperationException();
        JsonSerializer.Serialize(writer, json, options);
    }
}

public class JsonLobbySlotArrayConverter : JsonConverter<ILobbySlot[]> {
    public override ILobbySlot[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<JsonLobbySlot[]>(ref reader, options);

    public override void Write(Utf8JsonWriter writer, ILobbySlot[] value, JsonSerializerOptions options) {
        if (value is not JsonLobbySlot[] json)
            throw new InvalidOperationException();
        JsonSerializer.Serialize(writer, json, options);
    }
}
