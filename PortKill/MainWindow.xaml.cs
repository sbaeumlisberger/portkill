using Microsoft.UI.Xaml;
using System;
using System.Net;
using System.Runtime.InteropServices;
using Windows.Win32;
using WinUIEx;
using Windows.Win32.NetworkManagement.IpHelper;
using System.Diagnostics;
using System.IO;

namespace PortKill;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.SetWindowSize(400, 200);
        InitializeComponent();
    }

    private void StackPanel_Loaded(object sender, RoutedEventArgs e)
    {
        portNumberTextBox.Focus(FocusState.Programmatic);
    }

    private void ShowProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(portNumberTextBox.Text, out int port))
        {
            if (GetProcessListeningOnPort(port) is Process process)
            {
                Trace.WriteLine($"Process ID: {process.Id}");
                string executableName = Path.GetFileName(process.MainModule?.FileName) ?? process.ProcessName;
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

    private void KillButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(portNumberTextBox.Text, out int port))
        {
            if (GetProcessListeningOnPort(port) is Process process)
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

    public unsafe static Process? GetProcessListeningOnPort(int port)
    {
        const int AF_INET = 2;

        uint bufferSize = 0;

        PInvoke.GetExtendedTcpTable(null, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

        if (bufferSize <= 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            uint result = PInvoke.GetExtendedTcpTable(buffer.ToPointer(), ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

            if (result != 0)
            {
                return null;
            }

            int numberOfEntries = Marshal.ReadInt32(buffer);
            IntPtr rowPointer = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < numberOfEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPointer);

                int localPort = IPAddress.NetworkToHostOrder((short)(ushort)(row.dwLocalPort & 0xFFFF)) & 0xFFFF;

                if (localPort == port)
                {
                    return Process.GetProcessById((int)row.dwOwningPid);
                }

                rowPointer = IntPtr.Add(rowPointer, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return null;
    }
}
