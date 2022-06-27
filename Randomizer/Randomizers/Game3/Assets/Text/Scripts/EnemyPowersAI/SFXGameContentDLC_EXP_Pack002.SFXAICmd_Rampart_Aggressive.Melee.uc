auto state Melee 
{
    // State Functions
    
    // State code
Begin:
    if (Outer.FireTarget == None)
    {
        Outer.AILog_Internal("Lost enemy.  Re-acquiring target", 'Combat');
        while (Outer.SelectTarget() == FALSE)
        {
            Outer.Sleep(1.0);
        }
    }
    Outer.Attack();
    if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Melee) == FALSE || IsProtectedByCover(Pawn(Outer.FireTarget)))
    {
        Outer.AILog_Internal("Enemy not in range or can't melee effectively (target protected by cover)", 'Combat');
        SetupTimeout();
        TargetInitialLoc = Outer.FireTarget.location;
        Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, , FALSE);
    }
    else if (Outer.Pawn.FastTrace(Outer.FireTarget.location, Outer.Pawn.location, , ) == FALSE)
    {
        Outer.AILog_Internal("No LOS to enemy, moving...", 'Combat');
        TargetInitialLoc = Outer.FireTarget.location;
        Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.MeleeMoveOffset, , FALSE);
    }
    else
    {
        Outer.bReachedMoveGoal = TRUE;
    }
    if (Outer.bReachedMoveGoal)
    {
        if (Outer.IsRampartInRange(Outer.EnemyDistance_Melee) && Outer.ShouldMelee(Outer.FireTarget))
        {
            Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
            Outer.FinishRotation();
            if (SFXPawn(Outer.FireTarget) != None && SFXPawn(Outer.FireTarget).IsInvisible())
            {
                Outer.Focus = Outer.FireTarget;
            }
            Outer.MyBP.StartCustomAction(133);
            Outer.LastMeleeTime = Outer.WorldInfo.GameTimeSeconds;
        }
    }
    else
    {
        Outer.AILog_Internal("Failed to find path to attack enemy");
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};