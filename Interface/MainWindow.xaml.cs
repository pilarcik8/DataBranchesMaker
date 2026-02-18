using System.Windows;
using ListMakerSettings = ListMaker.ListMaker;
using PersonMakerSettings = PersonMaker.PersonMaker;
using SetMakerSettings = SetMaker.SetMaker;
    
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

            ClassCheckBox.IsChecked = true;
            UpdateSizeInputsVisibility();

            AllowChangeCheckBox.IsChecked = true;
            AllowRemoveCheckBox.IsChecked = true;
            AllowAddCheckBox.IsChecked = true;
            AllowShiftCheckBox.IsChecked = true;

            UpdateAllowedActionsForTarget();

            FolderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void TargetCheckBox_Checked(object sender, RoutedEventArgs e)
        {
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
            UpdateAllowedActionsForTarget();
        }

        private void TargetCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SetCheckBox.IsChecked != true && ListCheckBox.IsChecked != true && ClassCheckBox.IsChecked != true)
            {
                if (sender is System.Windows.Controls.CheckBox cb) cb.IsChecked = true;
            }
            UpdateSizeInputsVisibility();
            UpdateAllowedActionsForTarget();
        }

        private void UpdateSizeInputsVisibility()
        {
            // min a max size sú relevantné len pre Set a List, takže ich panel zobrazíme len ak je vybraný Set alebo List
            bool showSize = (SetCheckBox.IsChecked == true) || (ListCheckBox.IsChecked == true);
            SizeInputsPanel.Visibility = showSize ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAllowedActionsForTarget()
        {
            bool isSet = SetCheckBox.IsChecked == true;
            bool isList = ListCheckBox.IsChecked == true;
            bool isClass = ClassCheckBox.IsChecked == true;

            // Set: Add + Remove
            if (isSet)
            {
                AllowAddCheckBox.IsEnabled = true;
                AllowRemoveCheckBox.IsEnabled = true;

                AllowChangeCheckBox.IsEnabled = false;
                AllowShiftCheckBox.IsEnabled = false;

                if (AllowAddCheckBox.IsChecked != true) AllowAddCheckBox.IsChecked = true;
                if (AllowRemoveCheckBox.IsChecked != true) AllowRemoveCheckBox.IsChecked = true;

                AllowChangeCheckBox.IsChecked = false;
                AllowShiftCheckBox.IsChecked = false;
            }
            // List: Add, Remove, Change, Shift
            else if (isList)
            {
                AllowAddCheckBox.IsEnabled = true;
                AllowRemoveCheckBox.IsEnabled = true;
                AllowChangeCheckBox.IsEnabled = true;
                AllowShiftCheckBox.IsEnabled = true;

                // na začiatku true, ale ak už používateľ odklikol a zmenil výber, nech to nezruší
                if (AllowAddCheckBox.IsChecked != true) AllowAddCheckBox.IsChecked = true;
                if (AllowRemoveCheckBox.IsChecked != true) AllowRemoveCheckBox.IsChecked = true;
                if (AllowChangeCheckBox.IsChecked != true) AllowChangeCheckBox.IsChecked = true;
                if (AllowShiftCheckBox.IsChecked != true) AllowShiftCheckBox.IsChecked = true;
            }
            // Class(Person): Add, Remove, Change
            else if (isClass)
            {
                AllowAddCheckBox.IsEnabled = true;
                AllowRemoveCheckBox.IsEnabled = true;
                AllowChangeCheckBox.IsEnabled = true;

                AllowShiftCheckBox.IsEnabled = false;

                if (AllowAddCheckBox.IsChecked != true) AllowAddCheckBox.IsChecked = true;
                if (AllowRemoveCheckBox.IsChecked != true) AllowRemoveCheckBox.IsChecked = true;
                if (AllowChangeCheckBox.IsChecked != true) AllowChangeCheckBox.IsChecked = true;

                AllowShiftCheckBox.IsChecked = false;
            }

            UpdateMaxPanelsVisibility();
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
                    // Class
                    case ExportTarget.Person:
                        bool allowChange = AllowChangeCheckBox.IsChecked == true;
                        bool allowRemove = AllowRemoveCheckBox.IsChecked == true;
                        bool allowAdd = AllowAddCheckBox.IsChecked == true;

                        int maxChanges = int.MaxValue;
                        int maxRemovals = int.MaxValue;
                        int maxAdditions = int.MaxValue;

                        if (allowChange)
                        {
                            if (!int.TryParse(MaxChangesTextBox.Text, out maxChanges) || maxChanges < 0)
                            {
                                StatusTextBlock.Text = "Max changes must be a non-negative integer.";
                                return;
                            }
                        }

                        if (allowRemove)
                        {
                            if (!int.TryParse(MaxRemovalsTextBox.Text, out maxRemovals) || maxRemovals < 0)
                            {
                                StatusTextBlock.Text = "Max removals must be a non-negative integer.";
                                return;
                            }
                        }

                        if (allowAdd)
                        {
                            if (!int.TryParse(MaxAdditionsTextBox.Text, out maxAdditions) || maxAdditions < 0)
                            {
                                StatusTextBlock.Text = "Max additions must be a non-negative integer.";
                                return;
                            }
                        }

                        PersonMakerSettings.SetParameters(
                            numberIterations: iteration,
                            changesAllowed: allowChange,
                            removingAllowed: allowRemove,
                            addingAllowed: allowAdd,
                            outputDirectory: folder
                        );

                        PersonMakerSettings.SetMaxAllowed(
                            maxChanges: maxChanges,
                            maxRemovals: maxRemovals,
                            maxAdditions: maxAdditions
                        );

                        PersonMakerSettings.Main();

                        StatusTextBlock.Text = $"Exported class Person to '{folder}'";
                        break;

                    // List
                    case ExportTarget.List:
                        bool listAllowRemove = AllowRemoveCheckBox.IsChecked == true;
                        bool listAllowAdd = AllowAddCheckBox.IsChecked == true;
                        bool listAllowShift = AllowShiftCheckBox.IsChecked == true;

                        int listMaxRemovals = int.MaxValue;
                        int listMaxAdditions = int.MaxValue;
                        int listMaxShifts = int.MaxValue;

                        if (listAllowRemove)
                        {
                            if (!int.TryParse(MaxRemovalsTextBox.Text, out listMaxRemovals) || listMaxRemovals < 0)
                            {
                                StatusTextBlock.Text = "Max removals must be a non-negative integer.";
                                return;
                            }
                        }

                        if (listAllowAdd)
                        {
                            if (!int.TryParse(MaxAdditionsTextBox.Text, out listMaxAdditions) || listMaxAdditions < 0)
                            {
                                StatusTextBlock.Text = "Max additions must be a non-negative integer.";
                                return;
                            }
                        }

                        if (listAllowShift)
                        {
                            if (!int.TryParse(MaxShiftsTextBox.Text, out listMaxShifts) || listMaxShifts < 0)
                            {
                                StatusTextBlock.Text = "Max shifts must be a non-negative integer.";
                                return;
                            }
                        }

                        ListMakerSettings.SetParameters(
                            numberIterations: iteration,
                            removingAllowed: listAllowRemove,
                            addingAllowed: listAllowAdd,
                            allowShifts: listAllowShift,
                            outputDirectory: folder,
                            minResultSize: min!.Value,
                            maxResultSize: max!.Value
                        );

                        ListMakerSettings.SetAllowedMax(
                            maxRemovals: listMaxRemovals,
                            maxAdditions: listMaxAdditions,
                            maxShifts: listMaxShifts
                        );

                        ListMakerSettings.Main();

                        StatusTextBlock.Text = $"Exported List<string> to '{folder}' (size range {min.Value}-{max.Value}, iterations={iteration})";
                        break;

                    // Set
                    case ExportTarget.HashSet:
                        bool setAllowRemove = AllowRemoveCheckBox.IsChecked == true;
                        bool setAllowAdd = AllowAddCheckBox.IsChecked == true;

                        int setMaxRemovals = int.MaxValue;
                        int setMaxAdditions = int.MaxValue;

                        if (setAllowRemove)
                        {
                            if (!int.TryParse(MaxRemovalsTextBox.Text, out setMaxRemovals) || setMaxRemovals < 0)
                            {
                                StatusTextBlock.Text = "Max removals must be a non-negative integer.";
                                return;
                            }
                        }

                        if (setAllowAdd)
                        {
                            if (!int.TryParse(MaxAdditionsTextBox.Text, out setMaxAdditions) || setMaxAdditions < 0)
                            {
                                StatusTextBlock.Text = "Max additions must be a non-negative integer.";
                                return;
                            }
                        }

                        SetMakerSettings.SetParameters(
                            numberIterations: iteration,
                            removingAllowed: setAllowRemove,
                            addingAllowed: setAllowAdd,
                            outputDirectory: folder,
                            minResultSize: min!.Value,
                            maxResultSize: max!.Value
                        );

                        SetMakerSettings.SetAllowedMax(
                            maxRemovals: setMaxRemovals,
                            maxAdditions: setMaxAdditions
                        );

                        SetMakerSettings.Main();

                        StatusTextBlock.Text = $"Exported HashSet<string> to '{folder}' (size range {min.Value}-{max.Value}, iterations={iteration})";
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

        private void AllowAction_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMaxPanelsVisibility();
        }

        private void AllowAction_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateMaxPanelsVisibility();
        }

        private void UpdateMaxPanelsVisibility()
        {
            if (MaxChangesPanel != null) MaxChangesPanel.Visibility = AllowChangeCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (MaxRemovalsPanel != null) MaxRemovalsPanel.Visibility = AllowRemoveCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (MaxAdditionsPanel != null) MaxAdditionsPanel.Visibility = AllowAddCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (MaxShiftsPanel != null) MaxShiftsPanel.Visibility = AllowShiftCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}