using Bogus;
using Bogus.DataSets;
using Microsoft.VisualBasic;
using Shared;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace CollectionMaker
{
    public enum ElementAction
    {
        KEEP,
        REMOVAL,
        ADDITION,
        SHIFT
    }

    public static class CollectionMaker
    {
        // Počet vykonaných akcií v iterácii, resetuje sa na začiatku každej iterácie
        private static int MadeRemovals = 0;
        private static int MadeAdditions = 0;
        private static int MadeShifts = 0;

        public static int Iterations { get; set; } = 5;
        // Povolenie jednotlivých akcií
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdd { get; set; } = true;
        public static bool AllowShift { get; set; } = true;
        // Maximálny počet povolených akcií (globálne pre všetky iterácie)
        public static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        public static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        public static int MaxAllowedShifts { get; set; } = int.MaxValue;

        // Veľkosť očakávaného výsledku (originálneho xml)
        public static int MaxResultSize { get; set; } = 10;
        public static int MinResultSize { get; set; } = 10;
        public static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ListMakerOutput");
        public static string ChangeLogText = "";

        public static string ActualElement = string.Empty;

        // Pomocná premenná, ktorá zabezpečí, že po REMOVE musí nasledovat KEEP
        // pri odstraneny 2 za sebou iducich elementov merger nevie presne poradie = xmldiff nahodne vyberie ale sharpdifflib oznaci ako konflikt
        public static bool NextWillBeKeep = false;

        // True = poradie záleží (List), False = poradie nezáleží (Set)
        public static bool OrderMatters = true;

        public static HashSet<string> UsedShiftItems = new HashSet<string>();

        public static List<string> ResultList = new List<string>();
        public static List<string> RightList = new List<string>();
        public static List<string> LeftList = new List<string>();
        public static List<string> BaseList = new List<string>();

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, bool allowShifts, string outputDirectory, int minResultSize, int maxResultSize, bool orderMatters)
        {
            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            AllowShift = allowShifts;
            OutputDirectory = outputDirectory;
            OrderMatters = orderMatters;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions, int maxShifts)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
            MaxAllowedShifts = maxShifts;
        }

        public static void Main()
        {
            bool writeSteps = true; // todo: gui

            var faker = new Faker();
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Init(faker);
                var elementCount = ResultList.Count;
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;

                int startingNumberOfMods =
                    GetRemainingActions(ElementAction.REMOVAL) +
                    GetRemainingActions(ElementAction.SHIFT) +
                    GetRemainingActions(ElementAction.ADDITION);

                int leftModificationCount = 0;
                int rightModificationCount = 0;

                // Akcie sa vyberu a rovno vykonaju
                for (int i = 0; i < elementCount; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string l = "left_step" + i;
                    string r = "right_step" + i;
                    string b = "base_step" + i;

                    var item = ResultList[i];
                    int remainingPositions = elementCount - i;
                    ActualElement = item;
                    ElementAction leftAct, rightAct;
                    string message;

                    bool leftIsKeep = Random.Shared.NextDouble() < leftKeepProbability;
                    if (startingNumberOfMods == 2 && leftModificationCount > 0)
                    {
                        leftIsKeep = true;
                    }
                    else if (startingNumberOfMods == 2 && rightModificationCount > 0)
                    {
                        leftIsKeep = false;
                    }                     

                    if (leftIsKeep)
                    {
                        leftAct = ElementAction.KEEP;
                        rightAct = ChooseAction(RightList, remainingPositions);
                    }
                    else
                    {
                        leftAct = ChooseAction(LeftList, remainingPositions);
                        rightAct = ElementAction.KEEP;
                    }

                    if (leftAct == ElementAction.KEEP && rightAct == ElementAction.KEEP)
                    {
                        message = "L, R, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(RightList, BaseList, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == ElementAction.KEEP)
                    {
                        message = "R, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(RightList, BaseList, item, rightAct, faker, iteration);
                        rightModificationCount++;
                        if (writeSteps)
                        {
                            XMLOutput.Export(RightList, r, null, pathToStepsR);
                            XMLOutput.Export(BaseList, b, null, pathToStepsB);
                        }
                    }
                    else if (rightAct == ElementAction.KEEP)
                    {
                        message = "L, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(LeftList, BaseList, item, leftAct, faker, iteration);
                        leftModificationCount++;
                        if (writeSteps)
                        {
                            XMLOutput.Export(LeftList, l, null, pathToStepsL);
                            XMLOutput.Export(BaseList, b, null, pathToStepsB);
                        }
                    }
                }

                // Export Listov do XML
                if (OrderMatters)
                {
                    XMLOutput.Export(LeftList, "left", iteration, OutputDirectory);
                    XMLOutput.Export(RightList, "right", iteration, OutputDirectory);
                    XMLOutput.Export(BaseList, "base", iteration, OutputDirectory);
                    XMLOutput.Export(ResultList, "expectedResult", iteration, OutputDirectory);
                }
                // Ak nezalezi na poradi, exportujeme ako sety
                else
                {
                    ExportAsSet(iteration, true); // TODO: shuffle volitelny - gui
                }
                // Export changelogu do txt
                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());

                Directory.CreateDirectory(iterationDir);
                ChangeLogText = WriteHeadForChangeLog(leftKeepProbability, iteration, startingNumberOfMods) + ChangeLogText;
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static void Init(Faker faker)
        {
            // Inicializácia
            MadeAdditions = 0;
            MadeRemovals = 0;
            MadeShifts = 0;

            ChangeLogText = string.Empty;
            UsedShiftItems.Clear();

            int elementCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

            ResultList = CreateStartingList(faker, elementCount);
            RightList = new List<string>(ResultList);
            LeftList = new List<string>(ResultList);
            BaseList = new List<string>(ResultList);
            RightList = new List<string>(ResultList);
        }

        private static string WriteHeadForChangeLog(double leftKeepProbability, int iteration, int startingNumberOfMods)
        {
            string head = "Allowed Actions: ";
            if (AllowRemove) head += "Remove ";
            if (AllowAdd) head += "Add ";
            if (AllowShift) head += "Shift ";
            head += "\n";

            head += "Max Allowed Actions: ";
            if (AllowRemove) head += $" Remove: {MaxAllowedRemovals} ";
            if (AllowAdd) head += $"Add: {MaxAllowedAdditions} ";
            if (AllowShift) head += $"Shift: {MaxAllowedShifts} ";
            head += "\n";

            head += "Total modifications: ";
            if (AllowRemove) head += $"Removals: {MadeRemovals} ";
            if (AllowAdd) head += $"Additions: {MadeAdditions} ";
            if (AllowShift) head += $"Shifts: {MadeShifts} ";
            head += "\n\n";

            if (startingNumberOfMods == 2)
            {
                head += $"Iteration {iteration}: One modification for Left and Base, another for Right and Base\n";
            }
            else
            {
                head += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {1 - leftKeepProbability:P0}\n";
            }
            return head;
        }

        private static void ExportAsSet(int iteration, bool shuffle)
        {
            if (shuffle)
            {
                LeftList = Shuffle(LeftList);
                RightList = Shuffle(RightList);
                BaseList = Shuffle(BaseList);
                ResultList = Shuffle(ResultList);
            }

            HashSet<string> leftSet = new HashSet<string>(LeftList, StringComparer.Ordinal);
            HashSet<string> rightSet = new HashSet<string>(RightList, StringComparer.Ordinal);
            HashSet<string> baseSet = new HashSet<string>(BaseList, StringComparer.Ordinal);
            HashSet<string> resultSet = new HashSet<string>(ResultList, StringComparer.Ordinal);

            XMLOutput.Export(leftSet, "left", iteration, OutputDirectory);
            XMLOutput.Export(rightSet, "right", iteration, OutputDirectory);
            XMLOutput.Export(baseSet, "base", iteration, OutputDirectory);
            XMLOutput.Export(resultSet, "expectedResult", iteration, OutputDirectory);
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

        // Fisher–Yates algoritmus pre náhodné premiešanie prvkov v zozname
        private static List<string> Shuffle(List<string> list)
        {
            if (OrderMatters) throw new InvalidOperationException("Cannot shuffle list when order matters.");

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, ElementAction action, Faker faker, int iteration)
        {
            if (action == ElementAction.KEEP)
            {
                ChangeLogText += $"Keeping item: '{item}'\n";
            }
            else if (action == ElementAction.REMOVAL)
            {
                int baseIndex = baseList.IndexOf(item);
                int branchIndex = branchList.IndexOf(item);

                if (OrderMatters)
                {
                    NextWillBeKeep = true;
                    ChangeLogText += $"Removing item: '{item}' at base index: '{baseIndex}' and branch index: '{branchIndex}'\n";
                }
                else
                {
                    ChangeLogText += $"Removing item: '{item}'\n";
                }

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ElementAction.ADDITION)
            {
                string newItem = GetNewDistinctWord(faker);

                int branchIndex = branchList.IndexOf(item);
                int baseIndex = baseList.IndexOf(item);

                baseList.Insert(baseIndex, newItem);
                branchList.Insert(branchIndex, newItem);

                if (OrderMatters)
                {
                    ChangeLogText += $"Adding item: '{newItem}' at base index: '{baseIndex}' and branch index: '{branchIndex}'\n";
                }
                else
                {
                    ChangeLogText += $"Adding item: '{newItem}'\n";
                }
                MadeAdditions++;
            }
            else if (action == ElementAction.SHIFT)
            {
                UsedShiftItems.Add(item);
                int elementAtBaseIndex = baseList.IndexOf(item);

                // výmena (swap) susediacich elementov spolu s dalším shiftom môže spôsobiť poradie elementov ktoré sa nedá nájť deteminicky
                if (!TestingOneActionOnce())
                {
                    if (elementAtBaseIndex > 0)
                    {
                        UsedShiftItems.Add(baseList[elementAtBaseIndex - 1]);
                    }
                    if (elementAtBaseIndex < baseList.Count - 1)
                    {
                        UsedShiftItems.Add(baseList[elementAtBaseIndex + 1]);
                    }
                }
                var baseListExceptDangerousTargets = new List<string>(baseList).Except(UsedShiftItems);
                var targetItem = baseListExceptDangerousTargets.ElementAt(Random.Shared.Next(baseListExceptDangerousTargets.Count()));
                while (!branchList.Contains(targetItem)) {
                    targetItem = baseListExceptDangerousTargets.ElementAt(Random.Shared.Next(baseListExceptDangerousTargets.Count()));
                }

                int oldBaseIndex = baseList.IndexOf(item);
                int oldBranchIndex = branchList.IndexOf(item);

                UsedShiftItems.Add(targetItem);

                baseList.Remove(item);
                branchList.Remove(item);

                //uložíme item za targetItem
                int baseIndex = baseList.IndexOf(targetItem) + 1;
                int branchIndex = branchList.IndexOf(targetItem) + 1;

                baseList.Insert(baseIndex, item);
                branchList.Insert(branchIndex, item);

                var itemBeforeBase = baseList[baseIndex - 1];
                var itemBeforeBranch = branchList[branchIndex - 1];

                ChangeLogText += $"Shifting element: '{item}' from index Base:'{oldBaseIndex}', Branch:'{oldBranchIndex}' to 'Base:'{baseIndex}', Branch:'{branchIndex} behind element Base:'{itemBeforeBase}'\n";
                MadeShifts++;

                // dalsí element sa nezmie modifikovať, lebo nastane rovnaký problém ako keď odstránime 2 prvky idúce za sebou vo vedlajsich vetviach (nedeterministicke poradie)
                NextWillBeKeep = true;
            }
        }

        private static string GetNewDistinctWord(Faker faker)
        {
            var word = faker.Random.Word();

            while (BaseList.Contains(word) || LeftList.Contains(word) || RightList.Contains(word) || ResultList.Contains(word))
            {
                word = faker.Random.Word();
            }

            return word;
        }

        private static ElementAction ChooseAction(List<string> branchList, int remainingPositions)
        {
            // Ak nastane REMOVE, ďalšia akcia musí byť KEEP
            if (NextWillBeKeep)
            {
                NextWillBeKeep = false;
                return ElementAction.KEEP;
            }

            // vypočítaj aktuálny zostávajúci počet modifikácií
            int remAdd = GetRemainingActions(ElementAction.ADDITION);
            int remShift = GetRemainingActions(ElementAction.SHIFT);
            int remRem = GetRemainingActions(ElementAction.REMOVAL);
            int remainingMods = remAdd + remShift + remRem;

            if ((remainingMods == 2 && remainingPositions <= 2) || (remainingMods == 1 && remainingPositions <= 1))
            {
                var forced = new List<ElementAction>();
                if (remShift > 0 && CanBeActionExecuted(ElementAction.SHIFT, BaseList, branchList)) forced.Add(ElementAction.SHIFT);
                if (remRem > 0 && CanBeActionExecuted(ElementAction.REMOVAL, BaseList, branchList)) forced.Add(ElementAction.REMOVAL);
                if (remAdd > 0 && CanBeActionExecuted(ElementAction.ADDITION, BaseList, branchList)) forced.Add(ElementAction.ADDITION);

                if (forced.Count > 0)
                    return forced[Random.Shared.Next(forced.Count)];
                // ak nič nie je vykonateľné, pokračujeme ďalej
            }
            
            // Zachovať pravidlo 'po REMOVE musí nasledovať KEEP' (musí byť volané po povinnej kontrole vyššie).
            if (ShouldNextActionBeKeep(remainingPositions))
            {
                return ElementAction.KEEP;
            }

            // existujúca logika výberu (preferovať SHIFT atď.)
            var allowed = new List<ElementAction> { };

            if (CanBeActionExecuted(ElementAction.SHIFT, BaseList, branchList))
            {
                // Shift nastáva iba pri rovnakých pozíciách v L, R a B
                // príležitosť je preto príliš vzácná, že keď sa naskytne, chceme ju využiť
                return ElementAction.SHIFT;

            }
            if (CanBeActionExecuted(ElementAction.REMOVAL, BaseList, branchList))
            {
                allowed.Add(ElementAction.REMOVAL);
            }
            if (CanBeActionExecuted(ElementAction.ADDITION, BaseList, branchList))
            {
                allowed.Add(ElementAction.ADDITION);
            }

            // dôvod: ADD sa vyskytuje príliš často, lebo nemá také obmedzenia ako REMOVE a SHIFT
            if (allowed.Count > 1 && allowed.Contains(ElementAction.ADDITION))
            {
                allowed.Remove(ElementAction.ADDITION);
            }
            // vyber náhodnú akciu z povolených, alebo KEEP ak žiadna modifikácia nie je povolená
            return allowed.Count > 0 ? allowed[Random.Shared.Next(allowed.Count)] : ElementAction.KEEP;
        }

        private static int GetRemainingActions(ElementAction action)
        {
            return action switch
            {
                ElementAction.ADDITION => AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0,
                ElementAction.REMOVAL => AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0,
                ElementAction.SHIFT => AllowShift ? MaxAllowedShifts - MadeShifts : 0,
                ElementAction.KEEP => int.MaxValue, // KEEP nie je obmedzený
                _ => throw new InvalidOperationException($"Unexpected action: {action}")
            };
        }

        private static bool CanBeActionExecuted(ElementAction action, List<string> baseList, List<string> branchList)
        {
            if (GetRemainingActions(action) == 0)
            {
                return false;
            }

            if (action == ElementAction.REMOVAL)
            {
                int baseIndex = baseList.IndexOf(ActualElement);
                int branchIndex = branchList.IndexOf(ActualElement);

                // skontroluj, či sa nachádzajú na rovnakej pozícii
                if (AllowAdd || AllowShift)
                {
                    return (baseIndex == branchIndex);
                }
            }

            if (action == ElementAction.SHIFT)
            {
                if (UsedShiftItems.Contains(ActualElement)) return false;
                if (BaseList.Count - 1 - UsedShiftItems.Count <= 3) return false;
            }
            return true;
        }

        private static bool ShouldNextActionBeKeep(int remainingPositions)
        {
            int remaningAdd = GetRemainingActions(ElementAction.ADDITION);
            int remaningShift = GetRemainingActions(ElementAction.SHIFT);
            int remaningRemove = GetRemainingActions(ElementAction.REMOVAL);

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            int madeMofifications = MadeAdditions + MadeRemovals + MadeShifts;
            int evenChance = Math.Max(1, remainingPositions);

            // jedna modifikacia
            if (TestingOneActionOnce())
            {
                if (remainingPositions == 1 && remainingModifications == 1)
                {
                    return false;
                }
                return Random.Shared.Next(evenChance) != 0;            

            }
            // dve modifikacie
            else if (TestingOneActionTwice())
            {

                if (AllowRemove && remainingPositions <= 3 && remainingModifications != 0)
                {
                    return false;
                }
                if (AllowShift && remainingPositions < ResultList.Count / 3 && remainingModifications != 1)
                {
                    return false;
                }
                if (AllowShift && remainingPositions < ResultList.Count - 2 && remainingModifications != 0)
                {
                    return false;
                }
                if (AllowAdd && remainingPositions <= 2 && remainingModifications != 0)
                {
                    return false;
                }
                return Random.Shared.Next(evenChance) != 0;
            }

            // normalny
            var allowed = new List<ElementAction> { ElementAction.KEEP };
            if (remaningAdd > 0)
            {
                allowed.Add(ElementAction.ADDITION);
            }
            if (remaningRemove > 0)
            {
                allowed.Add(ElementAction.REMOVAL);
            }
            if (remaningShift > 0)
            {
                allowed.Add(ElementAction.SHIFT);
            }

            return allowed[Random.Shared.Next(allowed.Count)] == ElementAction.KEEP;
        }

        private static bool TestingOneActionOnce()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 1;
        }

        private static bool TestingOneActionTwice()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 2;
        }

        private static long MaxActionsSum()
        {
            long allowedNumberOfActions = 0;
            allowedNumberOfActions += AllowAdd ? MaxAllowedAdditions : 0;
            allowedNumberOfActions += AllowShift ? MaxAllowedShifts : 0;
            allowedNumberOfActions += AllowRemove ? MaxAllowedRemovals : 0;
            return allowedNumberOfActions;
        }
    }
}