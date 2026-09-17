using format.core;
using System.Text;

namespace server_core.logic;

/// <summary>
/// Handles removal of disconnected users from the user list.
/// </summary>
public class UserRemoval
{
    /// <summary>
    /// Reference to the concurrent user list for removing entries
    /// </summary>
    private readonly UserList _userlist;
    
    /// <summary>
    /// Initializes UserRemoval with a reference to the user list.
    /// </summary>
    /// <param name="userList">The UserList instance to remove users from</param>
    public UserRemoval(UserList userList)
    {
        _userlist = userList;
    }
    
    /// <summary>
    /// Extracts user endpoint from message body and removes them from the user list.
    /// Conforms to MessageHandler delegate signature for use in RouterMap.
    /// </summary>
    /// <param name="protocolMessage">The disconnect message containing the user endpoint in its body</param>
    /// <returns>Always returns null as removal is a side-effect with no response</returns>
    public ProtocolMessage? Remove(ProtocolMessage protocolMessage)
    {
        var temptext = Encoding.UTF8.GetString(protocolMessage.Body);
        _userlist.TryRemove(temptext, out _);
        return null;
    }
}