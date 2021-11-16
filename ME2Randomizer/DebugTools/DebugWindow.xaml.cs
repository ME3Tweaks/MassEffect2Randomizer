using System.Windows;
using MahApps.Metro.Controls;
using RandomizerUI.Classes;
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
            DataFinder df = new DataFinder(mw);
        }

        private void CheckProperties_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.TestPropertiesInMERFS();
        }

        private void CheckPropertiesMER_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.TestPropertiesInBinaryAssets();
        }

        private void CheckImports_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.TestAllImportsInMERFS();
        }

        private void BuildStartupFile_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.BuildStartupPackage();
        }

        private void CheckExports_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.GetExportsInPersistentThatAreAlsoInSub();
        }

        private void CheckDroppedExports_Click(object sender, RoutedEventArgs e)
        {
            ME2Debug.CheckImportsWithPersistence();
        }

        private void TestSaveWipe_Click(object sender, RoutedEventArgs e)
        {
            TalentReset.GetSaveFiles();
        }
    }
}
