##All changed files and reasons behind their changes:
-Middleware.cs(server_core): Altered it so when MessageType.Disconect is recieved, the incoming packet is altered to contain the user IP:PORT in the body, so when the router parser it, it knows the IP:PORT to remove

-





