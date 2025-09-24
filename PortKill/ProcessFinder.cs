using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.NetworkManagement.IpHelper;

namespace PortKill;

internal class ProcessFinder
{
    private const uint AF_INET = 2;
    private const uint AF_INET6 = 23;

    public Process? GetProcessListeningOnPort(int port)
    {
        if (GetProcessListeningOnTcpPort(port, AF_INET) is Process tcpProcess)
        {
            return tcpProcess;
        }
        if (GetProcessListeningOnTcpPort(port, AF_INET6) is Process tcpIPv6Process)
        {
            return tcpIPv6Process;
        }
        if (GetProcessListeningOnUdpPort(port, AF_INET) is Process udpProcess)
        {
            return udpProcess;
        }
        if (GetProcessListeningOnUdpPort(port, AF_INET6) is Process udpIPv6Process)
        {
            return udpIPv6Process;
        }
        return null;
    }

    private unsafe Process? GetProcessListeningOnTcpPort(int port, uint addressFamily)
    {
        uint bufferSize = 0;

        PInvoke.GetExtendedTcpTable(null, ref bufferSize, true, addressFamily, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

        if (bufferSize <= 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            uint result = PInvoke.GetExtendedTcpTable(buffer.ToPointer(), ref bufferSize, true, addressFamily, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

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

    private unsafe Process? GetProcessListeningOnUdpPort(int port, uint addressFamily)
    {
        uint bufferSize = 0;

        PInvoke.GetExtendedUdpTable(null, ref bufferSize, true, addressFamily, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);

        if (bufferSize <= 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            uint result = PInvoke.GetExtendedUdpTable(buffer.ToPointer(), ref bufferSize, true, addressFamily, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);

            if (result != 0)
            {
                return null;
            }

            int numberOfEntries = Marshal.ReadInt32(buffer);
            IntPtr rowPointer = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();

            for (int i = 0; i < numberOfEntries; i++)
            {
                var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPointer);

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
