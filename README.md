# CollageMaker

`CollageMaker` is a .NET desktop-oriented solution that builds a photo collage from random images in an Immich person album and can optionally set the result as the Windows desktop background.

## Solution Structure

- `CollageMaker` - main app (configuration, Immich integration, image composition, wallpaper update)
- `JustifiedLayout` - C# port of Flickr's Justified Layout, used to place images in rows
- `CollageMaker.Tests` - unit and regression tests

## Requirements

- .NET SDK 10
- Windows (target framework: `net10.0-windows10.0.17763.0`)
- Access to an Immich instance and API key

## Configuration

Configuration is loaded from:

1. `CollageMaker/appsettings.json`
2. User secrets (recommended for sensitive values)

User secrets override `appsettings.json`.

### Immich settings (required)

- `Immich:BaseUrl` - Base URL of your Immich server (for example `https://immich.example.com`).
- `Immich:ApiKey` - Immich API key used to authenticate requests.
- `Immich:PersonId` - Person identifier used when fetching random images.
- `Immich:ImageCount` - Number of images to request (valid range: `2-30`).

### Output settings (common)

- `Output:Width` - Final collage width in pixels.
- `Output:Height` - Final collage height in pixels.
- `Output:Format` - Output image format (`png` or `jpeg`).
- `Output:OutputPath` - Path/filename for the generated collage.
- `Output:ImageSpacing` - Spacing in pixels between images.
- `Output:FetchAdditionalForCoverage` - If `true`, app can fetch a few extra images when initial layout coverage is poor.
- `Output:MaxAdditionalImagesForLastRow` - Upper bound for additional images fetched during coverage improvement.
- `Output:SaveDownloadedImages` - If `true`, also saves the downloaded source images for debugging.
- `Output:DownloadedImagesDirectory` - Directory used when `SaveDownloadedImages` is enabled.
- `Output:SetAsDesktopBackground` - If `true`, sets the generated collage as Windows wallpaper.
- `Output:RunInvisibly` - If `true`, does not show a console window during execution.

## Using User Secrets

From the solution root:

```powershell
dotnet user-secrets --project .\CollageMaker\CollageMaker.csproj set "Immich:BaseUrl" "https://your-immich-url"
dotnet user-secrets --project .\CollageMaker\CollageMaker.csproj set "Immich:ApiKey" "your-api-key"
dotnet user-secrets --project .\CollageMaker\CollageMaker.csproj set "Immich:PersonId" "your-person-id"
```

You can verify current secrets with:

```powershell
dotnet user-secrets --project .\CollageMaker\CollageMaker.csproj list
```

## Build and Run

From the solution root:

```powershell
dotnet build
```

Run the app:

```powershell
dotnet run --project .\CollageMaker\CollageMaker.csproj
```

## Run Tests

```powershell
dotnet test
```

## Notes

- Logging uses `Serilog`, and sinks/output are configured in `CollageMaker/appsettings.json` (for example console and rolling file output).