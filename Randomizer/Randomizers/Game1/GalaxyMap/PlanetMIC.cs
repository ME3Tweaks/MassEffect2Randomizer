using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game1.GalaxyMap
{
    class PlanetMIC
    {
        public static void RandomizePlanetMaterialInstanceConstant(GameTarget target, ExportEntry planetMaterial, bool realistic = false)
        {
            var props = planetMaterial.GetProperties();
            {
                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                scalars[0].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 1.0); //Horizon Atmosphere Intensity
                if (ThreadSafeRandom.Next(4) == 0)
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 0.7); //Atmosphere Min (how gas-gianty it looks)
                }
                else
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = 0; //Atmosphere Min (how gas-gianty it looks)
                }

                scalars[3].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 1.5); //Atmosphere Tiling U
                scalars[4].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 1.5); //Atmosphere Tiling V
                scalars[5].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 4); //Atmosphere Speed
                scalars[6].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0.5, 12); //Atmosphere Fall off...? seems like corona intensity

                foreach (var vector in vectors)
                {
                    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    StructTools.RandomizeTint(paramValue, false);
                }
            }
            planetMaterial.WriteProperties(props);
        }
    }
}
