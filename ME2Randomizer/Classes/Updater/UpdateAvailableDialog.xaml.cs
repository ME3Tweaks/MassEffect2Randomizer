using System;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace RandomizerUI.Classes.Updater
{
    /// <summary>
    /// Interaction logic for UpdateAvailableDialog.xaml
    /// </summary>
    public partial class UpdateAvailableDialog : CustomDialog
    {
        private MainWindow mainWindowRef;
        private bool _updateAccepted = false;
        public UpdateAvailableDialog(String headertext, String changelog, MainWindow mainWindow)
        {
            InitializeComponent();
            mainWindowRef = mainWindow;
            Textblock_UpdateText.Text = headertext;
            Textblock_ChangelogText.Text = changelog;
        }

        private async void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            _updateAccepted = true;
            await mainWindowRef.HideMetroDialogAsync(this);
        }

        internal bool wasUpdateAccepted()
        {
            return _updateAccepted;
        }

        private async void Later_Button_Click(object sender, RoutedEventArgs e)
        {
            _updateAccepted = false;
            await mainWindowRef.HideMetroDialogAsync(this);
        }
    }
}
