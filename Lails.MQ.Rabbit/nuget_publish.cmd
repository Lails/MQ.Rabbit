dotnet pack -c Release
dotnet nuget push bin/Release/Lails.MQ.Rabbit.8.0.0.nupkg -k 6B29FC4075C4CB3C-C9C0-4419-B6d01e85-2420-4b0c-b162-a25ca267c13b -s http://localhost:2000/v3/index.json


pause