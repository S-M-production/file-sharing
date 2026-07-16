using format.core;

namespace client_core.core;

public class HeartBeat
{
    private double _heartBeatInterval = 10; //Seconds
    Connection connection;
    Task heartBeatTask;
    public HeartBeat(Connection connection)
    {
        this.connection = connection;
        heartBeatTask = HeartBeatLoop();
    }

    private async Task HeartBeatLoop()
    {
        while (true)
        {
            await Task.Delay((int)(_heartBeatInterval*1000));
            await connection.AddTask(new ProtocolMessage(MessageType.Ping));
        }
    }
}