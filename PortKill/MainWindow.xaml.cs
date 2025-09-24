using Microsoft.UI.Xaml;
using System;
using WinUIEx;
using System.Diagnostics;
using System.IO;

namespace PortKill;

public sealed partial class MainWindow : Window
{
    private readonly ProcessFinder processFinder = new ProcessFinder();

    public MainWindow()
    {
        this.SetWindowSize(400, 200);
        ExtendsContentIntoTitleBar = true;
        InitializeComponent();
    }

    private void StackPanel_Loaded(object sender, RoutedEventArgs e)
    {
        portNumberTextBox.Focus(FocusState.Programmatic);
    }

    private void ShowProcessButton_Click(object sender, RoutedEventArgs args)
    {
        try
        {
            if (int.TryParse(portNumberTextBox.Text, out int port))
            {
                if (processFinder.GetProcessListeningOnPort(port) is Process process)
                {
                    Trace.WriteLine($"Process ID: {process.Id}");
                    string executableName = GetExecutableName(process);
                    infoTextBlock.Text = $"{executableName} ({process.Id})";
                }
                else
                {
                    infoTextBlock.Text = "No process found";
                }
            }
            else
            {
                infoTextBlock.Text = "Invalid port";
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Could not retrieve process info: {e}");
            infoTextBlock.Text = $"Error: {e.Message}";
        }
    }

    private void KillButton_Click(object sender, RoutedEventArgs args)
    {
        try
        {
            if (int.TryParse(portNumberTextBox.Text, out int port))
            {
                if (processFinder.GetProcessListeningOnPort(port) is Process process)
                {
                    Trace.WriteLine($"Process ID: {process.Id}");
                    process.Kill(true);
                    infoTextBlock.Text = $"Process {process.Id} successfully killed";
                }
                else
                {
                    infoTextBlock.Text = "No process found";
                }
            }
            else
            {
                infoTextBlock.Text = "Invalid port";
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Could not kill process: {e}");
            infoTextBlock.Text = $"Error: {e.Message}";
        }
    }

    private string GetExecutableName(Process process)
    {
        try
        {
            if (process.MainModule is not null)
            {
                return Path.GetFileName(process.MainModule.FileName);

            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Could not retrieve executable name: {e}");
        }

        return process.ProcessName;
    }
}
