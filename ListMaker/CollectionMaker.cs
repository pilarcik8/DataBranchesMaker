using System;
using System.IO;
using System.Text;
using Bogus;
using Shared;

namespace CollectionMaker
{
    public enum ElementAction
    {
        KEEP,
        REMOVE,
        ADD,
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
        public static int BaseSize;

        public static string ActualElement = string.Empty;

        // Pomocná premenná, ktorá zabezpečí, že po REMOVE musí nasledovat KEEP, inak merger nerozozna poradie prvkov
        public static bool NextWillBeKeep = false;

        // True = List / False = Set
        public static bool OrderMatters = true;

        public static List<string>? ResultList;
        public static List<string>? RightList;
        public static List<string>? LeftList;
        public static List<string>? BaseList;

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
            var faker = new Faker();
            MadeAdditions = 0;
            MadeRemovals = 0;
            MadeShifts = 0;

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                ChangeLogText = string.Empty;

                int targetCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

                ResultList = new List<string>();

                while (ResultList.Count < targetCount)
                {
                    string value = faker.Random.Word();
                    if (!ResultList.Contains(value))
                    {
                        ResultList.Add(value);
                    }
                }

                RightList = new List<string>(ResultList);
                LeftList = new List<string>(ResultList);
                BaseList = new List<string>(ResultList);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";

                foreach (string item in ResultList)
                {
                    ActualElement = item;
                    ElementAction leftAct, rightAct;
                    BaseSize = BaseList.Count;
                    string message;
                    if (Random.Shared.NextDouble() < leftKeepProbability)
                    {
                        leftAct = ElementAction.KEEP;
                        rightAct = GetAction();
                    }
                    else
                    {
                        leftAct = GetAction();
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
                    }
                    else if (rightAct == ElementAction.KEEP)
                    {
                        message = "L, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(LeftList, BaseList, item, leftAct, faker, iteration);

                    }
                    else
                    {
                        Console.Error.WriteLine("Nezanama akcia");
                        return;
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

        // Fisher-Yates shuffle algoritmus pre náhodné premiešanie prvkov v liste
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
                ChangeLogText += $"Keeping item: {item}\n";
            }
            else if (action == ElementAction.REMOVE)
            {
                ChangeLogText += $"Removing item: {item}\n";

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ElementAction.ADD)
            {
                string newItem = faker.Random.Word();
                // Hladaj uplne nove slovo
                while (BaseList.Contains(newItem) || LeftList.Contains(newItem) || RightList.Contains(newItem) || ResultList.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }

                int currentIndex = branchList.IndexOf(item);

                branchList.Insert(currentIndex, newItem);

                int baseIndex = baseList.IndexOf(item);
                if (baseIndex >= 0)
                {
                    baseList.Insert(baseIndex, newItem);
                }
                else
                {
                    baseList.Add(newItem);
                }

                if (OrderMatters)
                {
                    ChangeLogText += $"Adding item: {newItem} at index {currentIndex}\n";
                }
                else
                {
                    ChangeLogText += $"Adding item: {newItem}\n";
                }
                MadeAdditions++;
            }
            else if (action == ElementAction.SHIFT)
            {
                if (!OrderMatters) throw new InvalidOperationException("Cannot perform SHIFT action when order does not matter.");
                
                if (BaseList!.IndexOf(ActualElement) != LeftList!.IndexOf(ActualElement) && 
                    RightList!.IndexOf(ActualElement) != LeftList!.IndexOf(ActualElement))
                {
                    throw new InvalidOperationException($"Cannot shift item '{item}' because it is not in the same position in all lists.\n");
                }

                int currentIndex = branchList.IndexOf(item);
                int newIndex = Random.Shared.Next(branchList.Count);
                while (newIndex <= currentIndex)
                {
                    newIndex = Random.Shared.Next(branchList.Count);
                }
                branchList.Remove(item);
                branchList.Insert(newIndex, item);
                baseList.Remove(item);
                baseList.Insert(newIndex, item);

                ChangeLogText += $"Shifting item: '{item}' from index {currentIndex} to {newIndex}\n";
                MadeShifts++;
            }
        }

        public static ElementAction GetAction()
        {
            // ked nastane remove, dalsia akcia musi byt keep, inak merger nerozozna poradie prvkov
            if (NextWillBeKeep)
            {
                NextWillBeKeep = false;
                return ElementAction.KEEP;
            }
            var allowed = new List<ElementAction> { ElementAction.KEEP };

            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningShift = AllowShift ? MaxAllowedShifts - MadeShifts : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            if (remainingModifications == 1)
            {
                int randomValue = Random.Shared.Next(5);
                if (randomValue != 0) return ElementAction.KEEP;
            }

            if (remaningShift > 0)
            {
                // Shift je povolený len v prípade, že aktuálny element je na rovnakej pozícii
                // inak by merger nerozoznal poradie prvkov
                if (BaseList.IndexOf(ActualElement) == LeftList.IndexOf(ActualElement) && 
                    RightList.IndexOf(ActualElement) == LeftList.IndexOf(ActualElement))
                {
                    // Shift je povolený len v prípade, že aktuálny element nie je posledný v Listoch
                    // kód na exekúciu shiftu by totiž posúva elementy iba z hora dole, nikdy z dola nahor
                    if (BaseList.IndexOf(ActualElement) != BaseList.Count - 1)
                    {
                        allowed.Add(ElementAction.SHIFT);
                    }
                }
            }
            if (remaningRemove > 0)
            {
                allowed.Add(ElementAction.REMOVE);
                NextWillBeKeep = true;
            }
            if (remaningAdd > 0)
            {
                allowed.Add(ElementAction.ADD);
            }

            int index = Random.Shared.Next(allowed.Count);
            return allowed[index];
        }
    }
}