PLATFORM=$1

if [ "$PLATFORM" = "Windows" ]; then
  RUNTIME="win-x64"
elif [ "$PLATFORM" = "Linux" ]; then
  RUNTIME="linux-x64"
elif [ "$PLATFORM" = "Mac" ]; then
  RUNTIME="osx-x64"
else
  echo "Platform must be provided as first arguement: Windows, Linux or Mac"
  exit 1
fi

outputFolder='_output'
testPackageFolder='_tests'

rm -rf $outputFolder
rm -rf $testPackageFolder

slnFile=src/Lidarr.sln

platform=Posix

if [ "$PLATFORM" = "Windows" ]; then
  application=Lidarr.Console.dll
else
  application=Lidarr.dll
fi

dotnet clean $slnFile -c Debug
dotnet clean $slnFile -c Release

dotnet msbuild -restore $slnFile -p:Configuration=Debug -p:Platform=$platform -p:RuntimeIdentifiers=$RUNTIME -t:PublishAllRids

dotnet new tool-manifest
dotnet tool install --version 6.6.2 Swashbuckle.AspNetCore.Cli

dotnet tool run swagger tofile --output ./src/Lidarr.Api.V1/openapi.json "$outputFolder/net6.0/$RUNTIME/$application" v1 &

sleep 45

kill %1

exit 0
