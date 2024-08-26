
using System.Collections.Generic;

namespace ClangenReborn;

public enum ConditionSeverity : byte
{
    Minor, Major, Severe
}



public partial class Cat
{
    // TODO Rework death
    public bool Dead { get; private set; } = false;
}

