using System.Diagnostics;
using System.Windows;
using MahApps.Metro.Controls;
using Randomizer.MER;
using Randomizer.Randomizers.Game3.FirstRun;
using Randomizer.Randomizers.Shared.Classes;
using RandomizerUI.Classes.Controllers;

namespace RandomizerUI.DebugTools
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : MetroWindow
    {
        private MainWindow mw;

        public DebugWindow(MainWindow mw)
        {
            this.mw = mw;
            InitializeComponent();
        }

        private void MemAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            new MemoryAnalyzerUI().Show();
        }

        private void DataFinder_Click(object sender, RoutedEventArgs e)
        {
            var fpath = @"B:\SteamLibrary\steamapps\common\Mass Effect Legendary Edition\Game\ME2\BioGame\DLC\DLC_CER_02\CookedPCConsole\BioEngine.ini";
            var proxy = ConfigFileProxy.LoadIni(fpath);

            Debug.WriteLine(proxy.ToXmlString());
            var o = proxy;
            //DataFinder df = new DataFinder(mw);
        }

        private void CheckProperties_Click(object sender, RoutedEventArgs e)
        {
            //ME2Debug.TestPropertiesInMERFS();
        }

        private void CheckPropertiesMER_Click(object sender, RoutedEventArgs e)
        {
            //ME2Debug.TestPropertiesInBinaryAssets();
        }

        private void CheckImports_Click(object sender, RoutedEventArgs e)
        {
            //ME2Debug.TestAllImportsInMERFS();
        }

        private void BuildStartupFile_Click(object sender, RoutedEventArgs e)
        {
            //ME2Debug.BuildStartupPackage();
        }

        private void BuildInventoryPackages_Click(object sender, RoutedEventArgs e)
        {
#if __GAME3__
            Inventory.PerformInventory(Locations.GetTarget(true), @"C:\users\mgame\desktop\Inventory");
#endif
            //ME2Debug.GetExportsInPersistentThatAreAlsoInSub();
        }

        private void CheckDroppedExports_Click(object sender, RoutedEventArgs e)
        {
            //ME2Debug.CheckImportsWithPersistence();
        }

        private void TestSaveWipe_Click(object sender, RoutedEventArgs e)
        {
            TalentReset.GetSaveFiles();
        }
    }
}
