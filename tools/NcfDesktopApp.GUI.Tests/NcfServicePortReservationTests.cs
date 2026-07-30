using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class NcfServicePortReservationTests
{
    [TestMethod]
    public async Task ReserveAvailablePortAsync_WhenWorkspacesStartTogether_ReturnsDistinctPorts()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var probedPort = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var startPort = Math.Min(probedPort, 65520);

        using var firstHttpClient = new HttpClient();
        using var secondHttpClient = new HttpClient();
        var firstService = new NcfService(firstHttpClient);
        var secondService = new NcfService(secondHttpClient);
        var ports = Array.Empty<int>();

        try
        {
            ports = await Task.WhenAll(
                firstService.ReserveAvailablePortAsync(startPort, startPort + 10),
                secondService.ReserveAvailablePortAsync(startPort, startPort + 10));

            Assert.AreNotEqual(ports[0], ports[1]);
        }
        finally
        {
            foreach (var port in ports)
            {
                firstService.ReleasePortReservation(port);
            }
        }
    }
}
