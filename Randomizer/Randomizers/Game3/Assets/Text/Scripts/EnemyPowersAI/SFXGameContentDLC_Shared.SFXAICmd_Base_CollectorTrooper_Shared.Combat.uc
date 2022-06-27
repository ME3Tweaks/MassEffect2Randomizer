auto state Combat 
{
    // State Functions
    
    // State code
Begin:
    Outer.FindDrivablePawn();
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
        if (Outer.CombatMood == EAICombatMood.AI_Aggressive && !Outer.IsTargetStealthed())
        {
            Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_Aggressive, Outer.FireTarget);
        }
        else
        {
            Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_WeaponRange, Outer.FireTarget);
        }
        if ((Outer.bReachedCover == FALSE && Outer.CombatMood == EAICombatMood.AI_Aggressive) && !Outer.IsTargetStealthed())
        {
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, Outer.WebSummonProximity);
            Outer.bAcquireNewCover = FALSE;
        }
        Outer.SetTimer(Outer.GetCoverDelayTime(), FALSE, 'FindNewCover', );
    }
    if (Outer.ShouldFireWeb())
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_CollectorTrooper_DeployWeb_Shared');
    }
    else if (Outer.bAcquireNewCover == FALSE || Outer.bReachedCover)
    {
        Outer.bAcquireNewCover = FALSE;
        Outer.bReachedCover = FALSE;
        if (Outer.ShouldUseGrenade())
        {
            Outer.Focus = Outer.FireTarget;
            SFXGame(Outer.WorldInfo.Game).LastEnemyGrenadeTimestamp = Outer.WorldInfo.GameTimeSeconds;
            if (Normal(Outer.FireTarget.location - Outer.MyBP.location) Dot Vector(Outer.MyBP.Rotation) > Outer.GrenadeConeAngle)
            {
                Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.GrenadeAttack, Outer.FireTarget, , TRUE);
                Outer.LastGrenadeTime = Outer.WorldInfo.GameTimeSeconds;
                Outer.Sleep(1.0);
            }
        }
        else if (ShouldAttack())
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