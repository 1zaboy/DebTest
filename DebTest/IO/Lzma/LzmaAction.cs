namespace Packaging.Targets.IO
{
    internal enum LzmaAction
    {
        Run = 0,
        SyncFlush = 1,
        FullFlush = 2,
        Finish = 3,
        FullBarrier = 4
    }
}
