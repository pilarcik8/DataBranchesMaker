using Bogus;
using Bogus.DataSets;
using Microsoft.VisualBasic;
using Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static System.Collections.Specialized.BitVector32;

namespace ListMaker
{
    public static class ListMaker
    {
        private enum ListAction
        {
            KEEP,
            REMOVAL,
            ADDITION,
            SHIFT_SOURCE,
            SHIFT_TARGET
        }

        private struct RowAction
        {
            public ListAction Right;
            public ListAction Left;
            public ListAction Base => Right == ListAction.KEEP ? Left : Right;
            public string shiftTarget;
        }

        // Počet vykonaných akcií v iterácii, resetuje sa na začiatku každej iterácie
        private static int PreparedRemovals = 0;
        private static int PreparedAdditions = 0;
        private static int PreparedShifts = 0;

        private static int EndIteration { get; set; } = 5;
        // Povolenie jednotlivých akcií
        private static bool AllowRemove { get; set; } = true;
        private static bool AllowAdditions { get; set; } = true;
        private static bool AllowShift { get; set; } = true;
        // Maximálny počet povolených akcií (globálne pre všetky iterácie)
        private static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        private static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        private static int MaxAllowedShifts { get; set; } = int.MaxValue;

        // Veľkosť očakávaného výsledku (originálneho xml)
        private static int MaxResultSize { get; set; } = 10;
        private static int MinResultSize { get; set; } = 10;
        private static int RightModificationCount { get; set; } = 0;
        private static int LeftModificationCount { get; set; } = 0;
        private static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ListMakerOutput");
        private static string ChangeLogText = "";
        private static bool WriteSteps { get; set; } = false;

