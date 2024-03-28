using ImageMagick;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MarquitoUtils.Main.Class.Tools;
using Microsoft.SqlServer.Management.Smo;
using MarquitoUtils.Main.Class.Service.Threading;
using System.Windows.Threading;

namespace HEIC_JPEGConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IThreadingService ThreadingService { get; set; } = new ThreadingService();
        private int NumberOfThreads { get; set; } = 8;

        private int FilesProcessed { get; set; } = 0;
        private int FilesToProcess { get; set; } = 0;
        private Thread ConversionProcessThread { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string directory = this.GetOriginDirectory();

            if (Utils.IsNotEmpty(directory))
            {
                List<string> allHEICFiles = Directory.GetFiles(directory, "*.heic", SearchOption.AllDirectories)
                    .Where(file => !file.Contains(@"/old_HEIC/"))
                    .ToList();

                this.FilesToProcess = allHEICFiles.Count();

                int cpt = 0;

                this.ResetConversionProgression();

                this.ConversionProcessThread = this.ThreadingService
                    .PartitionDataProcess(this.NumberOfThreads, allHEICFiles, (files) =>
                    {
                        this.ConvertPictures(files);
                    }, () =>
                    {
                        this.UpdateConversionProgression();
                        this.FilesProcessed = 0;
                        this.FilesToProcess = 0;
                    });
            }
        }

        private string GetOriginDirectory()
        {
            string originDirectory = "";

            OpenFolderDialog folderDialog = new OpenFolderDialog();

            bool? result = folderDialog.ShowDialog();
            if (result == true)
            {
                originDirectory = folderDialog.FolderName;
            }

            return originDirectory;
        }

        private void ConvertPicture(string originFile)
        {
            FileInfo info = new FileInfo(originFile);

            string directoryName = $@"{info.Directory.FullName}\old_HEIC";
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (MagickImage image = new MagickImage(info.FullName))
            {
                image.Write(@$"{info.Directory.FullName}\{info.Name.Replace(".HEIC", "")}.jpg");
            }

            File.Move(info.FullName, $@"{directoryName}/{info.Name}");
        }

        private void ConvertPictures(List<string> HEIPictures)
        {
            foreach (string file in HEIPictures)
            {
                this.ConvertPicture(file);

                this.FilesProcessed++;

                this.UpdateConversionProgression();
            }
        }

        private void UpdateConversionProgression()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                double progress = (this.FilesProcessed * 100) / this.FilesToProcess;
                this.PbConversion.Value = progress;

                this.LblProgress.Text = $"{this.FilesProcessed} images processed out of {this.FilesToProcess} ({progress}%)";
            }));
        }

        private void ResetConversionProgression()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                this.PbConversion.Value = 0;

                this.LblProgress.Text = "";
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Utils.IsNotNull(this.ConversionProcessThread))
            {
                this.ConversionProcessThread.Interrupt();
            }
        }
    }
}