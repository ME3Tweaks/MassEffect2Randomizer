// Mixin: Turrets anywhere

public function NavigationPoint SelectTurretNav()
{
    local NavigationPoint TP;
    local NavigationPoint Closest;
    local float ClosestDist;
    local float Dist;
    local NavigationPoint TargetNav;
    local Pawn TargetPawn;
    local float DistToAnchor;
    local bool bFoundNodeWithLOS;
    local bool bClearLineBetweenNodes;
    
    if (ForcedTurretPoint != None)
    {
        return ForcedTurretPoint;
    }
    TargetPawn = Pawn(FireTarget);
    if (TargetPawn != None)
    {
        TargetNav = TargetPawn.Anchor;
        if (TargetNav == None)
        {
            TargetNav = TargetPawn.GetBestAnchor(TargetPawn, TargetPawn.location, FALSE, TRUE, DistToAnchor);
        }
    }
    foreach WorldInfo.RadiusNavigationPoints(Class'NavigationPoint', TP, MyBP.location, TurretPointSearchDist)
    {
        if (TP != None && UnreachableTurretPoints.Find(TP) == -1)
        {
            bClearLineBetweenNodes = FALSE;
            Dist = VSize(TP.location - MyBP.location);
            if (TargetNav != None)
            {
                if (VSizeSq(TP.location - TargetNav.location) < 250000.0)
                {
                    continue;
                }
                bClearLineBetweenNodes = HasApproximateSightBetweenNodes(TP, TargetNav);
            }
            if ((Closest == None || bClearLineBetweenNodes && !bFoundNodeWithLOS) || Dist < ClosestDist && bClearLineBetweenNodes == bFoundNodeWithLOS)
            {
                bFoundNodeWithLOS = bClearLineBetweenNodes || bFoundNodeWithLOS;
                Closest = TP;
                ClosestDist = Dist;
            }
        }
    }
    if (Closest != None)
    {
        return Closest;
    }
    return None;
}