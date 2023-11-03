dotnet pack -c Release
dotnet nuget push bin/Release/Lails.MQ.Rabbit.1.0.0.nupkg -k ______ -s https://api.nuget.org/v3/index.json

pause