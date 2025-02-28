using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Ahsoka.Test;

public class LinearTestBase
{
    private static readonly object SyncRoot = new object();

    [TestInitialize]
    public void Initialize()
    {
        Monitor.Enter(SyncRoot);
        OnTestInit();
    }

    [TestCleanup]
    public void Cleanup()
    {
        OnTestCleanup();
        Monitor.Exit(SyncRoot);
    }

    protected virtual void OnTestInit()
    {

    }

    protected virtual void OnTestCleanup()
    {

    }
}
