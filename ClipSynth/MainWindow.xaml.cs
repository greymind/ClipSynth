using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using IOPath = System.IO.Path;

namespace ClipSynth
{
    public partial class MainWindow : Window
    {
        #region Initialize and Exit

        readonly string ToolsFolder = String.Empty;
        readonly string VirtualDubPath = String.Empty;
        readonly string VirtualDubGUIPath = String.Empty;
        readonly string[] IgnoreFolders = new string[] { ".svn", ".hg", ".git" };

        string[] args;
        ClipSynthSettings settings;
        FolderBrowserDialog folderDialog;
        OpenFileDialog openDialog;
        SaveFileDialog saveDialog;
        OpenFileDialog openMovieDialog;
        OpenFileDialog openSoundtrackDialog;
        Regex regex;
        Favorites favorites;
        ClipSynthProject project;

        public MainWindow()
        {
            project = null;

            settings = new ClipSynthSettings();
            ClipSynthSettings.Load(ref settings);

            // Validate settings
            if (!Directory.Exists(settings.Folder))
            {
                settings.Folder = String.Empty;
            }

            if (!File.Exists(settings.Project))
            {
                settings.Project = String.Empty;
            }

            args = Environment.GetCommandLineArgs();
            ToolsFolder = IOPath.GetDirectoryName(args[0]);
            VirtualDubPath = IOPath.Combine(ToolsFolder, @"VirtualDub\vdub.exe");
            VirtualDubGUIPath = IOPath.Combine(ToolsFolder, @"VirtualDub\VirtualDub.exe");

            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = String.Format("ClipSynth v{0}", version);

            TabControl.SelectionChanged += TabControl_SelectionChanged;

            #region Command line argument(s)

            if (args.Length > 1)
            {
                var path = args[1];

                if (Directory.Exists(path))
                {
                    settings.Folder = path;
                    settings.Mode = ClipSynthMode.Images;
                }
                else if (ClipSynthProject.TryLoad(path, ref project))
                {
                    settings.Project = path;
                    settings.Mode = ClipSynthMode.Movies;
                }
            }

            #endregion

            #region Dialogs and windows

            folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select the root folder you wish to operate on.";

            openDialog = new OpenFileDialog();
            openDialog.DefaultExt = ".clipsynth";
            openDialog.Filter = "ClipSynth projects|*.clipsynth";
            openDialog.Title = "Select the ClipSynth project file to load";

            openMovieDialog = new OpenFileDialog();
            openMovieDialog.DefaultExt = ".avi";
            openMovieDialog.Filter = "ClipSynth supported movie files|*.avi";
            openMovieDialog.Title = "Select the movie file to load";

            openSoundtrackDialog = new OpenFileDialog();
            openSoundtrackDialog.DefaultExt = ".wav";
            openSoundtrackDialog.Filter = "ClipSynth supported soundtrack files (*.wav, *.mp3)|*.wav;*.mp3";
            openSoundtrackDialog.Title = "Select the soundtrack file to load";

            saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = ".clipsynth";
            saveDialog.Filter = "ClipSynth projects|*.clipsynth";
            saveDialog.Title = "Choose a filename for the current ClipSynth project";

            favorites = new Favorites();
            favorites.OKButton.Click += Favorites_OKButton_Click;

            #endregion

            TabControl.SelectedItem = settings.Mode == ClipSynthMode.Images ? this.FindName("ImagesTabItem") : this.FindName("MoviesTabItem");
            Refresh();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ProjectSaveCheck())
            {
                e.Cancel = true;
                return;
            }

            favorites.CancelClose = false;
            favorites.Close();

            ClipSynthSettings.Save(ref settings);
        }

        #endregion

        #region Modes and refresh

