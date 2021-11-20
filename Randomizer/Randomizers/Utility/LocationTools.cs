using System.Numerics;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace Randomizer.Randomizers.Utility
{
    public static class LocationTools
    {
        public static void SetLocation(ExportEntry export, Vector3 location)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, location.X, location.Y, location.Z);
            export.WriteProperty(prop);
        }

        public static void SetLocation(ExportEntry export, CFVector3 location)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, location);
            export.WriteProperty(prop);
        }

        public static void SetLocation(ExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, x, y, z);
            export.WriteProperty(prop);
        }
        public static void SetLocation(StructProperty prop, float x, float y, float z)
        {
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
        }

        public static void SetLocation(StructProperty prop, CFVector3 vector)
        {
            prop.GetProp<FloatProperty>("X").Value = vector.X;
            prop.GetProp<FloatProperty>("Y").Value = vector.Y;
            prop.GetProp<FloatProperty>("Z").Value = vector.Z;
        }

        public static CFVector3? GetLocation(ExportEntry export)
        {
            float x = 0, y = 0, z = int.MinValue;
            var prop = export.GetProperty<StructProperty>("location");
            if (prop != null)
            {
                foreach (var locprop in prop.Properties)
                {
                    switch (locprop)
                    {
                        case FloatProperty fltProp when fltProp.Name == "X":
                            x = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Y":
                            y = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Z":
                            z = fltProp;
                            break;
                    }
                }

                return new CFVector3() { X = x, Y = y, Z = z };
            }

            return null;
        }

        public static void SetRotation(ExportEntry export, float newDirectionDegrees)
        {
            StructProperty prop = export.GetProperty<StructProperty>("rotation");
            if (prop == null)
            {
                PropertyCollection p = new PropertyCollection();
                p.Add(new IntProperty(0, "Pitch"));
                p.Add(new IntProperty(0, "Yaw"));
                p.Add(new IntProperty(0, "Roll"));
                prop = new StructProperty("Rotator", p, "Rotation", true);
            }
            SetRotation(prop, newDirectionDegrees);
            export.WriteProperty(prop);
        }

        /// <summary>
        /// Sets the Yaw rotation on a struct property.
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="newDirectionDegrees"></param>
        public static void SetRotation(StructProperty prop, float newDirectionDegrees)
        {
            int newYaw = (int)((newDirectionDegrees / 360) * 65535);
            prop.GetProp<IntProperty>("Yaw").Value = newYaw;
        }

        /// <summary>
        /// Generates a random position between -100,000 and 100,0000
        /// </summary>
        /// <param name="e"></param>
        public static void RandomizeLocation(ExportEntry e)
        {
            SetLocation(e, ThreadSafeRandom.NextFloat(-100000, 100000), ThreadSafeRandom.NextFloat(-100000, 100000), ThreadSafeRandom.NextFloat(-100000, 100000));
        }
    }
}
