public static function int GetPhysicsLevel(Actor oImpacted, optional bool bIgnoreResistance = FALSE)
{
    local SFXGameConfig oConfig;
    local BioPawn oPawn;
    
    oPawn = BioPawn(oImpacted);
    if (oPawn != None && oPawn.ActorType != None)
    {
        // Disable human/hench check
        //if (oPawn.IsHumanControlled() || SFXPawn_Henchman(oPawn) != None)
        //{
        //    return 5;
        //}
        oConfig = SFXGame(oPawn.WorldInfo.Game).gameconfig;
        if (((oConfig != None && bIgnoreResistance == FALSE) && oConfig.bShieldsBlockPowers) && oPawn.GetCurrentShields() > float(0))
        {
            return 5;
        }
        return BioActorType(oImpacted.ActorType).m_nPhysicsLevel;
    }
    return 0;
}