public function Initialize()
{
    local SFXDifficultyHandler DH;
    local SFXShield_Base PawnShield;
    local float ShieldStrength;
    local ScaledFloat NewMaxShields;
    local float ShieldMultiplier;
    
    if (MyBP != None)
    {
        if (MyBP.Squad != None)
        {
            if (!MyBP.Squad.bSquadEnabled)
            {
                m_bInitiallyDisabled = TRUE;
            }
        }
        MoveTimerPenalty = 40.0 / MyBP.CombatGroundSpeed;
    }
    Super(BioAiController).Initialize();
    if (MyBP != None)
    {
        EvadeHealthThreshold = (MyBP.GetMaxHealth() + MyBP.GetMaxShields()) * RandRange(EvadeDamagePct.X, EvadeDamagePct.Y);
    }
    DH = SFXGRI(WorldInfo.GRI).DifficultyHandler;
    if (DH != None && SFXPawn(MyBP) != None)
    {
        SFXPawn(MyBP).AmmoDropPct = DH.GetFloat('AmmoDropPct', 'Global');
    }

    %COMBATSPEEDRANDOMIZER%
    
    %HEALTHRANDOMIZER%
}