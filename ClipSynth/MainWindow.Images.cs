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
        #region Images mode (TreeView)

        private void TreeView_Populate()
        {
            FolderListTreeView.Items.Clear();
            TreeView_AddItem(FolderListTreeView, settings.Folder);
            ExpandCollapse(true);
        }

        private void TreeView_AddItem(ItemsControl control, string path)
        {
            var item = new TreeViewItem();
            item.Header = new CheckBoxContent(path, IOPath.GetFileNameWithoutExtension(path), TreeViewCheckBox_Checked);
            control.Items.Add(item);

            foreach (var subdirectory in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                if (IgnoreFolders.Contains(IOPath.GetFileName(subdirectory)))
                {
                    break;
                }

                TreeView_AddItem(item, subdirectory);
            }
        }

        private TreeViewItem TreeView_GetSelectedItem()
        {
            TreeViewItem item = null;

            if (FolderListTreeView.Items.Count > 0)
            {
                item = (TreeViewItem)FolderListTreeView.Items[0];
            }

            var selectedItem = (TreeViewItem)FolderListTreeView.SelectedItem;
            if (selectedItem != null)
            {
                item = selectedItem;
            }

            return item;
        }

        void TreeViewCheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            var checkBoxContent = (CheckBoxContent)checkBox.Parent;
            var item = (TreeViewItem)checkBoxContent.Parent;

            var select = checkBox.IsChecked;

            if (select.HasValue)
            {
                foreach (TreeViewItem childItem in item.Items)
                {
                    var childCheckBoxContent = (CheckBoxContent)childItem.Header;
                    childCheckBoxContent.CheckBox.IsChecked = select;
                }
            }

            // This is typical TreeView behavior, but in this case, sometimes we might need
            // parent checked even with all children unchecked, so disabling.
            //var parentItem = item.Parent as TreeViewItem;
            //if (parentItem == null)
            //{
            //    return;
            //}

            //var parentCheckBoxContent = (CheckBoxContent)parentItem.Header;

            //var _count = TreeView_GetTrueFalseNullCount(parentItem);
            //var count = new { Trues = _count.Item1, Falses = _count.Item2, Nulls = _count.Item3 };

            //if (select == true && count.Falses == 0 && count.Nulls == 0)
            //{
            //    if (!parentCheckBoxContent.CheckBox.IsChecked.HasValue || parentCheckBoxContent.CheckBox.IsChecked.Value == false)
            //    {
            //        parentCheckBoxContent.CheckBox.IsChecked = true;
            //    }
            //}
            //else if (select == false && count.Trues == 0 && count.Nulls == 0)
            //{
            //    if (!parentCheckBoxContent.CheckBox.IsChecked.HasValue || parentCheckBoxContent.CheckBox.IsChecked.Value == true)
            //    {
            //        parentCheckBoxContent.CheckBox.IsChecked = false;
            //    }
            //}
            //else
            //{
            //    if (parentCheckBoxContent.CheckBox.IsChecked.HasValue)
            //    {
            //        parentCheckBoxContent.CheckBox.IsChecked = null;
            //    }
            //}
        }

        private Tuple<uint, uint, uint> TreeView_GetTrueFalseNullCount(TreeViewItem item)
        {
            uint trues = 0, falses = 0, nulls = 0;
            foreach (TreeViewItem childItem in item.Items)
            {
                var checkBoxContent = (CheckBoxContent)childItem.Header;
                if (checkBoxContent.CheckBox.IsChecked.HasValue)
                {
                    if (checkBoxContent.CheckBox.IsChecked.Value == true)
                    {
                        ++trues;
                    }
                    else
                    {
                        ++falses;
                    }
                }
                else
                {
                    ++nulls;
                }
            }

            return new Tuple<uint, uint, uint>(trues, falses, nulls);
        }

        private List<CheckBoxContent> TreeView_GetCheckedItems(TreeViewItem item)
        {
            var checkedItems = new List<CheckBoxContent>();
            foreach (TreeViewItem childItem in item.Items)
            {
                var checkBoxContent = (CheckBoxContent)childItem.Header;
                if (checkBoxContent.CheckBox.IsChecked.GetValueOrDefault() == true)
                {
                    checkedItems.Add(checkBoxContent);
                }

                checkedItems.AddRange(TreeView_GetCheckedItems(childItem));
            }

            return checkedItems;
        }

        private List<CheckBoxContent> TreeView_GetAllCheckedItems()
        {
            return FolderListTreeView.Items.Count > 0 ? TreeView_GetCheckedItems((TreeViewItem)FolderListTreeView.Items[0]) : new List<CheckBoxContent>();
        }

        #endregion

        #region Expand and Collapse

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapse(true);
        }

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapse(false);
        }

        private void ExpandCollapse(bool expand)
        {
            TreeView_ExpandCollapseItem(TreeView_GetSelectedItem(), expand);
            FolderListTreeView.Focus();
        }

        private void TreeView_ExpandCollapseItem(TreeViewItem item, bool expand)
        {
            if (item == null)
            {
                return;
            }

            item.IsExpanded = expand;

            foreach (TreeViewItem childItem in item.Items)
            {
                TreeView_ExpandCollapseItem(childItem, expand);
            }
        }

        #endregion

        #region Select and Deselect

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            TreeView_SelectDeselectFilter(true);
        }

        private void DeselectButton_Click(object sender, RoutedEventArgs e)
        {
            TreeView_SelectDeselectFilter(false);
        }

        private bool FilterWildcardSearch(string content, string keyword)
        {
            var pattern = new StringBuilder(keyword.Length + 10);
            pattern.Append("^");
            foreach (var c in keyword)
            {
                switch (c)
                {
                    case '*':
                        pattern.Append(".*");
                        break;
                    case '?':
                        pattern.Append(".");
                        break;
                    default:
                        pattern.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }
            pattern.Append("$");

            regex = new Regex(pattern.ToString(), RegexOptions.IgnoreCase);
            return regex.IsMatch(content);
        }

        private void TreeView_SelectDeselectFilter(bool select)
        {
            TreeView_SelectDeselectFilterItem(TreeView_GetSelectedItem(), select);
            FolderListTreeView.Focus();
        }

        private void TreeView_SelectDeselectFilterItem(TreeViewItem item, bool select)
        {
            if (item == null)
            {
                return;
            }

            var checkBoxContent = (CheckBoxContent)item.Header;
            if (FilterWildcardSearch(checkBoxContent.Content, FilterTextBox.Text))
            {
                TreeView_SelectDeselectItem(item, select);
            }

            foreach (TreeViewItem childItem in item.Items)
            {
                TreeView_SelectDeselectFilterItem(childItem, select);
            }
        }

        private void TreeView_SelectDeselectItem(TreeViewItem item, bool select, bool recursive = false)
        {
            var checkBoxContent = (CheckBoxContent)item.Header;
            checkBoxContent.CheckBox.IsChecked = select;

            if (recursive)
            {
                foreach (TreeViewItem childItem in item.Items)
                {
                    TreeView_SelectDeselectItem(childItem, select, recursive);
                }
            }
        }

        #endregion
    }
}
