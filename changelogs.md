##All changed files and reasons behind their changes:
-Heartbeat.cs(network-core.core): Moved to server_core.core
-Connection.cs: Removed dependency on heartbeat
-HeartBeat.cs: Altered name of HeartBeatLoop, and made it start on call, and it stores its own task with public get, Set heartbeat to 1 second intervals
-MainWindowViewModel.cs: Removed HeartBeat Param





