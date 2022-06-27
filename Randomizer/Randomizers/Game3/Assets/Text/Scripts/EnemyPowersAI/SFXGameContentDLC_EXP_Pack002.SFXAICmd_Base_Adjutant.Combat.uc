auto state Combat extends InCombat 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.FireTarget == None)
    {
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    Outer.m_bClearVelocityAfterMove = FALSE;
    Class'SFXAICmd_MoveToMeleeRange2'.static.MoveToMeleeRange(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, FALSE);
    if (ShouldCharge(Outer.FireTarget))
    {
        Outer.LastChargeTime = Outer.WorldInfo.GameTimeSeconds;
        BioPawn(Outer.FireTarget).SetAbilityTimeStamp(Outer.ChargeAbilityName);
        Outer.Pawn.Acceleration *= float(0);
        Outer.Pawn.Velocity *= float(0);
        Outer.MyBP.StartCustomAction(int(GetChargeCA(Outer.FireTarget)));
    }
    else if (ShouldBlast(Outer.FireTarget))
    {
        Outer.LastFireTime = Outer.WorldInfo.GameTimeSeconds;
        BioPawn(Outer.FireTarget).SetAbilityTimeStamp(Outer.BlastAbilityName);
        Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
        Outer.FinishRotation();
        Outer.MyBP.StartCustomAction(169);
    }
    else
    {
        Outer.Attack();
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};