##All changed files and reasons behind their changes:
-MainWindowViewModel.cs: Added a stopwatch to record how fast connection happens
-Connector.cs: Removed Old connect Method, Renamed ConnectToServer Connect and used Cancelation token to speed up connectivity. Instead of awaiting 1 second and checking, it now automatically connects if its within Timeout, if its not it throws a error

-





