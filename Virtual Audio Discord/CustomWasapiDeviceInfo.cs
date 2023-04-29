using ManagedBass.Wasapi;

public class CustomWasapiDeviceInfo
{
    public int Index { get; set; }
    public WasapiDeviceInfo DeviceInfo { get; set; }

    public override string ToString()
    {
        return DeviceInfo.ToString();
    }
}
