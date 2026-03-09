using System.Windows;
using PeopleMaker = PersonMaker.PersonMaker;
using ListsMaker = ListMaker.ListMaker;
using SetsMaker = SetMaker.SetMaker;

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
            UpdateShuffleVisibility();
        }

        private void UpdateSizeInputsVisibility()
        {
            // min a max size sú relevantné len pre Set a List, takže ich panel zobrazíme len ak je vybraný Set alebo List
            bool showSize = (SetCheckBox.IsChecked == true) || (ListCheckBox.IsChecked == true);
            SizeInputsPanel.Visibility = showSize ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateShuffleVisibility()
        {
            // Shuffle je relevantný len pre Set, takže jeho checkbox zobrazíme len ak je vybraný Set
            ShuffleBranchesCheckBox.Visibility = SetCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAllowedActionsForTarget()
        {
            bool isSet = SetCheckBox.IsChecked == true;
            bool isList = ListCheckBox.IsChecked == true;
            bool isClass = ClassCheckBox.IsChecked == true;

            // Set: Add + Remove
            if (isSet)
            {
                ShuffleBranchesCheckBox.IsChecked = false;
                // schopnosť zakliknúť Add a Remove necháme, ale Change a Shift zakážeme
                AllowAddCheckBox.IsEnabled = true;
                AllowRemoveCheckBox.IsEnabled = true;
                AllowChangeCheckBox.IsEnabled = false;
                AllowShiftCheckBox.IsEnabled = false;

                if (AllowAddCheckBox.IsChecked != true) AllowAddCheckBox.IsChecked = true;
                if (AllowRemoveCheckBox.IsChecked != true) AllowRemoveCheckBox.IsChecked = true;

                AllowChangeCheckBox.IsChecked = false;
                AllowShiftCheckBox.IsChecked = false;
            }
            // List: Add, Change, Shift
            else if (isList)
            {
                ShuffleBranchesCheckBox.IsChecked = false;

                AllowAddCheckBox.IsEnabled = true;
                AllowRemoveCheckBox.IsEnabled = true;
                AllowChangeCheckBox.IsEnabled = false;
                AllowShiftCheckBox.IsEnabled = true;

                if (AllowAddCheckBox.IsChecked != true) AllowAddCheckBox.IsChecked = true;
                if (AllowRemoveCheckBox.IsChecked != true) AllowRemoveCheckBox.IsChecked = true;
                if (AllowShiftCheckBox.IsChecked != true) AllowShiftCheckBox.IsChecked = true;

                AllowChangeCheckBox.IsChecked = false;
            }
            // Class(Person): Add, Remove, Change
            else if (isClass)
            {
                ShuffleBranchesCheckBox.IsChecked = false;

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
                if (!int.TryParse(IterationTextBox.Text, out int iteration) || iteration <= 0)
                {
                    StatusTextBlock.Text = "Iteration must be a positive integer.";
                    return;
                }

                var selected = GetSelectedTarget();
                string folder = string.IsNullOrWhiteSpace(FolderTextBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : FolderTextBox.Text.Trim();

                int? min = null;
                int? max = null;
                bool? writeSteps = null;

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

                        writeSteps = WriteStepsCheckBox.IsChecked == true;

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

                        PeopleMaker.SetParameters(
                            numberIterations: iteration,
                            changesAllowed: allowChange,
                            removingAllowed: allowRemove,
                            addingAllowed: allowAdd,
                            outputDirectory: folder,
                            writeSteps: writeSteps.Value
                        );

                        PeopleMaker.SetMaxAllowed(
                            maxChanges: maxChanges,
                            maxRemovals: maxRemovals,
                            maxAdditions: maxAdditions
                        );

                        PeopleMaker.Main();

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

                        writeSteps = WriteStepsCheckBox.IsChecked == true;

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
                        
                        ListsMaker.SetParameters(
                            numberIterations: iteration,
                            removingAllowed: listAllowRemove,
                            addingAllowed: listAllowAdd,
                            allowShifts: listAllowShift,
                            outputDirectory: folder,
                            minResultSize: min!.Value,
                            maxResultSize: max!.Value,
                            writeSteps: writeSteps.Value
                        );

                        ListsMaker.SetAllowedMax(
                            maxRemovals: listMaxRemovals,
                            maxAdditions: listMaxAdditions,
                            maxShifts: listMaxShifts
                        );

                        ListsMaker.Main();
                        
                        StatusTextBlock.Text = $"Exported List<string> to '{folder}' (size range {min.Value}-{max.Value}, iterations={iteration})";
                        break;

                    // Set
                    case ExportTarget.HashSet:
                        bool setAllowRemove = AllowRemoveCheckBox.IsChecked == true;
                        bool setAllowAdd = AllowAddCheckBox.IsChecked == true;

                        int setMaxRemovals = int.MaxValue;
                        int setMaxAdditions = int.MaxValue;

                        bool shuffle = ShuffleBranchesCheckBox.IsChecked == true;
                        writeSteps = WriteStepsCheckBox.IsChecked == true;

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
                        SetsMaker.SetParameters(
                            numberIterations: iteration,
                            removingAllowed: setAllowRemove,
                            addingAllowed: setAllowAdd,
                            outputDirectory: folder,
                            minResultSize: min!.Value,
                            maxResultSize: max!.Value,
                            shuffle: shuffle,
                            writeSteps: writeSteps!.Value
                        );

                        SetsMaker.SetAllowedMax(
                            maxRemovals: setMaxRemovals,
                            maxAdditions: setMaxAdditions
                        );

                        SetsMaker.Main();

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