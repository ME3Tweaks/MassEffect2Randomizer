// Mixin: Repair all pawns

public final function UpdateRepairTargetsAndTurretNodes()
{
    local SFXPawn P;
    local int idx;
    local Actor A;
    
    foreach WorldInfo.AllPawns(Class'SFXPawn', P)
    {
        if (P != None && P != Pawn)
        {
            if (RepairTargets.Find(P) == -1)
            {
                RepairTargets.AddItem(P);
            }
        }
    }
    for (idx = RepairTargets.Length - 1; idx >= 0; idx--)
    {
        A = RepairTargets[idx];
        if (IsValidRepairTarget(A) == FALSE)
        {
            RepairTargets.Remove(idx, 1);
        }
    }
    for (idx = UnreachableTurretPoints.Length - 1; idx >= 0; idx--)
    {
        if (UnreachableTurretPoints[idx] == None)
        {
            UnreachableTurretPoints.Remove(idx, 1);
        }
    }
}