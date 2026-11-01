using System.Runtime.InteropServices;

namespace SoundSwitcher.Audio;

/// <summary>
/// Small, isolated interop adapter for Windows' audio policy COM interface.
/// The interface is intentionally kept out of the application and domain layers.
/// </summary>
internal static class AudioPolicyConfig
{
    private static readonly Guid PolicyConfigClientClsid =
        new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");

    private static readonly AudioRole[] DefaultOutputRoles =
    [
        AudioRole.Console,
        AudioRole.Multimedia
    ];

    public static void SetDefaultOutputEndpoint(
        string endpointId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);

        var policyConfigType = Type.GetTypeFromCLSID(PolicyConfigClientClsid, throwOnError: true)
            ?? throw new InvalidOperationException("Windows audio policy configuration is unavailable.");

        var policyConfig = Activator.CreateInstance(policyConfigType) as IPolicyConfig
            ?? throw new InvalidOperationException("Windows audio policy configuration is unavailable.");

        try
        {
            foreach (var role in DefaultOutputRoles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = policyConfig.SetDefaultEndpoint(endpointId, role);
                Marshal.ThrowExceptionForHR(result);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(policyConfig);
        }
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr format);

        int GetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int defaultFormat,
            IntPtr format);

        int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        int SetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IntPtr endpointFormat,
            IntPtr mixFormat);

        int GetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int defaultPeriod,
            IntPtr defaultPeriodValue,
            IntPtr minimumPeriodValue);

        int SetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IntPtr periodValue);

        int GetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IntPtr mode);

        int SetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            IntPtr mode);

        int GetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int store,
            IntPtr propertyKey,
            IntPtr value);

        int SetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int store,
            IntPtr propertyKey,
            IntPtr value);

        int SetDefaultEndpoint(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            AudioRole role);

        int SetEndpointVisibility(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int visible);
    }

    private enum AudioRole
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2
    }
}
