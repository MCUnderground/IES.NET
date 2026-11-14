# IES.NET

IES.NET parses IES LM-63 photometric files and renders the photometric data to a WPF bitmap.

## Requirements

- .NET 9
- Windows
- A WPF application

The renderer returns a `BitmapSource`, so it is intended for use in WPF applications.

## Install

Install the package from NuGet:

```powershell
dotnet add package MCUnderground.IES.NET
```

## Usage

Parse an IES file and assign the rendered bitmap to a WPF `Image` control:

```xml
<Image x:Name="PhotometricImage" Stretch="Uniform" />
```

```csharp
using IES.NET.Options;
using IES.NET.Parsing;
using IES.NET.Rendering;

var parser = new IesParser();
var data = parser.Parse(@"C:\Photometry\fixture.ies");

var bitmap = IesBitmapRenderer.RenderToBitmapSource(
    data,
    new RenderOptions
    {
        Width = 512,
        Height = 512
    });

PhotometricImage.Source = bitmap;
```

`RenderOptions` can be used to adjust the image dimensions, viewing angle, exposure, gamma, rotation, blur, and bloom.

`IesParser.Parse` reads a file from disk and returns an `IesData` instance containing the vertical angles, horizontal angles, and candela values. It throws `FileNotFoundException` if the path does not exist and `IesParsingException` if the file cannot be parsed.

## License

IES.NET is distributed under the MIT license. See [LICENSE](LICENSE).
