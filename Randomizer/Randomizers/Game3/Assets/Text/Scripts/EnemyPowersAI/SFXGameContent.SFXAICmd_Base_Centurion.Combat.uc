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
        if (Outer.ShouldSmokeTarget())
        {
            Class'SFXAICmd_Centurion_SmokeTarget'.static.SmokeBetweenUs(Outer, Outer.FireTarget);
            Outer.LastSmokeTime = Outer.WorldInfo.GameTimeSeconds;
        }
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
        if (Outer.ShouldUseGrenade())
        {
            SFXGame(Outer.WorldInfo.Game).LastEnemyGrenadeTimestamp = Outer.WorldInfo.GameTimeSeconds;
            Outer.Focus = Outer.FireTarget;
            if (Normal(Outer.FireTarget.location - Outer.MyBP.location) Dot Vector(Outer.MyBP.Rotation) > Outer.GrenadeConeAngle)
            {
                Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.GrenadeAttack, Outer.FireTarget, , TRUE);
                Outer.LastGrenadeTime = Outer.WorldInfo.GameTimeSeconds;
                Outer.Sleep(1.0);
            }
        }
        if (ShouldAttack())
        {
            Outer.Attack();
            Outer.Sleep(3.0 + FRand() * 7.0);
        }
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};