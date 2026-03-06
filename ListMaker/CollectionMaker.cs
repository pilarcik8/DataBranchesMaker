using Bogus;
using Microsoft.VisualBasic;
using Shared;
using System;
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
            Run();
        }

        public static void Run()
        {
            var faker = new Faker();
            for (int iteration = 0; iteration < Iterations; iteration++)
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

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                ChangeLogText += $"Allowed Actions: ";
                if (AllowRemove) ChangeLogText += "Remove ";
                if (AllowAdd) ChangeLogText += "Add ";
                if (AllowShift) ChangeLogText += "Shift ";
                ChangeLogText += "\n";
                ChangeLogText += $"Max Allowed Actions: Remove: {MaxAllowedRemovals}, Add: {MaxAllowedAdditions}, Shift: {MaxAllowedShifts}\n";

                int startingNumberOfMOds = 
                    GetRemainingActions(ElementAction.REMOVAL) +
                    GetRemainingActions(ElementAction.SHIFT) +
                    GetRemainingActions(ElementAction.ADDITION);

                if (startingNumberOfMOds == 2)
                {
                    leftKeepProbability = 0.5;
                    rightKeepProbability = 0.5;
                    ChangeLogText += $"Iteration {iteration}: One modification for Left and Base, another for Right and Base\n";
                }
                else
                {
                    ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";
                }

                int leftModificationCount = 0;
                int rightModificationCount = 0;

                // Akcie sa vyberu a rovno vykonaju
                for (int i = 0; i < elementCount; i++)
                {
                    string l = "left_debug" + i + "_";
                    string r = "right_debug_" + i + "_";
                    string b = "base_debug_" + i + "_";

                    var item = ResultList[i];
                    int remainingPositions = elementCount - i;
                    ActualElement = item;
                    ElementAction leftAct, rightAct;
                    string message;

                    bool leftIsKeep = Random.Shared.NextDouble() < leftKeepProbability;
                    if (startingNumberOfMOds == 2 && leftModificationCount > 0)
                    {
                        leftIsKeep = true;
                    }
                    else if (startingNumberOfMOds == 2 && rightModificationCount > 0)
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
                        /*
                        XMLOutput.Export(RightList, r, iteration, OutputDirectory);
                        XMLOutput.Export(BaseList, b, iteration, OutputDirectory);*/
                    }
                    else if (rightAct == ElementAction.KEEP)
                    {
                        message = "L, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(LeftList, BaseList, item, leftAct, faker, iteration);
                        leftModificationCount++;
                        /*
                        XMLOutput.Export(LeftList, l, iteration, OutputDirectory);
                        XMLOutput.Export(BaseList, b, iteration, OutputDirectory);*/
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
                    HashSet<string> leftSet = new HashSet<string>(Shuffle(LeftList), StringComparer.Ordinal);
                    HashSet<string> rightSet = new HashSet<string>(Shuffle(RightList), StringComparer.Ordinal);
                    HashSet<string> baseSet = new HashSet<string>(Shuffle(BaseList), StringComparer.Ordinal);
                    HashSet<string> resultSet = new HashSet<string>(Shuffle(ResultList), StringComparer.Ordinal);

                    XMLOutput.Export(leftSet, "left", iteration, OutputDirectory);
                    XMLOutput.Export(rightSet, "right", iteration, OutputDirectory);
                    XMLOutput.Export(baseSet, "base", iteration, OutputDirectory);
                    XMLOutput.Export(resultSet, "expectedResult", iteration, OutputDirectory);
                }
                // Export changelogu do txt
                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
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
        public static List<string> Shuffle(List<string> list)
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
                    ChangeLogText += $"Removing item: '{item}' at base index: {baseIndex} and branch index: {branchIndex}\n";
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
                if (UsedShiftItems.Contains(item)) throw new Exception($"Item '{item}' has already been used for shifting. Cannot shift the same item multiple times.");

                UsedShiftItems.Add(item);
                int elementAtBaseIndex = baseList.IndexOf(item);

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
                var count = baseListExceptDangerousTargets.Count();
                if (baseListExceptDangerousTargets.Count() == 0) throw new Exception("Žiadne mozne pozicie pre Shift");

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

                var itemBeforeBase = baseIndex > 0 ? baseList[baseIndex - 1] : "NOTHING - IS FIRST";
                var itemBeforeBranch = branchIndex > 0 ? branchList[branchIndex - 1] : "NOTHING - IS FIRST";

                if (itemBeforeBase != itemBeforeBranch)
                {
                    throw new Exception($"Nezhoda v poziciách po Shift: Base pred '{item}' je '{itemBeforeBase}', Branch pred '{item}' je '{itemBeforeBranch}'");
                }

                ChangeLogText += $"Shifting element: '{item}' from index Base:'{oldBaseIndex}', Branch:'{oldBranchIndex}' to 'Base:'{baseIndex}', Branch:'{branchIndex} behind element Base:'{itemBeforeBase}''\n";
                MadeShifts++;
                ChangeLogText += $"Used shift items: {string.Join(", ", UsedShiftItems)}\n";
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

        public static ElementAction ChooseAction(List<string> branchList, int remainingPositions)
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

        public static int GetRemainingActions(ElementAction action)
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

        public static bool CanBeActionExecuted(ElementAction action, List<string> baseList, List<string> branchList)
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

        public static bool ShouldNextActionBeKeep(int remainingPositions)
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

        public static bool TestingOneActionOnce()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 1;
        }

        public static bool TestingOneActionTwice()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 2;
        }

        public static int MaxActionsSum()
        {
            int allowedNumberOfActions = 0;
            allowedNumberOfActions += AllowAdd ? MaxAllowedAdditions : 0;
            allowedNumberOfActions += AllowShift ? MaxAllowedShifts : 0;
            allowedNumberOfActions += AllowRemove ? MaxAllowedRemovals : 0;
            return allowedNumberOfActions;
        }
    }
}