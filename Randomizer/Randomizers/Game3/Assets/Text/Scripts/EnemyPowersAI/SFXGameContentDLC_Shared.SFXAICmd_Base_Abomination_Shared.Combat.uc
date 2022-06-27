auto state Combat extends InCombat 
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
    if (Outer.FireTarget != None)
    {
        Outer.Focus = Outer.FireTarget;
    }
    Outer.Attack();
    if (Outer.InRange(Outer.FireTarget.location, Outer.EnemyDistance_Melee) == FALSE)
    {
        Outer.m_bClearVelocityAfterMove = FALSE;
        Class'SFXAICmd_MoveToMeleeRange'.static.MoveToMeleeRange(Outer, Outer.FireTarget, 150.0, FALSE);
        Outer.Sleep(0.5);
    }
    Outer.Sleep(0.100000001);
    goto 'Begin';
    stop;
};