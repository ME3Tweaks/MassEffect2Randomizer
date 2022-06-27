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
                SFXGRI(Outer.WorldInfo.GRI).TriggerVocalizationEvent(130, Outer.MyBP, None, , , TRUE);
                Class'SFXAICmd_UsePower'.static.UsePower(Outer, Outer.GrenadeAttack, Outer.FireTarget, , TRUE);
                Outer.LastGrenadeTime = Outer.WorldInfo.GameTimeSeconds;
                Outer.Sleep(1.0);
            }
        }
        else if (Outer.ShouldSpawnDrone())
        {
            Outer.MyBP.StartCustomAction(205);
            SFXGRI(Outer.WorldInfo.GRI).TriggerVocalizationEvent(131, Outer.MyBP, None, , , TRUE);
            Outer.Sleep(0.200000003);
        }
        else if (ShouldAttack())
        {
            Outer.Attack();
            Outer.Sleep(3.0 + FRand() * 7.0);
        }
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};