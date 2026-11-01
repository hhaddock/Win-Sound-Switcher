namespace SoundSwitcher.Overlay;

public sealed class SwitcherDeviceItem
{
    public SwitcherDeviceItem(
        string endpointId,
        string label,
        bool isAvailable,
        bool isActive)
    {
        EndpointId = endpointId;
        Label = label;
        IsAvailable = isAvailable;
        IsActive = isActive;
    }

    public string EndpointId { get; }

    public string Label { get; }

    public bool IsAvailable { get; }

    public bool IsActive { get; }

    public string StatusText =>
        !IsAvailable
            ? "Unavailable"
            : IsActive
                ? "Active"
                : "Available";
}
