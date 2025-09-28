using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.System.Com;
using WinUIEx;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Microsoft.UI;

namespace PortKill;

public sealed partial class MainWindow : Window
{
    private readonly ProcessFinder processFinder = new ProcessFinder();

    public MainWindow()
    {
        this.SetWindowSize(400, 200);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonForegroundColor = App.Current.RequestedTheme == ApplicationTheme.Light ? Colors.Black : Colors.White;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"));

        InitializeComponent();

        CreateStartMenuEntry();
    }

    private unsafe void CreateStartMenuEntry()
    {
        try
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "PortKill.lnk");
            if (!File.Exists(filePath))
            {
                var exePath = Environment.ProcessPath!;

                Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
                PInvoke.CoCreateInstance(CLSID_ShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER, out IShellLinkW* shellLink).ThrowOnFailure();

                shellLink->SetPath(exePath);
                shellLink->SetDescription("PortKill");
                shellLink->SetWorkingDirectory(Path.GetDirectoryName(exePath)!);
                shellLink->SetIconLocation(exePath, 0);

                Guid IID_IPersistFile = new("0000010B-0000-0000-C000-000000000046");
                shellLink->QueryInterface(IID_IPersistFile, out void* persistFilePtr).ThrowOnFailure();
                var persistFile = (IPersistFile*)persistFilePtr;
                persistFile->Save(filePath, true);

                persistFile->Release();
                shellLink->Release();
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"Could not create Start Menu entry: {e}");
        }
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
