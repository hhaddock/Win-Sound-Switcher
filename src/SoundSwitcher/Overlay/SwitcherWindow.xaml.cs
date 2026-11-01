using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SoundSwitcher.Domain;

namespace SoundSwitcher.Overlay;

public partial class SwitcherWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;

    public SwitcherWindow(
        IReadOnlyList<ConfiguredAudioDevice> configuredDevices,
        IReadOnlyCollection<AudioDevice> availableDevices,
        string? currentEndpointId)
    {
        ArgumentNullException.ThrowIfNull(configuredDevices);
        ArgumentNullException.ThrowIfNull(availableDevices);

        var availableIds = availableDevices
            .Where(device => device.IsAvailable)
            .Select(device => device.EndpointId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Items = new ObservableCollection<SwitcherDeviceItem>(
            configuredDevices.Select(device => new SwitcherDeviceItem(
                device.EndpointId,
                device.Label,
                availableIds.Contains(device.EndpointId),
                string.Equals(
                    device.EndpointId,
                    currentEndpointId,
                    StringComparison.OrdinalIgnoreCase))));

        InitializeComponent();
        DataContext = this;

        SelectedItem = Items.FirstOrDefault(item => item.IsActive)
            ?? Items.FirstOrDefault(item => item.IsAvailable);

        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _dismissTimer.Tick += DismissTimer_Tick;
    }

    public ObservableCollection<SwitcherDeviceItem> Items { get; }

    public SwitcherDeviceItem? SelectedItem
    {
        get => DeviceList.SelectedItem as SwitcherDeviceItem;
        set => DeviceList.SelectedItem = value;
    }

    public string? SelectedEndpointId { get; private set; }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Activate();
        Focus();
        _dismissTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _dismissTimer.Stop();
        base.OnClosed(e);
    }

    private void DismissTimer_Tick(object? sender, EventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Enter:
                ApplySelection();
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            default:
                if (TryGetNumberIndex(e.Key, out var numberIndex))
                {
                    SelectAvailableItem(numberIndex);
                    e.Handled = true;
                }

                break;
        }

        RestartDismissTimer();
    }

    private void DeviceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ApplySelection();
    }

    private void ApplySelection()
    {
        if (SelectedItem is null || !SelectedItem.IsAvailable)
        {
            return;
        }

        SelectedEndpointId = SelectedItem.EndpointId;
        DialogResult = true;
    }

    private void MoveSelection(int offset)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedItem is null ? 0 : Items.IndexOf(SelectedItem);
        for (var step = 1; step <= Items.Count; step++)
        {
            var candidateIndex = (currentIndex + (offset * step) + Items.Count) % Items.Count;
            if (Items[candidateIndex].IsAvailable)
            {
                SelectedItem = Items[candidateIndex];
                return;
            }
        }
    }

    private void SelectAvailableItem(int index)
    {
        if (index >= 0 && index < Items.Count && Items[index].IsAvailable)
        {
            SelectedItem = Items[index];
        }
    }

    private static bool TryGetNumberIndex(Key key, out int index)
    {
        if (key >= Key.D1 && key <= Key.D9)
        {
            index = key - Key.D1;
            return true;
        }

        if (key >= Key.NumPad1 && key <= Key.NumPad9)
        {
            index = key - Key.NumPad1;
            return true;
        }

        index = -1;
        return false;
    }

    private void RestartDismissTimer()
    {
        _dismissTimer.Stop();
        _dismissTimer.Start();
    }
}
