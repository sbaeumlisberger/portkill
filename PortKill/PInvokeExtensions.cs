using System;
using Windows.Win32.UI.Shell;
using Windows.Win32.Storage.FileSystem;

namespace PortKill;

internal static class PInvokeExtensions
{
    private const int MAX_PATH = 260;

    public static unsafe string GetPath(this ref IShellLinkW shellLink)
    {
        var buffer = new Span<char>(new char[MAX_PATH]);
        var findData = new WIN32_FIND_DATAW();
        shellLink.GetPath(buffer, ref findData, 0);
        int length = buffer.IndexOf('\0');
        return length >= 0 ? new string(buffer.Slice(0, length)) : new string(buffer);
    }
}
