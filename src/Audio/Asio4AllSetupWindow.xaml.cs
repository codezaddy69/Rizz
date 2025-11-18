using System.Diagnostics;
using System.Windows;

namespace DJMixMaster.Audio
{
    /// <summary>
    /// Interaction logic for Asio4AllSetupWindow.xaml
    /// </summary>
    public partial class Asio4AllSetupWindow : Window
    {
        public Asio4AllSetupWindow()
        {
            InitializeComponent();
        }

        private void OpenSoundSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open Windows Sound control panel
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control",
                    Arguments = "mmsys.cpl,,0",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback: try to open settings app
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:sound",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Last resort: show message
                    MessageBox.Show("Please open Windows Sound settings manually.", "Unable to open settings");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}