namespace LambdaExec
{
    internal readonly struct LambdaManifest
    {
        public string Configuration { get; }
        public string Framework { get; }
        public int MemorySize { get; }
        public int Timeout { get; }
        public EntrypointName Entrypoint { get; }

        public LambdaManifest(string configuration, string framework, int memorySize, int timeout, EntrypointName entrypoint)
        {
            Configuration = configuration;
            Framework = framework;
            MemorySize = memorySize;
            Timeout = timeout;
            Entrypoint = entrypoint;
        }

        public static LambdaManifest Empty => new LambdaManifest(null, null, 0, 0, default);
    }
}
