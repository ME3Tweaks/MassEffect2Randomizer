using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Handlers;

namespace Randomizer.Randomizers.Game2.Misc
{
    class HammerHead
    {
        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            var ini = CoalescedHandler.GetIniFile("BioGame.ini");

            var section = ini.GetOrAddSection("SFXGame.SFXVehicleHover");
            section.SetSingleEntry("JumpForce", GenerateRandomVectorStruct(-400, 400, 0, 0, 1200, 4000));
            section.SetSingleEntry("OnGroundJumpMultiplier", ThreadSafeRandom.NextFloat(.5, 2));
            section.SetSingleEntry("MaxThrustJuice", ThreadSafeRandom.NextFloat(0.5, 1));
            section.SetSingleEntry("BoostForce", GenerateRandomVectorStruct(600, 2000, 0, 0, -200, 50));
            section.SetSingleEntry("ThrustRegenerationFactor", ThreadSafeRandom.NextFloat(0.45, 0.7));
            section.SetSingleEntry("SelfRepairRate", ThreadSafeRandom.NextFloat(50, 150));
            section.SetSingleEntry("SelfRepairDelay", ThreadSafeRandom.NextFloat(3, 8));
            section.SetSingleEntry("OffGroundForce", GenerateRandomVectorStruct(0, 0, 0, 0, -2200, -10));
            section.SetSingleEntry("ThrustRegenerationDelay", ThreadSafeRandom.NextFloat(0.2, 1));
            section.SetSingleEntry("VerticalThrustBurnRate", ThreadSafeRandom.NextFloat(0.5, 1.5));
            section.SetSingleEntry("BurnOutPercentage", ThreadSafeRandom.NextFloat(0.1, 0.3));
            section.SetSingleEntry("MaxPitchAngle", ThreadSafeRandom.NextFloat(25, 55));

            ini = CoalescedHandler.GetIniFile("BioWeapon.ini");
            section = ini.GetOrAddSection("SFXGameContent_Inventory.SFXHeavyWeapon_VehicleMissileLauncher");
            section.SetSingleEntry("Damage", ThreadSafeRandom.NextFloat(200, 500));
            section.SetSingleEntry("RateOfFire", ThreadSafeRandom.NextFloat(100, 300));
            return true;
        }

        private static string GenerateRandomRangeStruct(float min1, float max1, float min2, float max2)
        {
            var x = ThreadSafeRandom.NextFloat(min1, max1);
            var y = ThreadSafeRandom.NextFloat(min2, max2);
            return $"(X={x},Y={y})";
        }

        private static string GenerateRandomVectorStruct(float min1, float max1, float min2, float max2, float min3, float max3)
        {
            var x = ThreadSafeRandom.NextFloat(min1, max1);
            var y = ThreadSafeRandom.NextFloat(min2, max2);
            var z = ThreadSafeRandom.NextFloat(min3, max3);
            return $"(X={x},Y={y},Z={z})";
        }
    }
}
