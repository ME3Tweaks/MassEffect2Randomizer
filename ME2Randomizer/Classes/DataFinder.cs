using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MassEffectRandomizer;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

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


            mainWindow.ButtonPanelVisible = Visibility.Collapsed;
            mainWindow.ProgressPanelVisible = Visibility.Visible;
            dataworker.RunWorkerAsync();
        }

        private void ResetUI(object sender, RunWorkerCompletedEventArgs e)
        {
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.CurrentProgressValue = 0;
            mainWindow.ButtonPanelVisible = Visibility.Collapsed;
            mainWindow.ProgressPanelVisible = Visibility.Visible;
            mainWindow.CurrentOperationText = "Data finder done";
        }

        private void FindDatapads(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values.ToList();
            mainWindow.CurrentOperationText = "Scanning for datapads";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Scanning for datapads [{numdone}/{numtodo}]";
                  var package = MEPackageHandler.OpenMEPackage(file);
                  var skeletalMeshes = package.Exports.Where(x => x.ClassName == "SkeletalMeshComponent");
                  foreach (var skm in skeletalMeshes)
                  {
                      var sm = skm.GetProperty<ObjectProperty>("SkeletalMesh");
                      if (sm != null && sm.Value != 0)
                      {
                          var entry = package.GetUExport(sm.Value);
                          if (entry.ObjectName.Name.Contains("datapad", StringComparison.InvariantCultureIgnoreCase))
                          {
                              Debug.WriteLine($"{entry.UIndex} {entry.InstancedFullPath} in {file}");
                          }
                      }
                  }
                  Interlocked.Increment(ref numdone);
                  mainWindow.CurrentProgressValue = numdone;
              });
        }
    }
}
