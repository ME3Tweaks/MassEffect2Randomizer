Class SFXAICmd_Base_GethRocketTrooper extends SFXAICmd_Base_Cover within SFXAI_GethRocketTrooper;

// States
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
            Class'SFXAICmd_MoveToGoal'.static.MoveToGoal(Outer, Outer.FireTarget, 100.0);
            Outer.bAcquireNewCover = FALSE;
        }
        Outer.SetTimer(Outer.GetCoverDelayTime(), FALSE, 'FindNewCover', );
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

//class default properties can be edited in the Properties tab for the class's Default__ object.
defaultproperties
{
}