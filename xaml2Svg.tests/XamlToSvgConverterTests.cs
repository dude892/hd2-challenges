using System.IO;
using System.Xml.Linq;
using Hd2Xaml2Svg;
using Xunit;

namespace Hd2Xaml2Svg.Tests;

public sealed class XamlToSvgConverterTests
{
    [Fact]
    public void ConvertsSupportedCanvasShapesAndPreservesAlphaAndTransforms()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string inputPath = temporaryDirectory.WriteInput("stratagem_icons.xaml", """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <DataTemplate x:Key="StratagemSample">
                <Viewbox><Canvas Width="20" Height="10">
                  <Rectangle Width="20" Height="10" Fill="#80FF0000" Canvas.Left="2" />
                  <Ellipse Width="8" Height="4" Fill="Blue" />
                  <Polygon Points="0,0 1,1 2,0" Fill="Green" />
                  <Path Data="F1M 0,0 L 10,10" Fill="White" />
                </Canvas></Viewbox>
              </DataTemplate>
            </ResourceDictionary>
            """);

        XamlToSvgConverter.ProcessXamlFile(inputPath, temporaryDirectory.Path);

        string svgPath = System.IO.Path.Combine(temporaryDirectory.Path, "stratagem", "Sample.svg");
        XDocument document = XDocument.Load(svgPath);
        XNamespace svg = "http://www.w3.org/2000/svg";
        XElement root = document.Root!;

        Assert.Equal("20", root.Attribute("width")?.Value);
        Assert.Equal("0 0 20 10", root.Attribute("viewBox")?.Value);
        Assert.Equal(4, root.Elements().Count());

        XElement rectangle = root.Element(svg + "g")!.Element(svg + "rect")!;
        Assert.Equal("#FF0000", rectangle.Attribute("fill")?.Value);
        Assert.Equal("0.502", rectangle.Attribute("fill-opacity")?.Value);
        Assert.Equal("translate(2 0)", root.Element(svg + "g")?.Attribute("transform")?.Value);
        Assert.Equal("4", root.Element(svg + "ellipse")?.Attribute("rx")?.Value);
        Assert.Equal("2", root.Element(svg + "ellipse")?.Attribute("ry")?.Value);
        Assert.Equal("0,0 1,1 2,0", root.Element(svg + "polygon")?.Attribute("points")?.Value);
        Assert.Equal("M 0,0 L 10,10", root.Element(svg + "path")?.Attribute("d")?.Value);
        Assert.Equal("nonzero", root.Element(svg + "path")?.Attribute("fill-rule")?.Value);
    }

    [Fact]
    public void RemovesStaleSvgWhenTemplateHasNoSupportedShapes()
    {
        using TemporaryDirectory temporaryDirectory = new();
        string inputPath = temporaryDirectory.WriteInput("booster_icons.xaml", """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <DataTemplate x:Key="BoosterSample"><Viewbox><Canvas><TextBlock Text="Unsupported" /></Canvas></Viewbox></DataTemplate>
            </ResourceDictionary>
            """);
        string outputDirectory = System.IO.Path.Combine(temporaryDirectory.Path, "booster");
        Directory.CreateDirectory(outputDirectory);
        string staleSvgPath = System.IO.Path.Combine(outputDirectory, "Sample.svg");
        File.WriteAllText(staleSvgPath, "stale");

        XamlToSvgConverter.ProcessXamlFile(inputPath, temporaryDirectory.Path);

        Assert.False(File.Exists(staleSvgPath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory().FullName;

        public string WriteInput(string fileName, string content)
        {
            string filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

public sealed class SvgGalleryGeneratorTests
{
    [Fact]
    public void GalleryEncodesFolderAndFileNamesAndSkipsEmptyFolders()
    {
        string root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string populatedFolder = System.IO.Path.Combine(root, "icons&items");
            string emptyFolder = System.IO.Path.Combine(root, "empty");
            Directory.CreateDirectory(populatedFolder);
            Directory.CreateDirectory(emptyFolder);
            File.WriteAllText(System.IO.Path.Combine(populatedFolder, "A&B.svg"), "<svg />");

            SvgGalleryGenerator.GenerateGalleries(root);

            string gallery = File.ReadAllText(System.IO.Path.Combine(populatedFolder, "index.html"));
            Assert.Contains("icons&amp;items SVG Gallery", gallery);
            Assert.Contains("A%26B.svg", gallery);
            Assert.Contains("A&amp;B", gallery);
            Assert.False(File.Exists(System.IO.Path.Combine(emptyFolder, "index.html")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
