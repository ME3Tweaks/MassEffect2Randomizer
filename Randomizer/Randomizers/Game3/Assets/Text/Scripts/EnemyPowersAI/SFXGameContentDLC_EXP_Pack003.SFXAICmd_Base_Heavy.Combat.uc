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
    Outer.MoveTimeout = RandRange(Outer.MoveTimeoutInterval.X, Outer.MoveTimeoutInterval.Y);
    Outer.TotalMoveTime = 0.0;
    Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 100.0, FALSE, , TRUE);
    if (Outer.CanStopToFire())
    {
        SFXGRI(Outer.WorldInfo.GRI).TriggerVocalizationEvent(130, Outer.MyBP, None, , , TRUE);
        Outer.MyBP.StartCustomAction(206);
        while (Outer.bWantsToFire && NumFailedFire < MaxFailedFire)
        {
            Outer.Attack();
            Outer.Sleep(0.100000001);
            if (Outer.m_nWeaponCompletionReason != 1)
            {
                NumFailedFire++;
                Outer.Sleep(0.5);
            }
            else if (Outer.m_nWeaponCompletionReason == 1)
            {
                NumFailedFire = 0;
            }
            if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Medium) == FALSE)
            {
                Outer.bWantsToFire = FALSE;
            }
        }
    }
    if (CanSuppressTarget() == FALSE)
    {
        Outer.bWantsToFire = TRUE;
        MoveStartLocation = Outer.MyBP.location;
        Class'SFXAICmd_MoveToSuppressionPoint'.static.MoveToSuppressionPoint(Outer, Outer.FireTarget, 2, , , FALSE);
        if (!Outer.bReachedMoveGoal)
        {
            MoveStartLocation = Outer.MyBP.location;
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 500.0, FALSE);
        }
        Outer.Sleep(0.100000001);
    }
    Outer.Attack();
    NumFailedFire = 0;
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};