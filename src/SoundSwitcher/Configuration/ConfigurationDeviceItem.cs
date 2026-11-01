using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundSwitcher.Configuration;

public sealed class ConfigurationDeviceItem : INotifyPropertyChanged
{
    private string _label;
    private bool _isEnabled;

    public ConfigurationDeviceItem(
        string endpointId,
        string label,
        string displayName,
        bool isEnabled,
        bool isAvailable)
    {
        EndpointId = endpointId;
        _label = label;
        DisplayName = displayName;
        _isEnabled = isEnabled;
        IsAvailable = isAvailable;
    }

    public string EndpointId { get; }

    public string DisplayName { get; }

    public bool IsAvailable { get; }

    public string AvailabilityText => IsAvailable ? "Available" : "Unavailable";

    public string Label
    {
        get => _label;
        set
        {
            if (_label == value)
            {
                return;
            }

            _label = value;
            OnPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
