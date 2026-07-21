using System.Net;
using format.core;
using client_core.middleware;


namespace client_core.router.logic;

public class UserListCallBack
{
    private MessageHandler UserListCall(ProtocolMessage message)
    {
        UserInfoList.GetUserInfoList(message,out var userInfos);
        //TODO
    }
}