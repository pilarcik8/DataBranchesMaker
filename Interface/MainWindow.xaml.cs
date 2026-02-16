using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Shared;
using Person = Shared.Person;

namespace Interface
{
    public partial class MainWindow : Window
    {
        enum ExportTarget
        {
            Person,
            List,
            HashSet
        }

        public MainWindow()
        {
            InitializeComponent();

            // default selection: Class
            ClassCheckBox.IsChecked = true;
            UpdateSizeInputsVisibility();

            // Default folder: user's Documents
            FolderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void TargetCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // ensure only one checkbox is checked at a time
            if (sender == SetCheckBox)
            {
                ListCheckBox.IsChecked = false;
                ClassCheckBox.IsChecked = false;
            }
            else if (sender == ListCheckBox)
            {
                SetCheckBox.IsChecked = false;
                ClassCheckBox.IsChecked = false;
            }
            else if (sender == ClassCheckBox)
            {
                SetCheckBox.IsChecked = false;
                ListCheckBox.IsChecked = false;
            }

            UpdateSizeInputsVisibility();
        }

        private void TargetCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // prevent having zero checked: re-check the sender if none remain
            if (SetCheckBox.IsChecked != true && ListCheckBox.IsChecked != true && ClassCheckBox.IsChecked != true)
            {
                if (sender is System.Windows.Controls.CheckBox cb) cb.IsChecked = true;
            }
            UpdateSizeInputsVisibility();
        }

        private void UpdateSizeInputsVisibility()
        {
            // Show min/max only when List or Set is selected
            bool showSize = (SetCheckBox.IsChecked == true) || (ListCheckBox.IsChecked == true);
            SizeInputsPanel.Visibility = showSize ? Visibility.Visible : Visibility.Collapsed;
        }

        private ExportTarget GetSelectedTarget()
        {
            if (ClassCheckBox.IsChecked == true) return ExportTarget.Person;
            if (ListCheckBox.IsChecked == true) return ExportTarget.List;
            return ExportTarget.HashSet;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.Description = "Select output folder";
            dlg.SelectedPath = FolderTextBox.Text;
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                FolderTextBox.Text = dlg.SelectedPath;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(IterationTextBox.Text, out int iteration) || iteration < 0)
                {
                    StatusTextBlock.Text = "Iteration must be a non-negative integer.";
                    return;
                }

                var selected = GetSelectedTarget();
                string folder = string.IsNullOrWhiteSpace(FolderTextBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : FolderTextBox.Text.Trim();

                int? min = null;
                int? max = null;
                if (selected == ExportTarget.List || selected == ExportTarget.HashSet)
                {
                    if (!int.TryParse(MinSizeTextBox.Text, out int minVal) || minVal < 0)
                    {
                        StatusTextBlock.Text = "Min size must be a non-negative integer.";
                        return;
                    }
                    if (!int.TryParse(MaxSizeTextBox.Text, out int maxVal) || maxVal < 0)
                    {
                        StatusTextBlock.Text = "Max size must be a non-negative integer.";
                        return;
                    }
                    if (minVal > maxVal)
                    {
                        StatusTextBlock.Text = "Min size cannot be greater than Max size.";
                        return;
                    }
                    min = minVal;
                    max = maxVal;
                }

                switch (selected)
                {
                    case ExportTarget.Person:
                        var person = CreateSamplePerson();
                        FileOutput.Export(person, "person", iteration, folder);
                        StatusTextBlock.Text = $"Exported Person to '{folder}'";
                        break;

                    case ExportTarget.List:
                        int listSize = Random.Shared.Next(min!.Value, max!.Value + 1);
                        var list = CreateSampleList(listSize);
                        FileOutput.Export(list, "list", iteration, folder);
                        StatusTextBlock.Text = $"Exported List<string> (size={listSize}) to '{folder}'";
                        break;

                    case ExportTarget.HashSet:
                        int setSize = Random.Shared.Next(min!.Value, max!.Value + 1);
                        var set = CreateSampleSet(setSize);
                        FileOutput.Export(set, "set", iteration, folder);
                        StatusTextBlock.Text = $"Exported HashSet<string> (size={setSize}) to '{folder}'";
                        break;

                    default:
                        StatusTextBlock.Text = "Unknown selection.";
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private Person CreateSamplePerson()
        {
            var p = new Person();
            p.FirstName = "John";
            p.LastName = "Doe";
            p.Email = "john.doe@example.com";
            p.Phone = "+420 123 456 789";
            p.Company = "Acme";
            return p;
        }

        private List<string> CreateSampleList(int size)
        {
            var list = new List<string>(size);
            for (int i = 0; i < size; i++)
                list.Add($"item_{i + 1}");
            return list;
        }

        private HashSet<string> CreateSampleSet(int size)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < size; i++)
                set.Add($"item_{i + 1}");
            return set;
        }
    }
}