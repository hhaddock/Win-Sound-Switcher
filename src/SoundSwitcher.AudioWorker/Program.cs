using SoundSwitcher.Audio;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("An audio endpoint ID is required.");
    return 2;
}

try
{
    var audioDeviceService = new NAudioDeviceService();
    await audioDeviceService.SetDefaultOutputDeviceAsync(args[0]);
    return 0;
}
catch (Exception exception) when (exception is not StackOverflowException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
