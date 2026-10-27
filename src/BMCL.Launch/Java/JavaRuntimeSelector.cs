namespace BMCL.Launch.Java;

public static class JavaRuntimeSelector
{
    public static JavaRuntime? SelectCompatible(
        IEnumerable<JavaRuntime> runtimes,
        int requiredMajorVersion) =>
        runtimes
            .Where(runtime => runtime.MajorVersion >= requiredMajorVersion)
            .MinBy(runtime => runtime.MajorVersion);
}
