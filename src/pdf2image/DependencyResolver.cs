using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Ionic.Zip;

namespace pdf2image
{

    public enum DependencyType
    {
        ImageMagick,
        GhostScript
    }

    static class DependencyResolver
    {

        private static readonly string Cwd = AppDomain.CurrentDomain.BaseDirectory;

        private static ManualResetEvent WaitHandle;

        public static void DownloadDependency(DependencyType dependencyType)
        {

            WaitHandle = new ManualResetEvent(false);

            // type of dependency to download
            string name;
            string filetype;
            string dependencyDir = null;
            Uri url;

            switch (dependencyType)
            {
                case DependencyType.ImageMagick:
                    name = "ImageMagick";
                    filetype = "zip";
                    dependencyDir = Path.Combine(Cwd, "ImageMagick");
                    url = new Uri("https://www.nuget.org/api/v2/package/Magick.NET-Q16-x86/");
                    break;
                case DependencyType.GhostScript:
                    name = "GhostScript";
                    filetype = "exe";
                    url = new Uri("http://downloads.ghostscript.com/public/gs907w32.exe");
                    break;
                default:
                    throw new NotSupportedException("Unknown dependecy type");
            }

            // delete previous archives
            string filename = Path.Combine(Cwd, string.Format("{0}.{1}", name, filetype));
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // download dependency
            Console.WriteLine("{0} not found. Please wait while downloading...", name);
            using (var wc = new WebClient())
            {
                int p = 0;
                wc.DownloadProgressChanged +=
                    (sender, args) =>
                        {
                            if (args.ProgressPercentage == p) return;
                            Console.WriteLine("Downloaded {0} of 100%.", args.ProgressPercentage);
                            p = args.ProgressPercentage;
                        };
                wc.DownloadFileCompleted += (sender, args) =>
                    {
                        Console.WriteLine("Finished downloading {0}.", name);
                        switch (dependencyType)
                        {
                            case DependencyType.ImageMagick:
                                InstallImageMagick(filename, dependencyDir);
                                break;
                            case DependencyType.GhostScript:
                                InstallGhostScript(filename);
                                break;
                        }
                    };
                Console.WriteLine("Starting to download...");
                wc.DownloadFileAsync(url, filename);
                WaitHandle.WaitOne(Timeout.Infinite);
                
            }
        }

        public static void InstallImageMagick(string filename, string outputPath)
        { 
            Console.WriteLine("Unpacking imagemagick nuget package...");

            const string dirnet20 = "net20";
            const string dirxml = "xml";

            using (ZipFile zip = ZipFile.Read(filename))
            {
                var directories =
                    zip.Entries.Where(
                        x => x.FileName.StartsWith("ImageMagick/" + dirnet20) || x.FileName.StartsWith("ImageMagick/" + dirxml));

                Directory.CreateDirectory(outputPath);

                foreach (var e in directories)
                {
                    e.Extract(outputPath, ExtractExistingFileAction.OverwriteSilently);
                }

            }

            foreach (var file in Directory.GetFiles(Path.Combine(outputPath, "ImageMagick\\" + dirnet20), "*.dll"))
            {
// ReSharper disable AssignNullToNotNullAttribute
                File.Move(file, Path.Combine(outputPath, Path.GetFileName(file.Replace("%2B", "+"))));
// ReSharper restore AssignNullToNotNullAttribute
            }

            foreach (var file in Directory.GetFiles(Path.Combine(outputPath, "ImageMagick\\" + dirxml), "*.xml"))
            {
// ReSharper disable AssignNullToNotNullAttribute
                File.Move(file, Path.Combine(outputPath, Path.GetFileName(file)));
// ReSharper restore AssignNullToNotNullAttribute
            }
            
            Console.WriteLine("Finished unpacking imagemagick...");
            File.Delete(filename);

            WaitHandle.Set();
        }

        public static void InstallGhostScript(string filename)
        {
            Console.WriteLine("Installing ghostscript...");

            var i = new ProcessStartInfo(filename, "/S");
            var p = Process.Start(i);
            p.WaitForExit(Timeout.Infinite);
            var success = p.ExitCode == 0;

            Console.WriteLine(!success
                                  ? "Failed to install ghostscript 32-bit (exit code: " + p.ExitCode + ")..."
                                  : "Successfully installed ghostscript 32-bit...");

            File.Delete(filename);
            WaitHandle.Set();

        }


    }
}
