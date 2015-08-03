using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

using IOPath = System.IO.Path;
using System.IO;

namespace ClipSynth
{
    class CheckBoxContent : StackPanel
    {
        public CheckBox CheckBox;
        public Label Label;
        public String Path;
        public Image Thumbnail;
        public TextBox StartTextBox;
        public TextBox EndTextBox;
        public Label UniqueIdLabel;

        public string Content
        {
            get
            {
                return (String)Label.Content;
            }
            set
            {
                Label.Content = value;
            }
        }

        public UInt32 UniqueId
        {
            get
            {
                return (UInt32)UniqueIdLabel.Content;
            }
            set
            {
                UniqueIdLabel.Content = value;
            }
        }

        public int Start
        {
            get
            {
                int result;
                if (int.TryParse(StartTextBox.Text, out result))
                {
                    return result;
                }

                return 1;
            }
            set
            {
                StartTextBox.Text = value.ToString();
            }
        }

        public int End
        {
            get
            {
                int result;
                if (int.TryParse(EndTextBox.Text, out result))
                {
                    return result;
                }

                return 1;
            }
            set
            {
                EndTextBox.Text = value.ToString();
            }
        }

        public CheckBoxContent(String path, String content, RoutedEventHandler checkBoxHandler)
        {
            Path = path;

            Orientation = Orientation.Horizontal;

            CheckBox = new CheckBox();
            CheckBox.Margin = new Thickness(0, 2, 0, 2);
            CheckBox.Content = null;
            CheckBox.IsChecked = false;
            CheckBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            CheckBox.Checked += checkBoxHandler;
            CheckBox.Unchecked += checkBoxHandler;
            CheckBox.Indeterminate += checkBoxHandler;

            Label = new Label();
            Label.Padding = new Thickness(2, 0, 0, 0);
            Label.Margin = new Thickness(0, 0, 0, 2);
            Label.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Content = content;

            Children.Add(CheckBox);
            Children.Add(Label);
        }

        public CheckBoxContent(String path, String content, RoutedEventHandler checkBoxHandler, string thumbnail, int start, int end, UInt32 uniqueId)
            : this(path, content, checkBoxHandler)
        {
            UniqueIdLabel = new Label();
            UniqueIdLabel.Padding = new Thickness(0, 0, 0, 0);
            UniqueIdLabel.Margin = new Thickness(4, 0, 0, 4);
            UniqueIdLabel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            UniqueId = uniqueId;

            Thumbnail = new Image();
            Thumbnail.Margin = new Thickness(2, 0, 0, 2);
            Thumbnail.Width = 128;
            Thumbnail.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            if (!String.IsNullOrEmpty(thumbnail) && File.Exists(thumbnail))
            {
                Thumbnail.Source = new BitmapImage(new Uri(thumbnail));
            }

            StartTextBox = new TextBox();
            StartTextBox.Padding = new Thickness(0, 0, 0, 0);
            StartTextBox.Margin = new Thickness(2, 0, 0, 2);
            StartTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Start = start;

            EndTextBox = new TextBox();
            EndTextBox.Padding = new Thickness(0, 0, 0, 0);
            EndTextBox.Margin = new Thickness(2, 0, 0, 2);
            EndTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            End = end;

            Children.Insert(1, EndTextBox);
            Children.Insert(1, StartTextBox);
            Children.Insert(1, Thumbnail);
            Children.Insert(1, UniqueIdLabel);
        }
    }
}
