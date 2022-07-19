// Mixin: Allow Enemy Weapon Penetration
public simulated function ImpactInfo CalcWeaponFire(Vector StartTrace, Vector EndTrace, optional out array<ImpactInfo> ImpactList, optional Vector Extent)
{
    local SFXPlayerCamera Camera;
    local Vector ToPC;
    local Vector CamRot;
    local float PenetrationMax;
    
    DrawDebugShot(StartTrace, EndTrace);
    if ((Instigator != None && Instigator.IsHumanControlled()) && Instigator.Controller != None)
    {
        Camera = SFXPlayerCamera(PlayerController(Instigator.Controller).PlayerCamera);
        if (Camera != None)
        {
            ToPC = Instigator.location - StartTrace;
            CamRot = Vector(Camera.CameraCache.POV.Rotation);
            StartTrace += CamRot * (ToPC Dot CamRot);
        }
    }
    if (Instigator != None && Instigator.GetTeam().TeamIndex >= 0)
    {
        PenetrationMax = DistancePenetrated + (PenetrationBonus.Value - 1.0) * 100.0;
    }
    else
    {
        PenetrationMax = 0.0;
    }
    return CalcWeaponFire_Native(StartTrace, EndTrace, ImpactList, PenetrationMax, Extent);
}