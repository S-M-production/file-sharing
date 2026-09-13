using System.Net;
using System.Net.Sockets;
using System.Text;
using format.core;

namespace format.formatTest;

[TestClass]
public class ParserTest
{
    [TestMethod]
    public void ProtocolMessage_ExpectedSignatureAndLengthAboveZero_ReturnsCorrectMessage()
    {
        // Arrange
        var sig = Encoding.ASCII.GetBytes("CNSR");
        var messageType = BitConverter.GetBytes(1);
        var length = BitConverter.GetBytes(5);
        var body = Encoding.ASCII.GetBytes("Hello");
        byte[] correctBites = sig
            .Concat(messageType)
            .Concat(length)
            .Concat(body)
            .ToArray();
        
        // Act
        using var listner = new TcpListener(IPAddress.Loopback, 0);
        listner.Start();

        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, ((IPEndPoint)listner.LocalEndpoint).Port);

        using var server = listner.AcceptTcpClient();
        using var serverStream = server.GetStream();

        var clientStream = client.GetStream();
        clientStream.Write(correctBites, 0, correctBites.Length);
        clientStream.Flush();

        var parser = new Parser(serverStream);
        //TODO: CHange the tests to acount for the change in ProtocolMessage Record
        // ProtocolMessage message = await parser.Parse();
        // // Assert
        // Console.WriteLine(message);
        // Assert.IsNotNull(message);
        // //Todo Alter this for Async
        // Assert.AreEqual("CNSR", Encoding.ASCII.GetString(message.Id));
        // Assert.AreEqual(5, message.Length);
        // CollectionAssert.AreEqual(body, message.Body);
    }
}