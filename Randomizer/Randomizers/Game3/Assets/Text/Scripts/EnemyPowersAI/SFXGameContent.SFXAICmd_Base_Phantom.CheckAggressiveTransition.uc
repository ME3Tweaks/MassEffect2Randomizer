public final function bool CheckAggressiveTransition()
{
    local SFXAI_Phantom AI;
    local int NumAggressive;
    
    foreach Outer.WorldInfo.AllControllers(Class'SFXAI_Phantom', AI)
    {
        if (AI != None && AI.CombatMood == EAICombatMood.AI_Aggressive)
        {
            NumAggressive++;
        }
    }
    if (Outer.WorldInfo.GameTimeSeconds - Outer.LastAggressiveTime >= Outer.AggressiveFrequency && NumAggressive < SFXGRI(Outer.WorldInfo.GRI).NumPlayersInGame())
    {
        Outer.Attack();
        return TRUE;
    }
    return FALSE;
}