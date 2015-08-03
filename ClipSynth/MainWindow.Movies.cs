using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.IO;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using IOPath = System.IO.Path;

namespace ClipSynth
{
    public partial class MainWindow
    {
        #region Movies mode (ListBox)

        private void AddMovieButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectIsNotNull())
            {
                var result = openMovieDialog.ShowDialog(this);
                if (result.GetValueOrDefault())
                {
                    if (File.Exists(openMovieDialog.FileName))
                    {
                        var moviePath = openMovieDialog.FileName;
                        var metaDataPath = String.Format("{0}.csmd", moviePath);
                        var metaData = new ClipSynthMetaData();

                        if (File.Exists(metaDataPath))
                        {
                            metaData.Path = metaDataPath;
                            ClipSynthMetaData.Load(ref metaData);
                        }
                        else
                        {
                            MessageBox.Show("Meta data file not found! Please ensure you generate this movie file using ClipSynth", "ClipSynth Warning");
                        }

                        var movie = new ClipSynthMovie()
                        {
                            Path = moviePath,
                            Thumbnail = metaData.Thumbnail,
                            MetaData = metaDataPath,
                            Start = 1,
                            End = metaData.Frames,
                            UniqueId = project.GetUniqueId(),
                        };

                        project.Movies.Add(movie);
                        ListBox_AddItem(movie);
                    }
                }

                project.Dirty = true;
            }
        }

        private void RemoveMovieButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectIsNotNull())
            {
                var index = -1;
                foreach (ListBoxItem movieItem in FileListBox.Items)
                {
                    ++index;

                    var checkBoxContent = (CheckBoxContent)movieItem.Content;
                    if (checkBoxContent.CheckBox.IsChecked.GetValueOrDefault())
                    {
                        project.Movies.Remove((ClipSynthMovie)checkBoxContent.Tag);
                    }
                }

                project.Dirty = true;
                Refresh();
            }
        }

        private bool ProjectIsNotNull()
        {
            if (project == null)
            {
                System.Windows.Forms.MessageBox.Show("Please create/load a project first!");
            }

            return project != null;
        }

        private void ListBox_Populate()
        {
            if (ProjectIsNotNull())
            {
                FileListBox.Items.Clear();
                foreach (var movie in project.Movies)
                {
                    ListBox_AddItem(movie);
                }
            }
        }

        private void ListBox_AddItem(ClipSynthMovie movie)
        {
            // Meta data intelligent check/fix
            if (!File.Exists(movie.Thumbnail))
            {
                var metaDataThumbnail = IOPath.Combine(IOPath.GetDirectoryName(movie.Path), IOPath.GetFileName(movie.Thumbnail));
                if (File.Exists(metaDataThumbnail))
                {
                    movie.Thumbnail = metaDataThumbnail;

                    // Fix meta data file
                    var metaData = new ClipSynthMetaData();
                    metaData.Path = movie.MetaData;
                    ClipSynthMetaData.Load(ref metaData);

                    metaData.Movie = movie.Path;
                    metaData.Thumbnail = metaDataThumbnail;
                    ClipSynthMetaData.Save(ref metaData);
                }
            }

            var item = new ListBoxItem();
            var checkBoxContent = new CheckBoxContent(movie.Path, IOPath.GetFileName(movie.Path), ListBoxCheckBox_Checked, movie.Thumbnail, movie.Start, movie.End, movie.UniqueId);
            checkBoxContent.Tag = movie;
            checkBoxContent.CheckBox.IsChecked = movie.Checked;
            checkBoxContent.Label.ToolTip = movie.Path;
            checkBoxContent.Label.Tag = movie.Path;
            item.Content = checkBoxContent;
            checkBoxContent.StartTextBox.TextChanged += ListBoxTextBox_TextChanged;
            checkBoxContent.EndTextBox.TextChanged += ListBoxTextBox_TextChanged;

            FileListBox.Items.Add(item);
        }

        private ListBoxItem ListBox_GetSelectedItem()
        {
            //ListBoxItem item = null;

            //if (FileListBox.Items.Count > 0)
            //{
            //    item = (ListBoxItem)FileListBox.Items[0];
            //}

            var selectedItem = (ListBoxItem)FileListBox.SelectedItem;
            return selectedItem;
            //if (selectedItem != null)
            //{
            //    item = selectedItem;
            //}

            //return item;
        }

        private List<int> ListBox_GetAllCheckedIndices()
        {
            var checkedIndices = new List<int>();

            var index = -1;
            foreach (ListBoxItem childItem in FileListBox.Items)
            {
                ++index;

                var checkBoxContent = (CheckBoxContent)childItem.Content;
                if (checkBoxContent.CheckBox.IsChecked.GetValueOrDefault())
                {
                    checkedIndices.Add(index);
                }
            }

            return checkedIndices;
        }

        private List<CheckBoxContent> ListBox_GetAllCheckedItems()
        {
            var checkedItems = new List<CheckBoxContent>();
            foreach (ListBoxItem childItem in FileListBox.Items)
            {
                var checkBoxContent = (CheckBoxContent)childItem.Content;
                if (checkBoxContent.CheckBox.IsChecked.GetValueOrDefault())
                {
                    checkedItems.Add(checkBoxContent);
                }
            }

            return checkedItems;
        }

        void ListBoxCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var checkBoxContent = (CheckBoxContent)checkBox.Parent;
            var item = (ListBoxItem)checkBoxContent.Parent;

            if (checkBoxContent.Tag is ClipSynthMovie)
            {
                var movie = (ClipSynthMovie)checkBoxContent.Tag;
                movie.Checked = checkBox.IsChecked.GetValueOrDefault();
            }
            else if (checkBoxContent.Tag is ClipSynthSoundtrack)
            {
                var soundtrack = (ClipSynthSoundtrack)checkBoxContent.Tag;
                soundtrack.Checked = checkBox.IsChecked.GetValueOrDefault();
            }
        }

        void ListBoxTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var checkBoxContent = (CheckBoxContent)textBox.Parent;
            var item = (ListBoxItem)checkBoxContent.Parent;

            if (checkBoxContent.Tag is ClipSynthMovie)
            {
                var movie = (ClipSynthMovie)checkBoxContent.Tag;

                if (textBox == checkBoxContent.StartTextBox && movie.Start != checkBoxContent.Start)
                {
                    movie.Start = checkBoxContent.Start;
                    project.Dirty = true;
                }
                else if (textBox == checkBoxContent.EndTextBox && movie.End != checkBoxContent.End)
                {
                    movie.End = checkBoxContent.End;
                    project.Dirty = true;
                }
            }
            else if (checkBoxContent.Tag is ClipSynthSoundtrack)
            {
                var soundtrack = (ClipSynthSoundtrack)checkBoxContent.Tag;

                if (textBox == checkBoxContent.StartTextBox && soundtrack.Offset != checkBoxContent.Start)
                {
                    soundtrack.Offset = checkBoxContent.Start;
                    project.Dirty = true;
                }
            }
        }

        #endregion

        #region Sountrack

        private void AddSoundtrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectIsNotNull())
            {
                var result = openSoundtrackDialog.ShowDialog(this);
                if (result.GetValueOrDefault())
                {
                    if (File.Exists(openSoundtrackDialog.FileName))
                    {
                        var soundtrackPath = openSoundtrackDialog.FileName;

                        var soundtrack = new ClipSynthSoundtrack()
                        {
                            Path = soundtrackPath,
                            Offset = 0,
                            Checked = true,
                        };

                        RemoveSoundtrackButton_Click(null, null);
                        project.Soundtrack = soundtrack;
                        SoundtrackListBox_AddItem(soundtrack);

                        project.Dirty = true;
                    }
                }
            }
        }

        private void RemoveSoundtrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectIsNotNull())
            {
                project.Soundtrack = new ClipSynthSoundtrack();

                project.Dirty = true;
                Refresh();
            }
        }

        private void SoundtrackListBox_Populate()
        {
            if (ProjectIsNotNull())
            {
                SoundtrackListBox.Items.Clear();
                if (!String.IsNullOrEmpty(project.Soundtrack.Path))
                {
                    SoundtrackListBox_AddItem(project.Soundtrack);
                }
            }
        }

        private void SoundtrackListBox_AddItem(ClipSynthSoundtrack soundtrack)
        {
            var item = new ListBoxItem();
            var checkBoxContent = new CheckBoxContent(soundtrack.Path, IOPath.GetFileName(soundtrack.Path), ListBoxCheckBox_Checked, String.Empty, soundtrack.Offset, 0, 0);
            checkBoxContent.UniqueIdLabel.Visibility = System.Windows.Visibility.Collapsed;
            checkBoxContent.Thumbnail.Visibility = System.Windows.Visibility.Collapsed;
            checkBoxContent.EndTextBox.Visibility = System.Windows.Visibility.Collapsed;
            checkBoxContent.Margin = new Thickness(0, 6, 0, 0);

            checkBoxContent.Tag = soundtrack;
            checkBoxContent.CheckBox.IsChecked = soundtrack.Checked;
            checkBoxContent.Label.ToolTip = soundtrack.Path;
            checkBoxContent.Label.Tag = soundtrack.Path;
            item.Content = checkBoxContent;
            checkBoxContent.StartTextBox.TextChanged += ListBoxTextBox_TextChanged;

            SoundtrackListBox.Items.Add(item);
        }

        private List<CheckBoxContent> SoundtrackListBox_GetAllCheckedItems()
        {
            var checkedItems = new List<CheckBoxContent>();
            foreach (ListBoxItem childItem in SoundtrackListBox.Items)
            {
                var checkBoxContent = (CheckBoxContent)childItem.Content;
                if (checkBoxContent.CheckBox.IsChecked.GetValueOrDefault())
                {
                    checkedItems.Add(checkBoxContent);
                }
            }

            return checkedItems;
        }

        #endregion

        #region Move Up/Down

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedIndices = ListBox_GetAllCheckedIndices();

            if (checkedIndices.Count > 0)
            {
                for (int i = 0; i < checkedIndices.Count; ++i)
                {
                    var index = checkedIndices[i];
                    if (index > 0)
                    {
                        var temp = project.Movies[index - 1];
                        project.Movies[index - 1] = project.Movies[index];
                        project.Movies[index] = temp;
                    }
                }

                project.Dirty = true;
                Refresh();
            }
            else
            {
                MessageBox.Show("Please select item(s) to move up", "ClipSynth Error");
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedIndices = ListBox_GetAllCheckedIndices();

            if (checkedIndices.Count > 0)
            {
                for (int i = checkedIndices.Count - 1; i >= 0; --i)
                {
                    var index = checkedIndices[i];
                    if (index < project.Movies.Count - 1)
                    {
                        var temp = project.Movies[index + 1];
                        project.Movies[index + 1] = project.Movies[index];
                        project.Movies[index] = temp;
                    }
                }

                project.Dirty = true;
                Refresh();
            }
            else
            {
                MessageBox.Show("Please select item(s) to move up", "ClipSynth Error");
            }
        }

        #endregion
    }
}
