namespace LambdaExec
{
    internal readonly struct EntrypointName
    {
        public string AssemblyName { get; }
        public string ClassName { get; }
        public string MethodName { get; }

        public EntrypointName(string assemblyName, string className, string methodName)
        {
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
        }

        public static EntrypointName Empty => new EntrypointName(null, null, null);

        public static bool TryParse(string s, out EntrypointName result)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                result = new EntrypointName(null, null, null);
                return false;
            }

            var parts = s.Split("::");
            if (parts.Length != 3)
            {
                result = new EntrypointName(null, null, null);
                return false;
            }

            result = new EntrypointName(parts[0], parts[1], parts[2]);
            return true;
        }
    }
}
