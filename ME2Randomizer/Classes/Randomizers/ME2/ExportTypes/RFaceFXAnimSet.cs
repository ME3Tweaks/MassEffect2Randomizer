using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    public class RFaceFXAnimSet
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "FaceFXAnimSet";
        public static bool RandomizeExport(ExportEntry exp, Random random, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            //if (exp.UIndex != 1999) return false;
            try
            {
                Log.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomizing FaceFX export {exp.UIndex}");
                var d = exp.Data;
                var animSet = ObjectBinary.From<FaceFXAnimSet>(exp);
                for (int i = 0; i < animSet.Lines.Count(); i++)
                {
                    var faceFxline = animSet.Lines[i];
                    //if (true)

                    bool randomizedBoneList = false;
                    if (random.Next(10 - (int)option.SliderValue) == 0)
                    {
                        //Randomize the names used for animation
                        faceFxline.AnimationNames.Shuffle(random);
                        randomizedBoneList = true;
                    }
                    if (!randomizedBoneList || random.Next(16 - (int)option.SliderValue) == 0)
                    {
                        //Randomize the points
                        for (int j = 0; j < faceFxline.Points.Count; j++)
                        {
                            bool isLast = j == faceFxline.Points.Count;
                            var currentWeight = faceFxline.Points[j].weight;

                            var currentPoint = faceFxline.Points[j];
                            switch (option.SliderValue)
                            {
                                case 1: //A few broken bones
                                    currentPoint.weight += random.NextFloat(-.25, .25);
                                    break;
                                case 2: //A significant option.SliderValue of broken bones
                                    currentPoint.weight += random.NextFloat(-.5, .5);
                                    break;
                                case 3: //That's not how the face is supposed to work
                                    if (random.Next(5) == 0)
                                    {
                                        currentPoint.weight = random.NextFloat(-3, 3);
                                    }
                                    else if (isLast && random.Next(3) == 0)
                                    {
                                        currentPoint.weight = random.NextFloat(.7, .7);
                                    }
                                    else
                                    {
                                        currentPoint.weight *= 8;
                                    }

                                    break;
                                case 4: //:O
                                    if (random.Next(12) == 0)
                                    {
                                        currentPoint.weight = random.NextFloat(-5, 5);
                                    }
                                    else if (isLast && random.Next(3) == 0)
                                    {
                                        currentPoint.weight = random.NextFloat(.9, .9);
                                    }
                                    else
                                    {
                                        currentPoint.weight *= 5;
                                    }
                                    //Debug.WriteLine(currentPoint.weight);

                                    break;
                                case 5: //Utter madness
                                    currentPoint.weight = random.NextFloat(-10, 10);
                                    break;
                                default:
                                    Debugger.Break();
                                    break;
                            }

                            faceFxline.Points[j] = currentPoint; //Reassign the struct
                        }
                    }

                    //Debugging only: Get list of all animation names
                    //for (int j = 0; j < faceFxline.animations.Length; j++)
                    //{
                    //    var animationName = animSet.Header.Names[faceFxline.animations[j].index]; //animation name
                    //    faceFxBoneNames.Add(animationName);
                    //}
                }

                var dataBefore = exp.Data;
                exp.WriteBinary(animSet);
                var dataAfter = exp.Data;
                if (dataBefore.SequenceEqual(dataAfter))
                {
                    //Debugger.Break();
                }
                else
                {
                    Log.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomized FaceFX for export " + exp.UIndex);
                }
                return true;
            }
            catch (Exception e)
            {
                //Do nothing for now.
                Log.Error("AnimSet error! " + App.FlattenException((e)));
            }
            return false;
        }
    }
}
