using System;
using System.Windows;
using System.Windows.Threading;

namespace AdbMirror;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        ShowError("Unhandled Exception", exception?.ToString() ?? "Unknown error occurred");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowError("Application Error", e.Exception.ToString());
        e.Handled = true; // Prevent app crash
    }

    private void ShowError(string title, string message)
    {
        try
        {
            MessageBox.Show(
                $"An error occurred:\n\n{message}\n\nThe application will continue running.",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // If MessageBox fails, try console output
            try
            {
                Console.WriteLine($"{title}: {message}");
            }
            catch
            {
                // Ignore if console also fails
            }
        }
    }
}


