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
        Class'SFXAICmd_AcquireCover'.static.AcquireNewCover(Outer, Outer.AtCover_WeaponRange, Outer.FireTarget, , FALSE);
        Outer.SetTimer(Outer.GetCoverDelayTime(), FALSE, 'FindNewCover', );
    }
    if (Outer.bAcquireNewCover == FALSE || Outer.bReachedCover)
    {
        Outer.bAcquireNewCover = FALSE;
        Outer.bReachedCover = FALSE;
        if (ShouldAttack())
        {
            SFXGRI(Outer.WorldInfo.GRI).TriggerVocalizationEvent(130, Outer.MyBP, None, , , TRUE);
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