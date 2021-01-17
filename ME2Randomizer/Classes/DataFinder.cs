using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using MassEffectRandomizer;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes
{
    class DataFinder
    {
        private MainWindow mainWindow;
        private BackgroundWorker dataworker;

        public DataFinder(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            dataworker = new BackgroundWorker();

            dataworker.DoWork += FindDatapads;
            dataworker.RunWorkerCompleted += ResetUI;

            mainWindow.ShowProgressPanel = true;
            dataworker.RunWorkerAsync();
        }

        private void ResetUI(object sender, RunWorkerCompletedEventArgs e)
        {
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.CurrentProgressValue = 0;
            mainWindow.ShowProgressPanel = false;
            mainWindow.CurrentOperationText = "Data finder done";
        }

        private void FindDatapads(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values.ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            ConcurrentDictionary<string, string> listM = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                  var package = MEPackageHandler.OpenMEPackage(file);
                  var weapons = package.Exports.Where(x => x.IsDefaultObject && x.IsA("SFXWeapon"));
                  foreach (var skm in weapons)
                  {
                      if (skm.ComponentMap.TryGetValue("WeaponMesh", out var mesh) && package.IsUExport(mesh + 1))
                      {
                          var meshExp = package.GetUExport(mesh + 1); // components are off by 1 for some reason
                          if (meshExp.GetProperty<ObjectProperty>("SkeletalMesh") != null)
                          {
                              listM[skm.Class.InstancedFullPath] = skm.Class.UIndex + " " + file;
                          }
                      }
                  }
                  Interlocked.Increment(ref numdone);
                  mainWindow.CurrentProgressValue = numdone;
              });

            foreach (var v in listM)
            {
                Debug.WriteLine($"{v.Key}\t{v.Value}");
            }

            // Coagulate stuff
            //Dictionary<string, int> counts = new Dictionary<string, int>();
            //foreach (var v in listM)
            //{
            //    foreach (var k in v.Value)
            //    {
            //        int existingC = 0;
            //        counts.TryGetValue(k, out existingC);
            //        existingC++;
            //        counts[k] = existingC;
            //    }
            //}

            //foreach (var count in counts.OrderBy(x => x.Key))
            //{
            //    Debug.WriteLine($"{count.Key}\t\t\t{count.Value}");
            //}
        }
    }
}
