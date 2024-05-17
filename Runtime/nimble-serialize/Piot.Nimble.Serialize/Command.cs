namespace Piot.Nimble.Serialize
{
    public static class Constants
    {
        public const uint OptimalChunkSize = 1000;
    }

    public enum ClientCommand
    {
        StartDownload = 0xa4,
        DownloadSerializedSaveState = 0x87,
        AuthoritativeSteps = 0x1f,
    }
}