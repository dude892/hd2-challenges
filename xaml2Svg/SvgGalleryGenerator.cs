using System.IO;

namespace Hd2Xaml2Svg
{
    public static class SvgGalleryGenerator
    {
        public static void GenerateGalleries(string outputRoot)
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
    }
}
