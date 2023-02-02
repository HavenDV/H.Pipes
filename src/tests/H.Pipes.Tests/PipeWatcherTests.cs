namespace H.Pipes.Tests;

[TestClass]
public class PipeWatcherTests
{
    [TestMethod]
    public void IsExistsTest()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return;
        }
        
        Assert.IsFalse(PipeWatcher.IsExists("this_pipe_100%_is_not_exists"));
    }
}
