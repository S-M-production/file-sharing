using format.core;
using System.Text;

namespace server_core.logic;

/// <summary>
/// used to remove a user
/// </summary>
public class UserRemoval
{
    private readonly UserList _userlist;
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="userList">stored value of user list</param>
    public UserRemoval(UserList userList)
    {
        _userlist = userList;
    }
    /// <summary>
    /// reterives the message and uses the connected body to delete instance out of dictonary
    /// </summary>
    /// <param name="protocolMessage">taking the message put in</param>
    /// <returns></returns>
    public ProtocolMessage? Remove(ProtocolMessage protocolMessage)
    {
        var temptext = Encoding.UTF8.GetString(protocolMessage.Body);
        _userlist.TryRemove(temptext, out _);
        return null;
    }
}