        private static List<string> ResultList = new List<string>();
        private static List<string> RightList = new List<string>();
        private static List<string> LeftList = new List<string>();
        private static List<string> BaseList = new List<string>();
        private static bool TestingOneActionOnce = false;

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, bool shiftingAllowed, string outputDirectory, int minResultSize, int maxResultSize, bool writeSteps)
        {
            if (numberIterations <= 0)
            {
                throw new Exception("Nízka hodnota iterácí");
            }
            if (!removingAllowed && !addingAllowed && !shiftingAllowed)
            {
                throw new Exception("Žiadna operácia nie je povolená");
            }

            if (maxResultSize <= 0 || minResultSize <= 0)
            {
                throw new Exception("Nízka hodnota veľkosti výsledku");
            }

            if (maxResultSize < minResultSize)
            {
                throw new Exception("Maximálna veľkosť výsledku musí být väčšia alebo rovná ako minimálna velikosť výsledku");
            }

            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            EndIteration = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdditions = addingAllowed;
            AllowShift = shiftingAllowed;
            OutputDirectory = outputDirectory;
            WriteSteps = writeSteps;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions, int maxShifts)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
            MaxAllowedShifts = maxShifts;
        }

        public static void Main()
        {
            var faker = new Faker();
            int nunOfAllowedActions = SharedMethods.GetNumberOfAllowedActions(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedShift: AllowShift);
            long numbOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedShift: AllowShift,
                                                                maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxShift: MaxAllowedShifts);
            TestingOneActionOnce = SharedMethods.LearnIfCurrentlyTestingOneActionOnce(nunOfAllowedActions, numbOfMaxMods);
            if (numbOfMaxMods <= 0)
            {
                throw new Exception("Maximálny počet modifikácií musí být väčší ako 0");
            }

            for (int iteration = 0; iteration < EndIteration; iteration++)
            {
                Init(faker);
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                int elementCount = ResultList.Count;

                var rowActions = GenerateRowActions(leftKeepProbability);
                var targerItems = new List<string>();
                var shiftSourceIndices = new List<int>();

                if (AllowShift) { 
                    for (int i = 0; i < rowActions.Count; i++)
                    {
                        if (rowActions[i].Base == ListAction.SHIFT_TARGET)
                        {
                            targerItems.Add(ResultList[i]);
                        }
                        if (rowActions[i].Base == ListAction.SHIFT_SOURCE)
                        {
                            shiftSourceIndices.Add(i);
                        }
                    }

                    targerItems = Shuffle(targerItems);

                    for (int i = 0; i < shiftSourceIndices.Count; i++)
                    {
                        int srcIdx = shiftSourceIndices[i];
                        var ra = rowActions[srcIdx];
                        ra.shiftTarget = targerItems[i];
                        rowActions[srcIdx] = ra;
                    }
                }
                for (int i = 0; i < elementCount; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    var item = ResultList[i];

                    if (rowActions[i].Base == ListAction.KEEP)
                    {
                        ChangeLogText += "L, R, B:\n";
                        ExecuteAction(RightList, BaseList, item, faker, rowActions[i]);
                    }
                    else if (rowActions[i].Left == ListAction.KEEP)
                    {
                        ChangeLogText += "R, B:\n";
                        ExecuteAction(RightList, BaseList, item, faker, rowActions[i]);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(RightList!, rightStepName, null, pathToStepsR);
                            XMLOutput.Export(BaseList!, baseStepName, null, pathToStepsB);
                        }
                    }
                    else if (rowActions[i].Right == ListAction.KEEP)
                    {
                        ChangeLogText += "L, B:\n";
                        ExecuteAction(LeftList, BaseList, item, faker, rowActions[i]);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(LeftList!, leftStepName, null, pathToStepsL);
                            XMLOutput.Export(BaseList!, baseStepName, null, pathToStepsB);
                        }
                    }
                }

                XMLOutput.Export(LeftList, "left", iteration, OutputDirectory);
                XMLOutput.Export(RightList, "right", iteration, OutputDirectory);
                XMLOutput.Export(BaseList, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultList, "expectedResult", iteration, OutputDirectory);

                // Export changelogu do txt
                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                string head = SharedMethods.GetHeadForChangeLog(
                                                                leftKeepProbability: leftKeepProbability,
                                                                iteration: iteration,
                                                                leftModsCount: LeftModificationCount, rightModsCount: RightModificationCount,
                                                                allowAdd: AllowAdditions, allowRemove: AllowRemove, allowShift: AllowShift,
                                                                madeAdditions: PreparedAdditions, madeRemovals: PreparedRemovals, madeShifts: PreparedShifts,
                                                                maxAllowedAdditions: MaxAllowedAdditions, maxAllowedRemovals: MaxAllowedRemovals, maxAllowedShifts: MaxAllowedShifts);
                ChangeLogText = head + ChangeLogText;
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static void Init(Faker faker)
        {
            // Inicializácia
            PreparedAdditions = 0;
            PreparedRemovals = 0;
            PreparedShifts = 0;

            RightModificationCount = 0;
            LeftModificationCount = 0;

            ChangeLogText = string.Empty;

            int elementCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

            ResultList = CreateStartingList(faker, elementCount);
            RightList = new List<string>(ResultList);
            LeftList = new List<string>(ResultList);
            BaseList = new List<string>(ResultList);
            RightList = new List<string>(ResultList);
        }

        private static List<string> CreateStartingList(Faker faker, int elementCount)
        {
            var list = new List<string>();

            while (list.Count < elementCount)
            {
                string value = faker.Random.Word();
                if (!list.Contains(value))
                {
                    list.Add(value);
                }
            }
            return list;
        }

        private static List<RowAction> GenerateRowActions(double leftKeepProbability)
        {
            var actions = new List<RowAction>();

            var sumOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedShift: AllowShift,
                                                              maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxShift: MaxAllowedShifts);
            var sealingOfMods = Math.Min(ResultList.Count, sumOfMaxMods);
            var bottomOfMods = TestingOneActionOnce ? 1 : 2;
            var numberOfMods = Random.Shared.Next(bottomOfMods, (int)(sealingOfMods + 1));

            int actionsGenerated = 0;

            while (LeftModificationCount + RightModificationCount < numberOfMods && actionsGenerated < ResultList.Count)
            {
                // compute remaining mods (not remaining actions)
                int remainingMods = numberOfMods - (LeftModificationCount + RightModificationCount);
                List<ListAction> actionSequence;
                try
                {
                    actionSequence = GetNextActions(ResultList.Count - actionsGenerated, remainingMods);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                actionsGenerated += actionSequence.Count;

                // rozhodneme stranu
                var isleft = SharedMethods.ShouldNextModificationBeOnLeft(LeftModificationCount, RightModificationCount, leftKeepProbability);
                foreach (var act in actionSequence)
                {
                    if (isleft)
                    {
                        actions.Add(new RowAction { Left = act, Right = ListAction.KEEP });
                        if (act != ListAction.KEEP && act != ListAction.SHIFT_TARGET) LeftModificationCount++;
                    }
                    else
                    {
                        actions.Add(new RowAction { Left = ListAction.KEEP, Right = act });
                        if (act != ListAction.KEEP && act != ListAction.SHIFT_TARGET) RightModificationCount++;
                    }
                }
            }

            if (!TestingOneActionOnce)
            {
                // môže nastať pri malom zozname alebo lebo SHIFT potrebuje 3 - 4 prvky na 1 operáciu
                if (LeftModificationCount + RightModificationCount < 2)
                {
                    throw new Exception("Zadané nastavanie tvoria nechcené výsledky. Upravte veľkosť zoznamu alebo vypnite SHIFT");
                }

                if (LeftModificationCount == 0 || RightModificationCount == 0)
                {
                    throw new Exception("Vygenerovaný výsledok neobsahuje modifikácie na jednej strane. Chyba generátora.");
                }
            }

            // doplníme KEEP akcie
            while (actions.Count < ResultList.Count)
            {
                var randIndex = Random.Shared.Next(actions.Count + 1);
                actions.Insert(randIndex, new RowAction { Left = ListAction.KEEP, Right = ListAction.KEEP });
            }
            return actions;
        }

        private static List<ListAction> GetNextActions(int remainingElements, int reamingMods)
        {
            var allowed = new List<ListAction>();

            if (AllowAdditions && MaxAllowedAdditions > PreparedAdditions) allowed.Add(ListAction.ADDITION);
            if (AllowRemove && MaxAllowedRemovals > PreparedRemovals) allowed.Add(ListAction.REMOVAL);
            if (AllowShift && MaxAllowedShifts > PreparedShifts && remainingElements >= 3) allowed.Add(ListAction.SHIFT_SOURCE);

            if (allowed.Count == 0) throw new InvalidOperationException("Všetky akcie boli už spotrebované");

            var actions = new List<ListAction> { };
            var action = allowed[Random.Shared.Next(allowed.Count)];
            switch (action)
            {
                case ListAction.ADDITION:
                    actions.Add(ListAction.ADDITION);
                    PreparedAdditions++;
                    break;

                case ListAction.REMOVAL:
                    actions = GenerateSafeRemovalSequence(remainingElements, reamingMods);
                    PreparedRemovals++;
                    break;

                case ListAction.SHIFT_SOURCE:
                    actions = GenerateSafeShiftSequence(remainingElements, reamingMods);
                    PreparedShifts++;
                    break;
            }
            return actions;
        }

        private static List<ListAction> GenerateSafeRemovalSequence(int remainingElements, int reamingMods)
        {
            var sequence = new List<ListAction>() { ListAction.REMOVAL };
            if (remainingElements > 1) sequence.Add(GetAlwaysSafeAction(reamingMods));
            return sequence;
        }

        private static List<ListAction> GenerateSafeShiftSequence(int remainingElements, int reamingMods)
        {
            var list = new List<ListAction>() { ListAction.SHIFT_SOURCE };
            int rndIndex = Random.Shared.Next(2);
            list.Insert(rndIndex, ListAction.SHIFT_TARGET);
            list.Insert(1, GetAlwaysSafeAction(reamingMods)); //medzi
            reamingMods--;
            if (remainingElements > 3 && list.Last() == ListAction.SHIFT_SOURCE)
            {
                list.Add(GetAlwaysSafeAction(reamingMods)); //za
            }
            return list;
        }

        private static ListAction GetAlwaysSafeAction(int reamingMods)
        {
            var allowed = new List<ListAction>() { ListAction.KEEP };
            if (AllowAdditions && MaxAllowedAdditions > PreparedAdditions && reamingMods > 1) allowed.Add(ListAction.ADDITION);
            int rndIndex = Random.Shared.Next(allowed.Count);
            var action = allowed[rndIndex];
            if (action == ListAction.ADDITION) PreparedAdditions++;
            return action;
        }

        private static List<string> Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, Faker faker, RowAction actions)
        {
            var action = actions.Base;

            if (action == ListAction.KEEP)
            {
                ChangeLogText += $"Keeping item: '{item}'\n";
            }
            else if (action == ListAction.REMOVAL)
            {
                int baseIndex = baseList.IndexOf(item);
                int branchIndex = branchList.IndexOf(item);

                ChangeLogText += $"Removing item: '{item}' at base index: '{baseIndex}' and branch index: '{branchIndex}'\n";

                baseList.Remove(item);
                branchList.Remove(item);
            }
            else if (action == ListAction.ADDITION)
            {
                string newItem = SharedMethods.GetNewUniqueWord(faker, BaseList, LeftList, RightList, ResultList);

                int branchIndex = branchList.IndexOf(item);
                int baseIndex = baseList.IndexOf(item);

                baseList.Insert(baseIndex, newItem);
                branchList.Insert(branchIndex, newItem);

                ChangeLogText += $"Adding item: '{newItem}' at base index: '{baseIndex}' and branch index: '{branchIndex} behind element '{item}'\n";
            }
            else if (action == ListAction.SHIFT_SOURCE)
            {
                var targetItem = actions.shiftTarget;
                if (string.IsNullOrEmpty(targetItem))
                {
                    ChangeLogText += $"Shift skipped: no shiftTarget assigned for item '{item}'\n";
                    return;
                }

                int oldBaseIndex = baseList.IndexOf(item);
                int oldBranchIndex = branchList.IndexOf(item);

                int targetBaseIndex = baseList.IndexOf(targetItem);
                int targetBranchIndex = branchList.IndexOf(targetItem);
                if (targetBaseIndex < 0 || targetBranchIndex < 0)
                {
                    ChangeLogText += $"Shift skipped: target '{targetItem}' not present for source '{item}'\n";
                    return;
                }

                baseList.Remove(item);
                branchList.Remove(item);

                baseList.Insert(targetBaseIndex, item);
                branchList.Insert(targetBranchIndex, item);

                int newBaseIndex = baseList.IndexOf(item);
                int newBranchIndex = branchList.IndexOf(item);

                ChangeLogText += $"Shifting element: '{item}' from Base:{oldBaseIndex},Branch:{oldBranchIndex} to Base:{newBaseIndex},Branch:{newBranchIndex} behind '{targetItem}'\n";
            }
            else if (action == ListAction.SHIFT_TARGET)
            {
                ChangeLogText += $"Keeping item: '{item}' - SHIFT TARGET\n";
            }
        }
    }
}