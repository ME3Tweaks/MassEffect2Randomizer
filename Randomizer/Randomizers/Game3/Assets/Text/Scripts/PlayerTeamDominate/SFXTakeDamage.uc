public simulated function SFXTakeDamage(float Damage, out TraceHitInfo HitInfo, out Vector HitLocation, Vector Momentum, Class<DamageType> DamageType, Controller instigatedBy, optional Actor DamageCauser)
{
    local Pawn DamageCauserPawn;
    
    DamageCauserPawn = Class'BioPawn'.static.FindAttackingPawn(instigatedBy, DamageCauser);
    if (SFXPawn_Henchman(DamageCauserPawn) != None && Pawn(ModuleOwner) != None)
    {
        if (!DamageCauserPawn.IsHostile(Pawn(ModuleOwner)))
        {
            return;
        }
        else
        {
            Damage *= 0.5;
        }
    }
    Super.SFXTakeDamage(Damage, HitInfo, HitLocation, Momentum, DamageType, instigatedBy, DamageCauser);
}