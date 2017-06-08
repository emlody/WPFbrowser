using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;

namespace WPFbrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string gpath = null;
        bool file = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadDirectories();
        }

        public void LoadDirectories()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                this.treeView.Items.Add(this.GetItem(drive));
            }
        }

        private TreeViewItem GetItem(DriveInfo drive)
        {
            var item = new TreeViewItem
            {
                Header = drive.Name,
                DataContext = drive,
                Tag = drive
            };
            this.AddDummy(item);
            item.Expanded += new RoutedEventHandler(item_Expanded);
            return item;
        }

        public class DummyTreeViewItem : TreeViewItem
        {
            public DummyTreeViewItem()
                : base()
            {
                base.Header = "Dummy";
                base.Tag = "Dummy";
            }
        }

        private TreeViewItem GetItem(DirectoryInfo directory)
        {
            var item = new TreeViewItem
            {
                Header = directory.Name,
                DataContext = directory,
                Tag = directory
            };
            this.AddDummy(item);
            item.Expanded += new RoutedEventHandler(item_Expanded);
            return item;
        }

        private TreeViewItem GetItem(FileInfo file)
        {
            var item = new TreeViewItem
            {
                Header = file.Name,
                DataContext = file,
                Tag = file
            };
            return item;
        }

        private void AddDummy(TreeViewItem item)
        {
            item.Items.Add(new DummyTreeViewItem());
        }

        private bool HasDummy(TreeViewItem item)
        {
            return item.HasItems && (item.Items.OfType<TreeViewItem>().ToList().FindAll(tvi => tvi is DummyTreeViewItem).Count > 0);
        }

        private void RemoveDummy(TreeViewItem item)
        {
            var dummies = item.Items.OfType<TreeViewItem>().ToList().FindAll(tvi => tvi is DummyTreeViewItem);
            foreach (var dummy in dummies)
            {
                item.Items.Remove(dummy);
            }
        }

        private void ExploreDirectories(TreeViewItem item)
        {
            var directoryInfo = (DirectoryInfo)null;
            if (item.Tag is DriveInfo)
            {
                directoryInfo = ((DriveInfo)item.Tag).RootDirectory;
            }
            else if (item.Tag is DirectoryInfo)
            {
                directoryInfo = (DirectoryInfo)item.Tag;
            }
            else if (item.Tag is FileInfo)
            {
                directoryInfo = ((FileInfo)item.Tag).Directory;
            }
            if (object.ReferenceEquals(directoryInfo, null)) return;

            foreach (var directory in directoryInfo.GetDirectories())
            {   
                    var isHidden = (directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                    var isSystem = (directory.Attributes & FileAttributes.System) == FileAttributes.System;
                    if (!isHidden && !isSystem)
                    {
                        item.Items.Add(this.GetItem(directory));
                    }
                         
            }
        }

        private void ExploreFiles(TreeViewItem item)
        {
            var directoryInfo = (DirectoryInfo)null;
            if (item.Tag is DriveInfo)
            {
                directoryInfo = ((DriveInfo)item.Tag).RootDirectory;
            }
            else if (item.Tag is DirectoryInfo)
            {
                directoryInfo = (DirectoryInfo)item.Tag;
            }
            else if (item.Tag is FileInfo)
            {
                directoryInfo = ((FileInfo)item.Tag).Directory;
            }
            if (object.ReferenceEquals(directoryInfo, null)) return;
            foreach (var file in directoryInfo.GetFiles())
            {
                var isHidden = (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                var isSystem = (file.Attributes & FileAttributes.System) == FileAttributes.System;
                if (!isHidden && !isSystem)
                {
                    item.Items.Add(this.GetItem(file));
                }
            }


        }
        void item_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (this.HasDummy(item))
            {
                this.Cursor = Cursors.Wait;
                this.RemoveDummy(item);
                this.ExploreDirectories(item);
                this.ExploreFiles(item);
                this.Cursor = Cursors.Arrow;
            }
        }

        private void treeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //czyszczenie prawej strony
            rightSide.Document.Blocks.Clear();

            TreeViewItem tvi = (TreeViewItem)e.NewValue;

            //jezeli dysk wypluj
            if (tvi.Tag is DriveInfo)
            {
                try
                {
                    double ttl = ((DriveInfo)tvi.Tag).TotalSize / 1024 / 1024 / 1024;
                    double afs = ((DriveInfo)tvi.Tag).AvailableFreeSpace / 1024 / 1024 / 1024;
                    rightSide.AppendText($"\nNazwa dysku: {((DriveInfo)tvi.Tag).RootDirectory}");
                    rightSide.AppendText($"\nCałkowita pojemność: {ttl} GB");
                    rightSide.AppendText($"\nDostępne wolne miejsce: {afs} GB");
                    rightSide.AppendText($"\nFormat plików: {((DriveInfo)tvi.Tag).DriveFormat}");
                    rightSide.AppendText($"\nTyp dysku: {((DriveInfo)tvi.Tag).DriveType}");
                    file = false;
                }
                catch (Exception)
                {
                    rightSide.AppendText("Szczegóły niedostępne");
                }
                
            }
            
            //jezeli katalog wypluj
            if (tvi.Tag is DirectoryInfo)
            {
                string dPath = ((DirectoryInfo)tvi.Tag).FullName;

                rightSide.AppendText($"\nNazwa folderu: {((DirectoryInfo)tvi.Tag).Name}");
                rightSide.AppendText($"\nData utworzenia: {((DirectoryInfo)tvi.Tag).CreationTime}");
                rightSide.AppendText($"\nData modyfikacji: {((DirectoryInfo)tvi.Tag).LastWriteTime}");
                rightSide.AppendText($"\nRozmiar katalogu: {GetDirectorySize(dPath)/1024/1024} MB");
                rightSide.AppendText($"\nScieżka: {((DirectoryInfo)tvi.Tag).FullName}");
                rightSide.AppendText($"\nAtrybuty: {((DirectoryInfo)tvi.Tag).Attributes}");
                file = false;
            }
            
            //jezeli plik
            if (tvi.Tag is FileInfo)
            {
                string ext = ((FileInfo)tvi.Tag).Extension;
                string path = ((FileInfo)tvi.Tag).FullName;
                gpath = ((FileInfo)tvi.Tag).FullName;
                file = true;

                if ((ext.Contains(".txt")) || (ext.Contains(".html")) || (ext.Contains(".css")) || (ext.Contains(".sql")))
                {
                    int counter = 0;
                    string line, bufor=null;

                    System.IO.StreamReader file = new System.IO.StreamReader(path);

                    while ((line = file.ReadLine()) != null && counter <5)
                    {
                        bufor += line+"\n";
                        counter++;
                    }
                    file.Close();
                    rightSide.AppendText(bufor);
                }
                if ((ext.Contains(".jpg")) || (ext.Contains(".png")))
                {
                    showImage(path, rightSide);
                }
            }

        }
        //pokaz obraz
        private void showImage(string path, RichTextBox txt)
        {
            byte[] image = FileToByteArray(path);
            LoadImage(image);
            rightSide.Paste();
        }

        private byte[] FileToByteArray(string path)
        {
            return File.ReadAllBytes(path);
        }

        //ładuj obraz
        private static void LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.DecodePixelWidth = 260;
           //     image.DecodePixelHeight = 400;
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            Clipboard.SetImage(image);
        }

        void mouse_DoubleClick(object sender, MouseButtonEventArgs e)
        {
          
            Process p = new Process();
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.UseShellExecute = true;
            pi.FileName = gpath;
            p.StartInfo = pi;

            //jezeli jest to plik
            if (file)
            {
                try
                {
                    rightSide.Document.Blocks.Clear();
                    p.Start();
                }
                catch (Exception)
                {
                    rightSide.AppendText("Błąd");
                }
            }
        }
        //obliczanie rozmiaru katalogu
        public static long GetDirectorySize(string fullDirectoryPath)
        {
            long startDirectorySize = 0;
            if (!Directory.Exists(fullDirectoryPath))
                return startDirectorySize; 

            var currentDirectory = new DirectoryInfo(fullDirectoryPath);
            
            currentDirectory.GetFiles().ToList().ForEach(f => startDirectorySize += f.Length);

            
            currentDirectory.GetDirectories().ToList()
                .ForEach(d => startDirectorySize += GetDirectorySize(d.FullName));

            return startDirectorySize; 
        }

    }
}
