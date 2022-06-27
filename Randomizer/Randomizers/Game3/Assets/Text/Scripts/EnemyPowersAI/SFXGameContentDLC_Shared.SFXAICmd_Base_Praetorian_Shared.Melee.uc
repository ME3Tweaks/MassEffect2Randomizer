auto state Melee 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.IsFlying())
    {
        Outer.MyBP.StartCustomAction(213);
    }
    else if (Outer.IsInFiringMode())
    {
        Outer.MyBP.StartCustomAction(211);
    }
    if (Outer.FireTarget == None)
    {
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    Outer.Attack();
    if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Melee) == FALSE || IsProtectedByCover(Pawn(Outer.FireTarget)))
    {
        Outer.m_bClearVelocityAfterMove = FALSE;
        Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, FALSE, FALSE);
    }
    else if (Outer.Pawn.FastTrace(Outer.FireTarget.location, Outer.Pawn.location, Outer.Pawn.GetCollisionExtent() * 0.25, ) == FALSE)
    {
        Outer.m_bClearVelocityAfterMove = FALSE;
        Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, FALSE, FALSE);
    }
    else
    {
        Outer.bReachedMoveGoal = TRUE;
    }
    if (Outer.ShouldFireBlast())
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_Praetorian_Firing_Shared');
    }
    else if (Outer.ShouldStartFlying())
    {
        Outer.SetCombatMood(4);
    }
    else
    {
        if (CanLeap())
        {
            Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
            Outer.FinishRotation();
            Outer.MyBP.StartCustomAction(208);
            BioPawn(Outer.FireTarget).SetAbilityTimeStamp(Outer.LeapAbilityName);
        }
        bMeleed = FALSE;
        if (Outer.IsPraetorianInRange(Outer.EnemyDistance_Melee) && Outer.ShouldMelee(Outer.FireTarget))
        {
            Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
            Outer.FinishRotation();
            bMeleed = Outer.MyBP.StartCustomAction(FRand() > 0.5 ? 133 : 134);
        }
        Outer.LastMeleeTime = Outer.WorldInfo.GameTimeSeconds;
        if (((bMeleed && FRand() <= Outer.SyncKillChance) && Outer.ShouldSyncMelee(Outer.FireTarget)) && Outer.IsPraetorianInRange(Outer.EnemyDistance_Sync))
        {
            Outer.Sleep(Outer.PreSyncDelay);
            if (Outer.ShouldSyncMelee(Outer.FireTarget) && Outer.IsPraetorianInRange(Outer.EnemyDistance_Sync))
            {
                Outer.MyBP.StartCustomAction(135, BioPawn(Outer.FireTarget));
                Outer.LastSyncMeleeTime = Outer.WorldInfo.GameTimeSeconds;
            }
        }
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};