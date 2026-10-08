using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Linq;

namespace Hd2Xaml2Svg
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Xaml2Svg <input-xaml-folder> <output-svg-folder>");
                return 1;
            }

            string inputRoot = args[0];
            string outputRoot = args[1];

            if (!Directory.Exists(inputRoot))
            {
                Console.WriteLine($"Input directory does not exist: {inputRoot}");
                return 1;
            }

            Directory.CreateDirectory(outputRoot);

            foreach (var xamlPath in Directory.GetFiles(inputRoot, "*.xaml", SearchOption.AllDirectories))
            {
                try
                {
                    ProcessXamlFile(xamlPath, outputRoot);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {xamlPath}: {ex.Message}");
                }
            }

            GenerateGalleries(outputRoot);

            return 0;
        }

        private static void GenerateGalleries(string outputRoot)
        {
            var folderPaths = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories)
                .Prepend(outputRoot);

            foreach (var folderPath in folderPaths)
            {
                var svgPaths = Directory.GetFiles(folderPath, "*.svg", SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (svgPaths.Length == 0)
                    continue;

                string folderTitle = System.Net.WebUtility.HtmlEncode(Path.GetFileName(folderPath));
                var figures = svgPaths.Select(svgPath =>
                {
                    string fileName = Path.GetFileName(svgPath);
                    string encodedFileName = System.Net.WebUtility.HtmlEncode(Uri.EscapeDataString(fileName));
                    string title = System.Net.WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(svgPath));
                    return $"<figure><a href=\"{encodedFileName}\" target=\"_blank\" rel=\"noopener\"><img src=\"{encodedFileName}\" alt=\"{title}\" loading=\"lazy\"></a><figcaption>{title}</figcaption></figure>";
                });

                var html = string.Join(Environment.NewLine,
                    "<!doctype html>",
                    "<html lang=\"en\">",
                    "<head>",
                    "  <meta charset=\"utf-8\">",
                    "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">",
                    $"  <title>{folderTitle} SVG Gallery</title>",
                    "  <style>",
                    "    body { margin: 0; padding: 24px; color: #17212b; background: #eef1f3; font: 16px/1.4 system-ui, sans-serif; }",
                    "    h1 { margin: 0 0 20px; font-size: 24px; }",
                    "    main { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 12px; }",
                    "    figure { min-width: 0; margin: 0; padding: 12px; background: white; border: 1px solid #d4dbe0; }",
                    "    a { display: grid; height: 160px; place-items: center; }",
                    "    img { max-width: 100%; max-height: 100%; object-fit: contain; }",
                    "    figcaption { overflow-wrap: anywhere; margin-top: 8px; font-size: 14px; }",
                    "  </style>",
                    "</head>",
                    "<body>",
                    $"  <h1>{folderTitle} SVGs</h1>",
                    "  <main>",
                    string.Join(Environment.NewLine, figures.Select(figure => $"    {figure}")),
                    "  </main>",
                    "</body>",
                    "</html>");

                string galleryPath = Path.Combine(folderPath, "index.html");
                File.WriteAllText(galleryPath, html);
                Console.WriteLine($"[GALLERY] {galleryPath}");
            }
        }

        private static void ProcessXamlFile(string xamlPath, string outputRoot)
        {
            var doc = XDocument.Load(xamlPath);

            var rd = doc.Root;
            if (rd == null || rd.Name.LocalName != "ResourceDictionary")
            {
                Console.WriteLine($"[SKIP] {xamlPath}: not a ResourceDictionary");
                return;
            }

            string sourceFileName = Path.GetFileNameWithoutExtension(xamlPath);
            string folderName = sourceFileName.ToLowerInvariant().Substring(0, sourceFileName.IndexOf('_'));
            string folderPath = Path.Combine(outputRoot, folderName);
            Directory.CreateDirectory(folderPath);

            foreach (var dt in rd.Elements().Where(e => e.Name.LocalName == "DataTemplate"))
            {
                var keyAttr = dt.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"));
                if (keyAttr == null)
                    continue;

                string templateKey = keyAttr.Value;
                string slug = SlugifyKey(templateKey);
                string svgPath = Path.Combine(folderPath, slug + ".svg");

                var viewbox = dt.Descendants().FirstOrDefault(e => e.Name.LocalName == "Viewbox");
                var canvas = viewbox?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Canvas");

                if (canvas == null)
                {
                    Console.WriteLine($"[WARN] {xamlPath}::{templateKey}: no Canvas found");
                    continue;
                }

                double width = GetDoubleAttribute(canvas, "Width", 256);
                double height = GetDoubleAttribute(canvas, "Height", 256);

                var svgContent = BuildSvgFromCanvas(canvas, width, height);
                if (svgContent == null)
                {
                    if (File.Exists(svgPath))
                        File.Delete(svgPath);

                    Console.WriteLine($"[SKIP] {xamlPath}::{templateKey}: no supported shapes");
                    continue;
                }

                if (File.Exists(svgPath))
                    File.Delete(svgPath);

                File.WriteAllText(svgPath, svgContent);

                Console.WriteLine($"[OK] {svgPath}");
            }
        }

        private static string SlugifyKey(string key)
        {
            string s = key;
            foreach (var prefix in new[] { "Booster", "Passive", "Stratagem", "Difficulty" })
            {
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(prefix.Length);
                    break;
                }
            }

            return s;
        }

        private static string BuildSvgFromCanvas(XElement canvas, double width, double height)
        {
            var nsSvg = "http://www.w3.org/2000/svg";

            var svg = new XElement(XName.Get("svg", nsSvg),
                new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("height", height.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("viewBox", $"0 0 {width} {height}")
            );

            foreach (var child in canvas.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "Path":
                        svg.Add(ConvertPath(child, nsSvg));
                        break;
                    case "Rectangle":
                        svg.Add(ConvertRectangle(child, nsSvg));
                        break;
                    case "Ellipse":
                        svg.Add(ConvertEllipse(child, nsSvg));
                        break;
                    case "Polygon":
                        svg.Add(ConvertPolygon(child, nsSvg));
                        break;
                }
            }

            if (!svg.HasElements)
                return null;

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), svg);
            using var sw = new StringWriter();
            doc.Save(sw);
            return sw.ToString();
        }

        private static XElement ConvertPath(XElement path, string nsSvg)
        {
            var svgPath = new XElement(XName.Get("path", nsSvg));

            var dataAttr = path.Attribute("Data");
            if (dataAttr != null)
            {
                var normalized = NormalizePathData(dataAttr.Value);
                svgPath.SetAttributeValue("d", normalized);
                svgPath.SetAttributeValue("fill-rule", GetSvgFillRule(dataAttr.Value));
            }

            CopyColorAttribute(path, svgPath, "Fill", "fill", "fill-opacity");
            CopyColorAttribute(path, svgPath, "Stroke", "stroke", "stroke-opacity");
            CopyAttribute(path, svgPath, "StrokeThickness", "stroke-width");

            return ApplyTransforms(path, ApplyPathStretch(path, svgPath));
        }

        private static string NormalizePathData(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return data;

            data = data.Trim();

            if (data.StartsWith("F1", StringComparison.OrdinalIgnoreCase) ||
                data.StartsWith("F0", StringComparison.OrdinalIgnoreCase))
                data = data.Substring(2);

            return data;
        }

        private static string GetSvgFillRule(string data)
        {
            data = data.TrimStart();
            return data.StartsWith("F1", StringComparison.OrdinalIgnoreCase) ? "nonzero" : "evenodd";
        }

        private static XElement ApplyPathStretch(XElement path, XElement content)
        {
            var stretchAttr = path.Attribute("Stretch");
            var dataAttr = path.Attribute("Data");
            var widthAttr = path.Attribute("Width");
            var heightAttr = path.Attribute("Height");

            if (stretchAttr == null || !string.Equals(stretchAttr.Value, "Fill", StringComparison.OrdinalIgnoreCase) ||
                dataAttr == null || widthAttr == null || heightAttr == null ||
                !double.TryParse(widthAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !double.TryParse(heightAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                return content;
            }

            var bounds = Geometry.Parse(NormalizePathData(dataAttr.Value)).Bounds;
            if (bounds.IsEmpty || bounds.Width == 0 || bounds.Height == 0)
                return content;

            double scaleX = width / bounds.Width;
            double scaleY = height / bounds.Height;
            double offsetX = -bounds.X * scaleX;
            double offsetY = -bounds.Y * scaleY;
            string transform = $"matrix({FormatNumber(scaleX)} 0 0 {FormatNumber(scaleY)} {FormatNumber(offsetX)} {FormatNumber(offsetY)})";

            return WrapInTransform(content, transform);
        }

        private static XElement ApplyTransforms(XElement src, XElement content)
        {
            string matrix = BuildMatrixTransform(src);
            if (matrix != null)
                content = WrapInTransform(content, matrix);

            string translate = BuildCanvasTranslate(src);
            if (translate != null)
                content = WrapInTransform(content, translate);

            return content;
        }

        private static XElement WrapInTransform(XElement content, string transform)
        {
            return new XElement(XName.Get("g", "http://www.w3.org/2000/svg"),
                new XAttribute("transform", transform),
                content);
        }

        private static string BuildCanvasTranslate(XElement src)
        {
            var leftAttr = src.Attribute("Canvas.Left");
            var topAttr = src.Attribute("Canvas.Top");

            if (leftAttr == null && topAttr == null)
                return null;

            double.TryParse(leftAttr?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var left);
            double.TryParse(topAttr?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var top);

            return $"translate({FormatNumber(left)} {FormatNumber(top)})";
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string BuildMatrixTransform(XElement src)
        {
            var rt = src.Element(XName.Get("Path.RenderTransform", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")) ??
                     src.Element(XName.Get("RenderTransform", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"));

            if (rt == null)
                return null;

            var mt = rt.Element(XName.Get("MatrixTransform", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"));
            if (mt == null)
                return null;

            var matrixAttr = mt.Attribute("Matrix");
            if (matrixAttr == null)
                return null;

            var parts = matrixAttr.Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
                return null;

            return $"matrix({string.Join(" ", parts)})";
        }

        private static void CopyColorAttribute(XElement src, XElement dst, string srcName, string dstFillName, string dstOpacityName)
        {
            var attr = src.Attribute(srcName);
            if (attr == null)
            {
                if (dstFillName == "fill")
                    dst.SetAttributeValue(dstFillName, "none");
                return;
            }

            var value = attr.Value.Trim();

            if (value.StartsWith("#") && value.Length == 9)
            {
                var a = value.Substring(1, 2);
                var r = value.Substring(3, 2);
                var g = value.Substring(5, 2);
                var b = value.Substring(7, 2);

                dst.SetAttributeValue(dstFillName, $"#{r}{g}{b}");

                if (byte.TryParse(a, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var alphaByte))
                {
                    var opacity = alphaByte / 255.0;
                    dst.SetAttributeValue(dstOpacityName, opacity.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }
            else
            {
                dst.SetAttributeValue(dstFillName, value);
            }
        }

        private static XElement ConvertRectangle(XElement rect, string nsSvg)
        {
            var svgRect = new XElement(XName.Get("rect", nsSvg));

            CopyAttribute(rect, svgRect, "Width", "width");
            CopyAttribute(rect, svgRect, "Height", "height");
            CopyColorAttribute(rect, svgRect, "Fill", "fill", "fill-opacity");
            CopyColorAttribute(rect, svgRect, "Stroke", "stroke", "stroke-opacity");
            CopyAttribute(rect, svgRect, "StrokeThickness", "stroke-width");

            return ApplyTransforms(rect, svgRect);
        }

        private static XElement ConvertEllipse(XElement ellipse, string nsSvg)
        {
            var svgEllipse = new XElement(XName.Get("ellipse", nsSvg));

            var widthAttr = ellipse.Attribute("Width");
            var heightAttr = ellipse.Attribute("Height");

            if (widthAttr != null &&
                double.TryParse(widthAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
            {
                svgEllipse.SetAttributeValue("rx", (w / 2).ToString(CultureInfo.InvariantCulture));
            }

            if (heightAttr != null &&
                double.TryParse(heightAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                svgEllipse.SetAttributeValue("ry", (h / 2).ToString(CultureInfo.InvariantCulture));
            }

            CopyColorAttribute(ellipse, svgEllipse, "Fill", "fill", "fill-opacity");
            CopyColorAttribute(ellipse, svgEllipse, "Stroke", "stroke", "stroke-opacity");
            CopyAttribute(ellipse, svgEllipse, "StrokeThickness", "stroke-width");

            return ApplyTransforms(ellipse, svgEllipse);
        }

        private static XElement ConvertPolygon(XElement poly, string nsSvg)
        {
            var svgPoly = new XElement(XName.Get("polygon", nsSvg));

            CopyAttribute(poly, svgPoly, "Points", "points");
            CopyColorAttribute(poly, svgPoly, "Fill", "fill", "fill-opacity");
            CopyColorAttribute(poly, svgPoly, "Stroke", "stroke", "stroke-opacity");
            CopyAttribute(poly, svgPoly, "StrokeThickness", "stroke-width");

            return ApplyTransforms(poly, svgPoly);
        }

        private static void CopyAttribute(XElement src, XElement dst, string srcName, string dstName)
        {
            var attr = src.Attribute(srcName);
            if (attr != null)
                dst.SetAttributeValue(dstName, attr.Value);
        }

        private static double GetDoubleAttribute(XElement element, string name, double fallback)
        {
            var attr = element.Attribute(name);
            if (attr == null)
                return fallback;

            if (double.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;

            return fallback;
        }
    }
}
