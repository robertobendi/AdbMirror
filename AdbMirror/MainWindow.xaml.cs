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
        InitializeComponent();
        DataContext = new MainViewModel();
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


