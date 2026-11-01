using System.Text.Json;
using SoundSwitcher.Audio;
using SoundSwitcher.Domain;

var operation = args.FirstOrDefault()?.ToLowerInvariant();

if (string.IsNullOrWhiteSpace(operation))
{
    Console.Error.WriteLine("An audio worker operation is required.");
    return 2;
}

try
{
    var audioDeviceService = new NAudioDeviceService();

    switch (operation)
    {
        case "list" when args.Length == 1:
        {
            var devices = await audioDeviceService.GetOutputDevicesAsync();
            WriteJson(devices.Select(ToWireModel));
            return 0;
        }
        case "default" when args.Length == 1:
        {
            var device = await audioDeviceService.GetDefaultOutputDeviceAsync();
            WriteJson(device is null ? null : ToWireModel(device));
            return 0;
        }
        case "get" when args.Length == 2 && !string.IsNullOrWhiteSpace(args[1]):
        {
            var device = await audioDeviceService.GetOutputDeviceAsync(args[1]);
            WriteJson(device is null ? null : ToWireModel(device));
            return 0;
        }
        case "switch" when args.Length == 2 && !string.IsNullOrWhiteSpace(args[1]):
            await audioDeviceService.SetDefaultOutputDeviceAsync(args[1]);
            return 0;
        default:
            Console.Error.WriteLine($"Invalid audio worker operation: {operation}");
            return 2;
    }
}
catch (Exception exception) when (exception is not StackOverflowException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static AudioDeviceWireModel ToWireModel(AudioDevice device) =>
    new(device.EndpointId, device.DisplayName, device.IsAvailable);

static void WriteJson<T>(T value) =>
    Console.WriteLine(JsonSerializer.Serialize(value));
