using System.Runtime.CompilerServices;
using NServiceBus.Heartbeat;
using NServiceBus.Heartbeat.Tests;
using NUnit.Framework;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Approve()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(HeartbeatSender).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
        TestApprover.Verify(publicApi);
    }
}