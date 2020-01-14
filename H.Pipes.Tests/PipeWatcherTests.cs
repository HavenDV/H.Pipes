using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Pipes.Tests
{
    [TestClass]
    public class PipeWatcherTests
    {
        [TestMethod]
        public void IsExistsTest()
        {
            Assert.IsFalse(PipeWatcher.IsExists("this_pipe_100%_is_not_exists"));
        }
    }
}
