namespace Microsoft.Band.Admin;

internal enum KeyboardMessageType : byte
{
    Init,
    Stroke,
    CandidatesForNextWord,
    CandidatesForWord,
    End,
    PreInit,
    TryReleaseClient,
    PreInitV2
}
