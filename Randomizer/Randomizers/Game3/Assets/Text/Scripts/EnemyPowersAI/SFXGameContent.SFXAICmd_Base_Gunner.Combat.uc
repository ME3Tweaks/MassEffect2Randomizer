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
    if (Outer.ShouldRepair())
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_Gunner_Repair');
    }
    else if (Outer.ShouldDeployTurret())
    {
        Outer.BeginCombatCommand(Class'SFXAICmd_Gunner_DeployTurret');
    }
    if (Outer.bAcquireNewCover)
    {
        Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_WeaponRange, Outer.FireTarget);
        if (Outer.bReachedCover)
        {
            Outer.SetTimer(Outer.GetCoverDelayTime(), FALSE, 'FindNewCover', );
        }
    }
    if (Outer.bAcquireNewCover == FALSE || Outer.bReachedCover)
    {
        Outer.bAcquireNewCover = FALSE;
        Outer.bReachedCover = FALSE;
        if (ShouldAttack())
        {
            Outer.Attack();
            Outer.Sleep(5.0 + FRand() * 7.0);
        }
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};