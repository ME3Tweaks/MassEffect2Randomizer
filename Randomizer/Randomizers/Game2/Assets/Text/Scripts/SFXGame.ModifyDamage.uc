// Friendly Fire (removes friendly check)
public function ModifyDamage(out float Damage, out TraceHitInfo HitInfo, out Vector HitLocation, out Vector Momentum, Class<SFXDamageType> DamageType, Actor injured, Controller InstigatedBy, optional Actor DamageCauser)
{
    local BioWorldInfo Info;
    local BioPlayerController PC;
    local Pawn TargetPawn;
    local BioPawn TargetBP;
    local SFXPRI InstigatorPRI;
    local SFXModule_GameEffectManager Manager;
    local RB_BodySetup HitBody;
    local SFXModule_Damage DamageMod;
    local float Multiplier;
    
    Multiplier = 1.0;
    if (SFXWeapon(DamageCauser) != None)
    {
        SFXWeapon(DamageCauser).ModifyDamage(Multiplier, HitLocation, injured);
    }
    if (InstigatedBy != None)
    {
        InstigatorPRI = SFXPRI(InstigatedBy.PlayerReplicationInfo);
    }
    if (EnableDamage == FALSE)
    {
        Damage = 0.0;
        Momentum = vect(0.0, 0.0, 0.0);
        return;
    }
    foreach LocalPlayerControllers(Class'BioPlayerController', PC)
    {
        Info = BioWorldInfo(WorldInfo);
        if ((Info != None && PC.GameModeManager2.IsActive(6)) && Info.m_bForceCinematicDamage == FALSE)
        {
            Damage = 0.0;
            Momentum = vect(0.0, 0.0, 0.0);
            return;
        }
        if (PC.GameModeManager2.IsActive(5))
        {
            Damage = 0.0;
            Momentum = vect(0.0, 0.0, 0.0);
            return;
        }
    }
    if (injured != None)
    {
        if (injured.bCanBeDamaged == FALSE || injured.PhysicsVolume != None && injured.PhysicsVolume.bNeutralZone)
        {
            Damage = 0.0;
            Momentum = vect(0.0, 0.0, 0.0);
            return;
        }
        TargetPawn = Pawn(injured);
        if (TargetPawn != None)
        {
            TargetBP = BioPawn(TargetPawn);
            if (TargetPawn.InGodMode() || TargetBP != None && TargetBP.m_oBehavior.IsPlotProtected())
            {
                Damage = 0.0;
                Momentum = vect(0.0, 0.0, 0.0);
                return;
            }
            if (TargetBP != None && TargetBP.GetCurrentHealth() <= float(0))
            {
                Damage = 1.0;
                return;
            }
            DamageMod = TargetBP.GetModule(Class'SFXModule_Damage');
            if (DamageMod.bPartBasedDamageEnabled && DamageType.default.bPartBasedDamageDisabled == FALSE)
            {
                HitBody = DamageMod.GetBodyFromHit(HitInfo);
                if ((HitBody != None && HitBody.bPartBasedDamageEnabled) && InstigatedBy != None)
                {
                    Multiplier += HitBody.fDamageScale - float(1);
                    SFXPRI(InstigatedBy.PlayerReplicationInfo).LastDamageCalculation.PartBasedDamageMultiplier = HitBody.fDamageScale;
                    if (HitBody.fDamageScale >= 1.5)
                    {
                        if (InstigatedBy != None)
                        {
                            if (DamageType.default.bResearchBonus_SniperHeadShotBonus)
                            {
                                Multiplier += SFXGame(TargetBP.WorldInfo.Game).BonusList.RchSniperHeadShotDmgCache - float(1);
                            }
                            if (InstigatedBy.Pawn != None)
                            {
                                Manager = InstigatedBy.Pawn.GetModule(Class'SFXModule_GameEffectManager');
                                if (Manager != None)
                                {
                                    Multiplier += Manager.GetEffectValue(Class'SFXGameEffect_PassiveHeadShotBonus');
                                }
                            }
                            Self.ScoreHeadshot(InstigatedBy, DamageType);
                        }
                    }
                }
            }
            if (SFXPawn_Player(TargetBP) != None || SFXPawn_Henchman(TargetBP) != None)
            {
                if (TargetBP.IsInCoverLeaning())
                {
                    Multiplier *= gameconfig.PlayerCoverLeanDamageMultiplier;
                    InstigatorPRI.LastDamageCalculation.CoverLeanMultiplier = gameconfig.PlayerCoverLeanDamageMultiplier;
                }
                else if (TargetBP.IsInCover())
                {
                    if (!DamageType.default.bIgnoresCoverDirection && Normal(HitLocation - InstigatedBy.Pawn.Location) Dot Vector(TargetBP.CurrentLink.GetSlotRotation(TargetBP.CurrentSlotIdx)) < -0.300000012)
                    {
                        if (VSize(HitLocation - InstigatedBy.Pawn.Location) > 300.0)
                        {
                            Damage = 0.0;
                            Momentum = vect(0.0, 0.0, 0.0);
                            return;
                        }
                    }
                    Multiplier *= gameconfig.PlayerCoverDamageMultiplier;
                    InstigatorPRI.LastDamageCalculation.CoverMultiplier = gameconfig.PlayerCoverDamageMultiplier;
                }
                else
                {
                    Manager = TargetBP.GetModule(Class'SFXModule_GameEffectManager');
                    if (Manager != None)
                    {
                        Multiplier *= Manager.GetEffectValue(Class'SFXGameEffect_NonCoverDamageResistBonus', 1.0);
                    }
                }
                if (SFXPawn_Player(TargetBP) != None && BioPlayerController(TargetBP.Controller).bStorming)
                {
                    Multiplier *= gameconfig.PlayerStormDamageMultiplier;
                    InstigatorPRI.LastDamageCalculation.StormMultiplier = gameconfig.PlayerStormDamageMultiplier;
                }
            }
            if (((TargetPawn != None && SFXPawn_Player(TargetBP) == None) && TargetPawn.Physics == EPhysics.PHYS_RigidBody) && SFXWeapon(DamageCauser) != None)
            {
                Multiplier += gameconfig.PawnInRagdollDamageMultiplier;
                InstigatorPRI.LastDamageCalculation.RagdollMultiplier = gameconfig.PawnInRagdollDamageMultiplier;
            }
        }
    }
    Damage *= Multiplier;
    if (Damage > float(0) && TargetPawn != None)
    {
        if (Damage > float(0) && SFXInventoryManager(TargetPawn.InvManager) != None)
        {
            SFXInventoryManager(TargetPawn.InvManager).ProcessDamage(Damage, HitInfo, HitLocation, Momentum, DamageType, InstigatedBy, DamageCauser);
        }
    }
}