        private void Refresh()
        {
            #region Dialogs and startup project

            folderDialog.SelectedPath = settings.Folder;

            if (!String.IsNullOrEmpty(settings.Project))
            {
                openDialog.InitialDirectory = IOPath.GetDirectoryName(settings.Project);
                openMovieDialog.InitialDirectory = IOPath.GetDirectoryName(settings.Project);
                saveDialog.InitialDirectory = IOPath.GetDirectoryName(settings.Project);

                if (project == null)
                {
                    ClipSynthProject.TryLoad(settings.Project, ref project);
                }
            }

            #endregion

            #region UI

            MoveUpButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;
            MoveDownButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;

            ExpandButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Visible : Visibility.Collapsed;
            CollapseButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Visible : Visibility.Collapsed;

            FilterTextBox.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Visible : Visibility.Collapsed;
            FilterStackPanel.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Visible : Visibility.Collapsed;

            LoadButton.Content = settings.Mode == ClipSynthMode.Images ? "_Load Folder" : "_Load Project";
            FilterTextBox.Text = settings.Mode == ClipSynthMode.Images ? "hi" : String.Empty;

            SaveButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;

            AddMovieButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;
            RemoveMovieButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;

            AddSoundtrackButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;
            RemoveSoundtrackButton.Visibility = settings.Mode == ClipSynthMode.Images ? Visibility.Collapsed : Visibility.Visible;

            #endregion

            #region Title

            var activeTitle = String.Empty;
            switch (settings.Mode)
            {
                case ClipSynthMode.Images:
                    activeTitle = String.Format("{0} - ", IOPath.GetFileNameWithoutExtension(settings.Folder));
                    break;
                case ClipSynthMode.Movies:
                    if (project != null)
                    {
                        activeTitle = String.Format("{0} - ", IOPath.GetFileNameWithoutExtension(project.Path));
                    }

                    break;
                default:
                    break;
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = String.Format("{0}ClipSynth v{1}.{2}", activeTitle, version.Major, version.Minor);

            #endregion

            #region Populate

            if (settings.Mode == ClipSynthMode.Images)
            {
                if (!String.IsNullOrEmpty(settings.Folder))
                {
                    PathTextBox.Text = settings.Folder.Replace("\\\\", "\\");
                    TreeView_Populate();
                }
            }
            else if (settings.Mode == ClipSynthMode.Movies)
            {
                if (!String.IsNullOrEmpty(settings.Project))
                {
                    PathTextBox.Text = settings.Project.Replace("\\\\", "\\");
                    ListBox_Populate();
                    SoundtrackListBox_Populate();
                }
            }

            #endregion
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabItem = TabControl.SelectedItem as TabItem;
            if (tabItem == null)
            {
                return;
            }

            var modeChanged = false;
            switch (tabItem.Name)
            {
                case "ImagesTabItem":
                    if (settings.Mode != ClipSynthMode.Images)
                    {
                        settings.Mode = ClipSynthMode.Images;
                        modeChanged = true;
                    }

                    break;
                case "MoviesTabItem":
                    if (settings.Mode != ClipSynthMode.Movies)
                    {
                        settings.Mode = ClipSynthMode.Movies;
                        modeChanged = true;
                    }

                    break;
                default:
                    break;
            }

            if (modeChanged)
            {
                Refresh();
            }
        }

        private void HelpAboutButton_Click(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(String.Format("ClipSynth v{0}.{1} (c) 2010-2011", version.Major, version.Minor));
            stringBuilder.AppendLine("    Engineering by Balakrishnan \"Balki\" Ranganathan (balki@live.com)");
            stringBuilder.AppendLine("    Design by Scott Easley (easleydunn@gmail.com)");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("    For technical difficulties, usage options, suggestions and comments, please email all contributing authors.");
            MessageBox.Show(stringBuilder.ToString());
        }

        #endregion

        #region Generate and Preview

        private void WidthHeightTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.SelectAll();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                // First type encountered will only be processed
                var fileTypes = new String[] { "jpg", "jpeg", "png", };

                var checkedItems = TreeView_GetAllCheckedItems();

                if (checkedItems.Count > 0)
                {
                    ProgressBar.Maximum = checkedItems.Count;
                }
                else
                {
                    MessageBox.Show("No items selected!", "ClipSynth Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                foreach (var checkedItem in checkedItems)
                {
                    ++ProgressBar.Value;
                    ProgressBar.InvalidateVisual();

                    foreach (var fileType in fileTypes)
                    {
                        var directory = IOPath.GetFileNameWithoutExtension(checkedItem.Path);
                        var files = Directory.GetFiles(checkedItem.Path, String.Format("*.{0}", fileType), SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            var jobs = new StreamWriter(@"VirtualDub.jobs");
                            jobs.WriteLine("VirtualDub.Open(U\"{0}\");", files[0]);

                            UInt32 width = 0, height = 0;
                            UInt32.TryParse(WidthTextBox.Text, out width);
                            UInt32.TryParse(HeightTextBox.Text, out height);

                            if (width != 0 && height != 0)
                            {
                                jobs.WriteLine("VirtualDub.video.filters.Add(\"resize\");");
                                jobs.WriteLine("VirtualDub.video.filters.instance[0].Config({0}, {1}, 2);", width, height);
                            }

                            var filename = String.Format("{0}.{1}.avi", IOPath.Combine(checkedItem.Path, directory), fileType);
                            var imageFileSplit = files[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                            if (imageFileSplit.Length > 0)
                            {
                                filename = String.Format("{0}.avi", imageFileSplit[0]);
                            }

                            jobs.WriteLine("VirtualDub.video.SetFrameRate2({0}, {1}, {2});", 30, 1, 1);
                            jobs.WriteLine("VirtualDub.SaveCompatibleAVI(U\"{0}\");", filename);
                            jobs.WriteLine("VirtualDub.Close();");
                            jobs.Close();

                            File.Delete(filename);

                            var process = Process.Start(VirtualDubPath, "/s VirtualDub.jobs");
                            process.WaitForExit();

                            if (File.Exists(filename))
                            {
                                var metaData = new ClipSynthMetaData();
                                metaData.Path = String.Format("{0}.csmd", filename);
                                metaData.Movie = filename;
                                metaData.Thumbnail = files[0];
                                metaData.Frames = files.Length;
                                ClipSynthMetaData.Save(ref metaData);
                            }
                        }
                    }
                }

                ProgressBar.Value = 0;
                ProgressBar.Maximum = 100;
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                var avs = new StreamWriter(@"VirtualDub.avs");
                var jobs = new StreamWriter(@"VirtualDub.jobs");

                var checkedItems = ListBox_GetAllCheckedItems();

                if (checkedItems.Count > 0)
                {
                    ProgressBar.Maximum = checkedItems.Count;
                }
                else
                {
                    MessageBox.Show("No items selected!", "ClipSynth Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    avs.Close();
                    jobs.Close();

                    return;
                }

                var index = 0;
                foreach (var checkedItem in checkedItems)
                {
                    ++ProgressBar.Value;
                    ProgressBar.InvalidateVisual();

                    var filename = (string)checkedItem.Label.Tag;
                    if (File.Exists(filename))
                    {
                        avs.WriteLine("_{0} = AVISource(\"{1}\").Trim({2}, {3})", index++, filename, checkedItem.Start, checkedItem.End);
                    }
                }

                if (index != 0)
                {
                    avs.Write("_{0} = ", index);
                    for (int i = 0; i < index; i++)
                    {
                        avs.Write("{0}_{1}", i != 0 ? " + " : String.Empty, i);
                    }
                    avs.WriteLine();

                    var soundtracks = SoundtrackListBox_GetAllCheckedItems();
                    if (soundtracks.Count > 0)
                    {
                        var soundtrack = (string)soundtracks[0].Label.Tag;
                        if (File.Exists(soundtrack))
                        {
                            if (IOPath.GetExtension(soundtrack) == ".mp3")
                            {
                                avs.Write("LoadPlugin(\"NicAudio.dll\")");
                                avs.Write("AudioDub(_{0}, NicMPG123Source(\"{1}\"))", index, soundtrack);
                                avs.WriteLine();
                                avs.Write("DelayAudio(-{0})", soundtracks[0].Start / 30.0f);
                            }
                            else if (IOPath.GetExtension(soundtrack) == ".wav")
                            {
                                avs.Write("AudioDub(_{0}, WAVSource(\"{1}\"))", index, soundtrack);
                                avs.WriteLine();
                                avs.Write("DelayAudio(-{0})", soundtracks[0].Start / 30.0f);
                            }
                        }
                    }
                    else
                    {
                        avs.Write("_{0}", index);
                    }
                    avs.Close();

                    var previewFilename = String.Format("{0}.avi", IOPath.Combine(IOPath.GetDirectoryName(settings.Project), "Preview"));
                    jobs.WriteLine("VirtualDub.Open(U\"VirtualDub.avs\");");
                    jobs.WriteLine("VirtualDub.SaveCompatibleAVI(U\"{0}\");", previewFilename);
                    jobs.WriteLine("VirtualDub.Close();");
                    jobs.Close();

                    File.Delete(previewFilename);

                    var process = Process.Start(VirtualDubPath, "/s VirtualDub.jobs");
                    process.WaitForExit();

                    if (File.Exists(previewFilename))
                    {
                        var result = MessageBox.Show("Generate complete. Do you want to preview now?", "ClipSynth Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        switch (result)
                        {
                            case MessageBoxResult.No:
                                break;
                            case MessageBoxResult.Yes:
                                process = Process.Start(previewFilename);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    avs.Close();
                    jobs.Close();
                }

                ProgressBar.Value = 0;
                ProgressBar.Maximum = 100;
            }

            #endregion
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                var firstFileOpened = false;
                var jobs = new StreamWriter(@"VirtualDub.jobs");

                var checkedItems = TreeView_GetAllCheckedItems();

                if (checkedItems.Count > 0)
                {
                    ProgressBar.Maximum = checkedItems.Count;
                }

                foreach (var checkedItem in checkedItems)
                {
                    ++ProgressBar.Value;
                    ProgressBar.InvalidateVisual();

                    var files = Directory.GetFiles(checkedItem.Path, "*.avi", SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        var filename = files[0];

                        if (File.Exists(filename))
                        {
                            if (!firstFileOpened)
                            {
                                firstFileOpened = true;
                                jobs.WriteLine("VirtualDub.Open(U\"{0}\");", filename);
                            }
                            else
                            {
                                jobs.WriteLine("VirtualDub.Append(U\"{0}\");", filename);
                            }
                        }
                    }
                }

                if (firstFileOpened)
                {
                    var previewFilename = String.Format("{0}.avi", IOPath.Combine(settings.Folder, "Preview"));
                    File.Delete(previewFilename);

                    jobs.WriteLine("VirtualDub.video.SetFrameRate2({0}, {1}, {2});", 30, 1, 1);

                    var write = false;
                    if (write)
                    {
                        jobs.WriteLine("VirtualDub.SaveCompatibleAVI(U\"{0}\");", previewFilename);
                        jobs.WriteLine("VirtualDub.Close();");
                        jobs.Close();

                        var process = Process.Start(VirtualDubPath, "/s VirtualDub.jobs");
                        process.WaitForExit();

                        if (File.Exists(previewFilename))
                        {
                            process = Process.Start(previewFilename);
                        }
                    }
                    else
                    {
                        jobs.WriteLine("VirtualDub.Preview();");
                        jobs.Close();

                        var process = Process.Start(VirtualDubGUIPath, "/s VirtualDub.jobs");
                    }
                }
                else
                {
                    jobs.Close();
                }

                ProgressBar.Value = 0;
                ProgressBar.Maximum = 100;
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                var avs = new StreamWriter(@"VirtualDub.avs");
                var jobs = new StreamWriter(@"VirtualDub.jobs");

                var checkedItems = ListBox_GetAllCheckedItems();

                if (checkedItems.Count > 0)
                {
                    ProgressBar.Maximum = checkedItems.Count;
                }
                else
                {
                    MessageBox.Show("No items selected!", "ClipSynth Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    avs.Close();
                    jobs.Close();

                    return;
                }

                var index = 0;
                foreach (var checkedItem in checkedItems)
                {
                    ++ProgressBar.Value;
                    ProgressBar.InvalidateVisual();

                    var filename = (string)checkedItem.Label.Tag;
                    if (File.Exists(filename))
                    {
                        avs.WriteLine("_{0} = AVISource(\"{1}\").Trim({2}, {3})", index++, filename, checkedItem.Start, checkedItem.End);
                    }
                }

                if (index != 0)
                {
                    avs.Write("_{0} = ", index);
                    for (int i = 0; i < index; i++)
                    {
                        avs.Write("{0}_{1}", i != 0 ? " + " : String.Empty, i);
                    }
                    avs.WriteLine();

                    var soundtracks = SoundtrackListBox_GetAllCheckedItems();
                    if (soundtracks.Count > 0)
                    {
                        var soundtrack = (string)soundtracks[0].Label.Tag;
                        if (File.Exists(soundtrack))
                        {
                            if (IOPath.GetExtension(soundtrack) == ".mp3")
                            {
                                avs.Write("LoadPlugin(\"NicAudio.dll\")");
                                avs.Write("AudioDub(_{0}, NicMPG123Source(\"{1}\"))", index, soundtrack);
                                avs.WriteLine();
                                avs.Write("DelayAudio(-{0})", soundtracks[0].Start / 30.0f);
                            }
                            else if (IOPath.GetExtension(soundtrack) == ".wav")
                            {
                                avs.Write("AudioDub(_{0}, WAVSource(\"{1}\"))", index, soundtrack);
                                avs.WriteLine();
                                avs.Write("DelayAudio(-{0})", soundtracks[0].Start / 30.0f);
                            }
                        }
                    }
                    else
                    {
                        avs.Write("_{0}", index);
                    }
                    avs.Close();

                    jobs.WriteLine("VirtualDub.Open(U\"VirtualDub.avs\");");
                    jobs.WriteLine("VirtualDub.Preview();");
                    jobs.Close();

                    var process = Process.Start(VirtualDubGUIPath, "/s VirtualDub.jobs");
                }
                else
                {
                    avs.Close();
                    jobs.Close();
                }

                ProgressBar.Value = 0;
                ProgressBar.Maximum = 100;
            }

            #endregion
        }

        #endregion

        #region Load, save and refresh

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void BrowseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                Process.Start(settings.Folder);
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                var listBoxItem = ListBox_GetSelectedItem();
                if (listBoxItem != null)
                {
                    var checkBoxContent = (CheckBoxContent)listBoxItem.Content;
                    var clipSynthMovie = (ClipSynthMovie)checkBoxContent.Tag;

                    Process.Start(IOPath.GetDirectoryName(clipSynthMovie.Path));
                }
                else
                {
                    MessageBox.Show("Please select an item to browse", "ClipSynth Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            #endregion
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                var result = folderDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    settings.Folder = folderDialog.SelectedPath;
                    Refresh();
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                var result = openDialog.ShowDialog(this);
                if (result.GetValueOrDefault() && ClipSynthProject.TryLoad(openDialog.FileName, ref project))
                {
                    settings.Project = openDialog.FileName;
                    Refresh();
                }
            }

            #endregion
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (project == null)
            {
                project = new ClipSynthProject();
            }

            var result = saveDialog.ShowDialog(this);
            if (result.GetValueOrDefault())
            {
                project.Path = saveDialog.FileName;
                project.Dirty = false;
                ClipSynthProject.Save(ref project);
            }
        }

        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                if (e.Key == Key.Return && Directory.Exists(PathTextBox.Text))
                {
                    settings.Folder = PathTextBox.Text;
                    Refresh();
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                if (e.Key == Key.Return && ClipSynthProject.TryLoad(PathTextBox.Text, ref project))
                {
                    settings.Project = PathTextBox.Text;
                    Refresh();
                }
            }

            #endregion
        }

        private bool ProjectSaveCheck()
        {
            if (project != null && project.Dirty)
            {
                var result = MessageBox.Show("There are unsaved changes to the project. Do you want to save now?", "ClipSynth Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        break;
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Yes:
                        project.Dirty = false;
                        ClipSynthProject.Save(ref project);
                        return true;
                    default:
                        break;
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion

        #region Select All

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var select = SelectAllCheckBox.IsChecked.Value;

            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                TreeViewItem item = null;

                if (FolderListTreeView.Items.Count > 0)
                {
                    item = (TreeViewItem)FolderListTreeView.Items[0];
                }

                if (item != null)
                {
                    foreach (TreeViewItem treeViewItem in item.Items)
                    {
                        TreeView_SelectDeselectItem(treeViewItem, select, true);
                    }
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                foreach (ListBoxItem item in FileListBox.Items)
                {
                    var checkBoxContent = (CheckBoxContent)item.Content;
                    checkBoxContent.CheckBox.IsChecked = select;
                }
            }

            #endregion
        }

        #endregion

        #region Favorites

        private void AddFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                if (!settings.FavoriteFolders.Contains(settings.Folder))
                {
                    settings.FavoriteFolders.Add(settings.Folder);
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                if (!settings.FavoriteProjects.Contains(settings.Project))
                {
                    settings.FavoriteProjects.Add(settings.Project);
                }
            }

            #endregion
        }

        private void RemoveFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                settings.FavoriteFolders.Remove(settings.Folder);
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                settings.FavoriteProjects.Remove(settings.Project);
            }

            #endregion
        }

        private void PopulateFavoritesList()
        {
            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                favorites.FavoritesList.Items.Clear();
                foreach (var favorite in settings.FavoriteFolders)
                {
                    favorites.FavoritesList.Items.Add(favorite);
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                favorites.FavoritesList.Items.Clear();
                foreach (var favorite in settings.FavoriteProjects)
                {
                    favorites.FavoritesList.Items.Add(favorite);
                }
            }

            #endregion
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            PopulateFavoritesList();
            favorites.Show();
        }

        private void Favorites_OKButton_Click(object sender, RoutedEventArgs e)
        {
            favorites.Close();

            var favorite = favorites.FavoritesList.SelectedItem as string;

            #region Images mode

            if (settings.Mode == ClipSynthMode.Images)
            {
                if (!String.IsNullOrEmpty(favorite))
                {
                    if (Directory.Exists(favorite))
                    {
                        if (settings.Folder != favorite)
                        {
                            settings.Folder = favorite;
                            Refresh();
                        }
                    }
                    else
                    {
                        var result = MessageBox.Show("This folder does not exist anymore, do you wish to remove it from your favorites?",
                            "ClipSynth Curious", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                settings.FavoriteFolders.Remove(favorite);
                                break;
                        }
                    }
                }
            }

            #endregion

            #region Movies mode

            if (settings.Mode == ClipSynthMode.Movies)
            {
                if (!String.IsNullOrEmpty(favorite))
                {
                    if (File.Exists(favorite))
                    {
                        if (settings.Project != favorite && ClipSynthProject.TryLoad(favorite, ref project) && ProjectSaveCheck())
                        {
                            settings.Project = favorite;
                            Refresh();
                        }
                    }
                    else
                    {
                        var result = MessageBox.Show("This project file does not exist anymore, do you wish to remove it from your favorites?",
                            "ClipSynth Curious", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                settings.FavoriteProjects.Remove(favorite);
                                break;
                        }
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
