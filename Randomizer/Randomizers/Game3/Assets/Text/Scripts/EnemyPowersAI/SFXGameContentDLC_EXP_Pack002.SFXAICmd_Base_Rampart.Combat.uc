auto state Combat 
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
    if (Outer.bAcquireNewCover)
    {
        if (SFXPawn(Outer.Pawn) != None)
        {
            SFXPawn(Outer.Pawn).PlayMoveToCoverSound();
        }
        if ((SFXPawn_Rampart(Outer.MyBP) != None && !SFXPawn_Rampart(Outer.MyBP).bAggressive) && !Outer.IsTargetStealthed())
        {
            Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_Aggressive, Outer.FireTarget);
        }
        else
        {
            Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_WeaponRange, Outer.FireTarget);
        }
        if (((Outer.bReachedCover == FALSE && SFXPawn_Rampart(Outer.MyBP) != None) && SFXPawn_Rampart(Outer.MyBP).bAggressive) && !Outer.IsTargetStealthed())
        {
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 100.0);
            Outer.bAcquireNewCover = FALSE;
        }
        Outer.SetTimer(Outer.GetCoverDelayTime(), FALSE, 'FindNewCover', );
    }
    if (CheckAggressiveTransition())
    {
        Outer.Attack();
        Outer.SetCombatMood(4);
    }
    if (Outer.bAcquireNewCover == FALSE || Outer.bReachedCover)
    {
        Outer.bAcquireNewCover = FALSE;
        Outer.bReachedCover = FALSE;
        if (ShouldAttack())
        {
            Outer.Attack();
            if (!Outer.bAcquireNewCover)
            {
                Outer.Sleep(Outer.GetActionDelayTime());
            }
        }
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};