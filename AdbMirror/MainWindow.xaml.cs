using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace AdbMirror;

/// <summary>
/// Main window hosting the minimal Phone Mirror UI.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize application:\n\n{ex}\n\nPlease check that platform-tools and scrcpy are available.",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            throw; // Re-throw to show error
        }
    }

    private void OnCreditsClick(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}


