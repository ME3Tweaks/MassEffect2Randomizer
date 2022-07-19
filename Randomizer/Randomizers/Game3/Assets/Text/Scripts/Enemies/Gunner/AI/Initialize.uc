// Random number of turrets

public function Initialize()
{
    local SFXDifficultyHandler DH;
    local SFXModule_Damage DmgMod;
    local SFXShield_Base PawnShield;
    
    SetTimer(0.200000003 + FRand() * 0.300000012, TRUE, 'UpdateRepairTargetsAndTurretNodes', );
    DH = SFXGRI(WorldInfo.GRI).DifficultyHandler;
    if (DH != None)
    {
        CancelFirePct = DH.GetFloat('CancelFirePct', 'Gunner');
        MaxFireWaitTime = DH.GetFloat('MaxFireWaitTime', 'Gunner');
        EvadeDamagePct.X = DH.GetFloat('EvadeDamagePctLow', 'Gunner');
        EvadeDamagePct.Y = DH.GetFloat('EvadeDamagePctHigh', 'Gunner');
        EvadeFrequency = DH.GetFloat('EvadeFrequency', 'Gunner');
        EvadeResetDuration = DH.GetFloat('EvadeResetDuration', 'Gunner');
        PartialLeanPct = DH.GetFloat('PartialLeanPct', 'Gunner');
        PowerEvadeChance = DH.GetFloat('PowerEvadeChance', 'Gunner');
        FlankReactionTime = DH.GetFloat('FlankReactionTime', 'Gunner');
        RepairPct = DH.GetFloat('RepairPct', 'Gunner');
        DeployBreachThreshold = DH.GetFloat('DeployBreachThreshold', 'Gunner');
        DmgMod = MyBP.GetModule(Class'SFXModule_Damage');
        if (DmgMod != None)
        {
            DmgMod.NormalizedHealth = DH.GetFloatNormalized('MaxHealth', 'Gunner');
            DmgMod.LevelScaledHealth = DH.GetFloat('MaxHealth', 'Gunner');
            DmgMod.InitializeMaxHealth(DmgMod.LevelScaledHealth);
        }
        PawnShield = SFXShield_Base(MyBP.InvManager.FindInventoryType(Class'SFXShield_Base', TRUE));
        if (PawnShield != None)
        {
            PawnShield.InitializeMaxShields(DH.GetFloat('MaxShields', 'Gunner'));
            PawnShield.MaxEnemyShieldRecharge = DH.GetFloat('MaxEnemyShieldRecharge', 'Gunner');
            if (PawnShield.MaxEnemyShieldRecharge > 0.0)
            {
                PawnShield.bRechargeable = TRUE;
                PawnShield.ShieldRegenPct = DH.GetFloat('AIShieldRegenPct', 'Gunner');
                PawnShield.ShieldRegenDelay.X = DH.GetMinFloat('AIShieldRegenDelay', 'Gunner');
                PawnShield.ShieldRegenDelay.Y = DH.GetMaxFloat('AIShieldRegenDelay', 'Gunner');
                PawnShield.ShieldRegenDelay.Value = DH.GetFloat('AIShieldRegenDelay', 'Gunner');
            }
        }
        if (MyBP != None)
        {
            MyBP.PowerThreshold_Standard = DH.GetFloat('PowerThreshold_Standard', 'Gunner');
            MyBP.PowerThreshold_Stagger = DH.GetFloat('PowerThreshold_Stagger', 'Gunner');
            MyBP.PowerThreshold_Knockback = DH.GetFloat('PowerThreshold_Knockback', 'Gunner');
            MyBP.HitReactionChanceMultiplier = DH.GetFloat('HitReactionChanceMultiplier', 'Gunner');
        }
    }
    TurretsToPlace = Rand(2) + 1;
    Super(SFXAI_Core).Initialize();
}