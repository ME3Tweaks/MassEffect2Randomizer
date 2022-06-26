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
        MoveStartLocation = Outer.MyBP.location;
        Class'SFXAICmd_MoveToSuppressionPoint'.static.MoveToSuppressionPoint(Outer, Outer.FireTarget, 1, 600.0, 3000.0, TRUE);
        if (!Outer.bReachedMoveGoal)
        {
            MoveStartLocation = Outer.MyBP.location;
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 500.0, TRUE);
        }
        Outer.Sleep(0.100000001);
    }
    if (Outer.ShouldSpawnTurret())
    {
        Outer.LastTurretSpawnTime = Outer.WorldInfo.GameTimeSeconds;
        TargetNav = Outer.SelectTurretNav();
        if (TargetNav != None)
        {
            OldFireTarget = Outer.FireTarget;
            Outer.FireTarget = TargetNav;
            Outer.SetDesiredRotation(Rotator(Outer.FireTarget.location - Outer.Pawn.location));
            Outer.FinishRotation();
            Outer.Sleep(0.5);
            SFXPawn_GethPrime(Outer.Pawn).PlayCastDroneVoc();
            Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.TurretPower, TargetNav, TargetNav.location, TRUE);
            Outer.FireTarget = OldFireTarget;
            OldFireTarget = None;
            TargetNav = None;
            Outer.MyBP.TriggerEventClass(Class'SFXSeqEvt_PrimeSpawnedPet', Outer.MyBP, 0);
            Outer.Sleep(0.200000003);
        }
    }
    else if (Outer.ShouldSpawnDrone())
    {
        SFXPawn_GethPrime(Outer.Pawn).PlayCastTurretVoc();
        Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.DronePower, None, , TRUE);
        Outer.LastDroneSpawnTime = Outer.WorldInfo.GameTimeSeconds;
        Outer.MyBP.TriggerEventClass(Class'SFXSeqEvt_PrimeSpawnedPet', Outer.MyBP, 1);
        Outer.Sleep(0.200000003);
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