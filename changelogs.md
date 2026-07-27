##All changed files and reasons behind their changes:
-UserList.cs: Wrapped _Connections in TryRemove and TryAdd methods so i can ping the pubsub
-Publisher.cs: Created publisher, going to be the pub for the pubsub, mainly for updating the list of all clients connected
-MessageType.cs: Added AddUserToList and RemoveUserFromList to signal if a user was added or removed from connections list
-ProtocolMessage.cs: Adding a way create ProtocolMessage via Message type and String body, added docs aswell
-Listener.cs(server-core): Incorporated publisher
##All these i changed AddTask to not be await:
-Heartbeat.cs
-Listener.cs(Network-core)
-MainWindow.axaml.cs
-ListWindowViewModel.cs





