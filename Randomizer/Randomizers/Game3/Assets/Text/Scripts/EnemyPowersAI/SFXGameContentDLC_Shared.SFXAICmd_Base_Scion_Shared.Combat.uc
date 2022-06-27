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
    if (CanSuppressTarget() == FALSE)
    {
        if (IsWeaponRaised())
        {
            SFXPawn_Scion_Shared(Outer.Pawn).bAiming = FALSE;
        }
        MoveStartLocation = Outer.MyBP.location;
        Class'SFXAICmd_MoveToSuppressionPoint'.static.MoveToSuppressionPoint(Outer, Outer.FireTarget, 2, 1000.0, 3000.0, FALSE);
        if (!Outer.bReachedMoveGoal)
        {
            MoveStartLocation = Outer.MyBP.location;
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 500.0, FALSE);
        }
        Outer.Sleep(0.100000001);
    }
    if (IsWeaponRaised() == FALSE)
    {
        SFXPawn_Scion_Shared(Outer.Pawn).bAiming = TRUE;
        Outer.Sleep(1.5);
    }
    if (Outer.ShouldBlast())
    {
        Outer.Focus = Outer.FireTarget;
        Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
        Outer.FinishRotation();
        Outer.MyBP.StartCustomAction(206);
        Outer.LastFireTime = Outer.WorldInfo.GameTimeSeconds;
        Outer.LastBlastTime = Outer.WorldInfo.GameTimeSeconds;
    }
    else
    {
        Outer.Attack();
        if (Outer.m_nWeaponCompletionReason == 1)
        {
            Outer.Sleep(Outer.GetFireDelayTime());
        }
    }
    Outer.Sleep(0.200000003);
    goto 'Begin';
    stop;
};