﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Battlegrounds.Game.Gameplay;

namespace Battlegrounds.Game.Match.Analyze;

public class NullAnalysis : IAnalyzedMatch {

    public bool IsFinalizableMatch => false;

    public IReadOnlyList<UnitStatus> Units => new ReadOnlyCollection<UnitStatus>(Array.Empty<UnitStatus>());

    public IReadOnlyList<ItemStatus> Items => new ReadOnlyCollection<ItemStatus>(Array.Empty<ItemStatus>());

    public IReadOnlyCollection<Player> Players => new ReadOnlyCollection<Player>(Array.Empty<Player>());

    public ISession Session => new NullSession();

    public TimeSpan Length => TimeSpan.MaxValue;

    public bool CompileResults() => true;

    public bool IsWinner(Player player) => false;

}
