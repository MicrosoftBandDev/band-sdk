# Microsoft Band SDKs (band-sdk)
A modernized version of the Microsoft Band SDKs for Microsoft Band 1/2

## Building the project
Band SDK uses .NET MAUI, which is still in the preview stages. Please follow the [installation guide in the .NET MAUI documentation](https://docs.microsoft.com/en-us/dotnet/maui/get-started/installation),
and remember that you need Visual Studio 2022 *Preview*.

Once you have the development environment set up, you're ready to clone the repo.
```bash
git clone https://github.com/MicrosoftBandDev/band-sdk.git
cd ./band-sdk
```

In the future, you may also need to run the following:
```bash
git submodule init
git submodule update
```

Once the repo has been downloaded, you can get started by opening [the BandSDK solution](https://github.com/MicrosoftBandDev/band-sdk/blob/main/src/BandSDK.sln)
in VS 2022 Preview.
