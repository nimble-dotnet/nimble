namespace Piot.Nimble.Serialize
{
    public static class Constants
    {
        public const uint OptimalChunkSize = 1024;
    }

    public enum ClientCommand
    {
        StartDownload,
        DownloadSerializedSaveState,
        AuthoritativeSteps,
    }
}