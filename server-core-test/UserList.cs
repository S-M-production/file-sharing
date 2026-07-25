// using System.Text;
// using System.Text.Json;
// using server_core.core;
// using server_core.logic;
// using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
// using CollectionAssert = Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
//
// namespace format.formatTest;
//
// [TestClass]
// public class UserListTest
// {
//     [TestMethod]
//     public void Serialize_ReturnsJsonRepresentationOfAllEntries()
//     {
//         var connections = new UserList();
//         connections.Connections.TryAdd("10.0.0.1:5000",new Worker());
//         connections.Connections.TryAdd("10.0.0.2:6000",new Worker());
//
//         byte[] payload = connections.Serialize();
//         string json = Encoding.UTF8.GetString(payload);
//         List<string>? entries = JsonSerializer.Deserialize<List<string>>(json);
//
//         Assert.IsNotNull(entries);
//         CollectionAssert.AreEquivalent(new[] { "10.0.0.1:5000", "10.0.0.2:6000" }, entries.ToArray());
//     }
// }