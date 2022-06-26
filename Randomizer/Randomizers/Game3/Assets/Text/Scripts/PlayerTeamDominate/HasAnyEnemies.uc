public function bool HasAnyEnemies()
{
    local int Index;
    local Pawn EnemyPawn;
    local SFXAI_Core EnemyAI;
    
    if (EnemyList.Length == 0)
    {
        return FALSE;
    }
    for (Index = 0; Index < EnemyList.Length; Index++)
    {
        EnemyPawn = EnemyList[Index].Pawn;
        if (EnemyPawn != None)
        {
            if (int(GetTeamNum()) != 0 && SFXPawn_Player(EnemyPawn) != None)
            {
                return TRUE;
            }
            EnemyAI = SFXAI_Core(EnemyPawn.Controller);
            if (EnemyAI != None && EnemyAI.bUnaware == FALSE)
            {
                return TRUE;
            }
        }
    }
    return FALSE;
}