using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageMagick;
using NDesk.Options;

namespace pdf2image
{
    class Program
    {


        private static readonly string Cwd = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ImageMagickDir = Path.Combine(Cwd, @"ImageMagick");

        public static string ProcessName
        {
            get { return Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName); }
        }

        static void Main(string[] args)
        {

            if (!Directory.Exists(ImageMagickDir))
            {
                DependencyResolver.DownloadDependency(DependencyType.ImageMagick);
            }

            var gsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "gs");
            if (!Directory.Exists(gsPath))
            {
                DependencyResolver.DownloadDependency(DependencyType.GhostScript);
            }

            string filename = null;
            string outputPrefix = null;
            string outputDirectory = null;
            string outputFormat = null;
            Tuple<int, int> pageRange = null;
            string searchFormat = null;
            bool showAllFormats = false;
            bool help = false;
            Tuple<int, int> dpi = null;
            Tuple<int, int> geometry = null;

            var o = new OptionSet
                {
                    {"file=", "pdf to convert", v => filename = v},
                    {"format=", "output format after pdf is converted", v => outputFormat = v },
                    {"page=", "convert specific page [0-3] or 3", v => pageRange = ParseRange(v) },
                    {"density|dpi=", "density (ex: 300x300)", v => dpi = ParseXY(v) },
                    {"geometry=", "width/height (ex: 800x600)", v => geometry = ParseXY(v) },
                    {"output-prefix=", "prefix output images", v => outputPrefix = v },
                    {"output-dir=", "conversion output directory", v => outputDirectory = v },
                    {"search-formats=", "search available output formats", v => searchFormat = v },
                    {"list-formats", "display available output formats", v => showAllFormats = true },
                    {"h|?|help", "show this message and exit",  v => help = v != null },
                };

            try
            {
                o.Parse(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: ", ProcessName);
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `{0} --help` for more information", ProcessName);
                return;
            }

            if (!string.IsNullOrEmpty(searchFormat) || showAllFormats)
            {
                ShowAvailableFormats(showAllFormats ? null : searchFormat);
                return;
            }

            if (help || string.IsNullOrEmpty(filename))
            {
                ShowHelp(o);
                return;
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }


            MagickNET.Initialize(ImageMagickDir);

            Convert(filename, outputPrefix, outputDirectory, outputFormat, pageRange, dpi, geometry);

        }

        private static Tuple<int, int> ParseRange(string strRange)
        {
            int x;
            if (strRange.StartsWith("[") &&
                strRange.EndsWith("]") &&
                strRange.Contains("-"))
            {
                var range = strRange.Split('-');
                if (range.Length == 2)
                {
                    int.TryParse(range[0], out x);
                    int y;
                    int.TryParse(range[1], out y);
                    return new Tuple<int, int>(x, y);
                }
                return null;
            }
            int.TryParse(strRange, out x);
            return new Tuple<int, int>(x, x);
        }

        private static Tuple<int, int> ParseXY(string xy)
        {
            if (!xy.Contains("x"))
            {
                return null;
            }
            int x, y;
            var s = xy.Split('x');
            if (s.Length != 2)
            {
                return null;
            }
            int.TryParse(s[0], out x);
            int.TryParse(s[1], out y);
            return new Tuple<int, int>(x, y);
        }

        private static void ShowHelp(OptionSet o)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]", ProcessName);
            Console.WriteLine("Convert a pdf to images.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            o.WriteOptionDescriptions(Console.Out);
        }

        private static void ShowAvailableFormats(string searchFormat)
        {
            searchFormat = searchFormat != null ? searchFormat.ToLowerInvariant() : null;
            IEnumerable<MagickFormatInfo> formats = !string.IsNullOrEmpty(searchFormat) ?
                MagickNET.SupportedFormats.Where(x => x.Format.ToString().ToLowerInvariant().Contains(searchFormat) && x.IsWritable) :
                MagickNET.SupportedFormats.Where(x => x.IsWritable);
            var magickFormatInfos = formats as IList<MagickFormatInfo> ?? formats.ToList();
            int formatsDiscovered = magickFormatInfos.Count;
            if (!string.IsNullOrEmpty(searchFormat))
            {
                if (formatsDiscovered > 0)
                {
                    Console.WriteLine("Listing {0} available writeable formats matches for query: {1}", formatsDiscovered, searchFormat);
                    Console.WriteLine("================================================");
                }
                else
                {
                    Console.WriteLine("Unable to find any formats for query: {0}", searchFormat);
                }
            }
            else
            {
                Console.WriteLine("Listing {0} available writeable formats", formatsDiscovered);
                Console.WriteLine("=======================");
            }
            foreach (var imFormat in magickFormatInfos)
            {
                Console.WriteLine(imFormat.Format.ToString().ToLowerInvariant());
            }
        }

        private static bool IsSupportedFormat(string format)
        {
            format = format.ToLowerInvariant();
            return MagickNET.SupportedFormats.Any(x => x.Format.ToString().ToLowerInvariant().Contains(format) && x.IsWritable);
        }

        private static void Convert(string filename, string outputPrefix, string outputDirectory, string outputFormat,
            Tuple<int, int> pageRange, Tuple<int, int> dpi, Tuple<int, int> geometry)
        {

            if (!File.Exists(filename))
            {
                Console.WriteLine("Unable to find file: {0}", filename);
                return;
            }

            filename = Path.GetFileName(filename);
            var ext = Path.GetExtension(filename);
            if (ext != ".pdf" && ext != ".pdfa")
            {
                Console.WriteLine("Invalid input format: {0}", ext);
                return;
            }

            if (!Directory.Exists(outputDirectory))
            {
                Console.WriteLine("Cannot find output directory: {0}", outputDirectory);
                return;
            }

            if (string.IsNullOrEmpty(outputFormat))
            {
                outputFormat = "png";
            }

            if (!IsSupportedFormat(outputFormat))
            {
                Console.WriteLine("Unsupported output format: {0}", outputFormat);
                return;
            }

            using (var collection = new MagickImageCollection())
            {
                Console.WriteLine("Reading {0}", filename);

                try
                {
                    var settings = new MagickReadSettings();
                    if (dpi != null)
                    {
                        settings.Density = new MagickGeometry(dpi.Item1, dpi.Item2);
                    }
                    if (geometry != null)
                    {
                        settings.Width = geometry.Item1;
                        settings.Height = geometry.Item2;
                    }
                    collection.Read(filename, settings);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to read file: {0}", ex);
                }

                Console.WriteLine("Read {0} pages", collection.Count);

                int x = pageRange == null ? 0 : pageRange.Item1;
                int y = pageRange == null ? collection.Count : pageRange.Item2;

                int index = 0;
                foreach (var page in collection)
                {
                    if (!index.Between(x, y))
                    {
                        break;
                    }

                    try
                    {
                        Console.WriteLine("Converting page {0} to {1}", index, page.Format);

                        page.Write(Path.Combine(outputDirectory, string.Format("{0}{1}.{2}", outputPrefix, index, outputFormat)));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to convert page on index {0}. Reason: {1}", index, ex.Message);
                    }

                    index++;
                }

            }

        }

    }
}
