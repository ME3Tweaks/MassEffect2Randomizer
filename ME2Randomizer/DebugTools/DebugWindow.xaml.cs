using MahApps.Metro.Controls;
using ME2Randomizer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ME2Randomizer.DebugTools
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
    }
}
