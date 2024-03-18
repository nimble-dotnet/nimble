# Changelog

## :bookmark: [v0.0.11](https://github.com/nimble-dotnet/nimble/releases/tag/v0.0.11) (2024-03-18)


### Fixed Struct

* :lady_beetle: Now `FixedOctets32WithLength` correctly serializes its length [c6df571a](https://github.com/nimble-dotnet/nimble/commit/c6df571a)

## :bookmark: [v0.0.10](https://github.com/nimble-dotnet/nimble/releases/tag/v0.0.10) (2024-03-17)

Mainly rename, refactor and documentation.

### Client

* :book: Add documentation for `NimbleClientReceiveStats`, `NimbleReceiveClient`, `NimbleSendClient`

### Host

* :book: Add documentation for `NimbleHost` and `PredictedStepsReader`.

### Replay

Changed from a generic replay-implementation to a specific for deterministic applications.

* :star2: Add `ScanOptions` for `ReplayReader` that enables a replay file to be read, ignoring some minor file corruptions like a missing end chunk.
* :hammer_and_wrench: Change `ApplicationVersion` from `SemanticVersion` to a 32 octet payload for greater flexibility.
* :hammer_and_wrench: Change icons for RAFF files.
* :book: Documentation of `ReplayReader` and `ReplayWriter`.

### Authoritative Steps

* :lady_beetle: now checks if the `SerializeProviderConnectState` is `Normal` before checking the payload. Payload is null in all other states.
* :hammer_and_wrench: rename `AuthoritativeStep` to `AuthoritativeStepForOneParticipant` to make it more clear.

### Predicted Steps

* :hammer_and_wrench: Rename `Deserialize` to `Read` in `PredictedStepsReader`
* :hammer_and_wrench: Rename `CombinedRangesReader` to `AuthoritativeStepsReader`
* :hammer_and_wrench: Rename `CombinedRangesWriter` to `AuthoritativeStepsWriter`
* :hammer_and_wrench: Rename `PredictedStepsSerialize` to `PredictedStepsWriter`
* :zap: Add AggressiveInlining in `PredictedStepsQueue`

### Discoid (circular buffers)

* :zap: Add AggressiveInlining for `CircularBuffer` and `DiscoidBuffer`

### Flood (octet- and bit-serialization)

* :zap: AggressiveInlining for methods in `OctetReader` and `OctetWriter`

### Fixed Struct

Start move of Piot.FixedStruct into Nimble

* :star2: Add `FixedOctets16`, `FixedOctets32` and `FixedOctets32WithLength`
