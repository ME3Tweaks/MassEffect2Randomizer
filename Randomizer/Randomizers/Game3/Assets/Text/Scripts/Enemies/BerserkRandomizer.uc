public final function BeginDefaultCommand(optional coerce string Reason, optional bool bForced)
{
    if (BerserkCommand != None && FRand() < %BERSERKCHANCE%)
    {
        BeginCombatCommand(BerserkCommand, Reason, bForced);
    }
    else if (CombatMood == EAICombatMood.AI_Aggressive && AggressiveCommand != None)
    {
        BeginCombatCommand(AggressiveCommand, Reason, bForced);
    }
    else if (CombatMood == EAICombatMood.AI_Fallback && FallbackCommand != None)
    {
        BeginCombatCommand(FallbackCommand, Reason, bForced);
    }
    else
    {
        BeginCombatCommand(DefaultCommand, Reason, bForced);
    }
}