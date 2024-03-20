namespace Piot.Nimble.Serialize
{
    public enum ClientToHostRequest
    {
        AckSerializedSaveStateBlobStream = 93,
        RequestAddPredictedStep = 0xf2,
        AckSerializedSaveStateStart = 0xff
    }
}