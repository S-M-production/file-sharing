##All changed files and reasons behind their changes:
-CallBackTask.cs: Created a record in network core that creates a TaskCompletionSource to notify whatever added the task that the message was sent
-Connection.cs: Changed the channels wrap around ProtocolMessage to CallBackTask





