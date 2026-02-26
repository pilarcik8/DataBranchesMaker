using Bogus;
using Shared;
using System.Text;

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

        // Pomocná premenná, ktorá zabezpečí, že po REMOVE musí nasledovat KEEP, inak merger nerozozna poradie prvkov
        public static bool NextWillBeKeep = false;

        // True = List / False = Set
        public static bool OrderMatters = true;

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
            var faker = new Faker();


            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                // Inicializácia
                MadeAdditions = 0;
                MadeRemovals = 0;
                MadeShifts = 0;

                ChangeLogText = string.Empty;

                int elementCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

                ResultList = CreateStartingList(faker, elementCount);
                RightList = new List<string>(ResultList); 
                LeftList = new List<string>(ResultList); 
                BaseList = new List<string>(ResultList);
                RightList = new List<string>(ResultList);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";

                // Akcie sa vyberu a rovno vykonaju
                foreach (string item in ResultList)
                {
                    ActualElement = item;
                    ElementAction leftAct, rightAct;
                    string message;
                    if (Random.Shared.NextDouble() < leftKeepProbability)
                    {
                        leftAct = ElementAction.KEEP;
                        rightAct = GetAction(RightList);
                    }
                    else
                    {
                        leftAct = GetAction(LeftList);
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
            else if (action == ElementAction.REMOVAL)
            {
                if (OrderMatters)
                {
                    NextWillBeKeep = true;
                }

                ChangeLogText += $"Removing item: {item}\n";
                int baseIndex = baseList.IndexOf(item);
                int branchIndex = branchList.IndexOf(item);

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ElementAction.ADDITION)
            {

                string newItem = GetNewDistinctWord(faker);

                int branchIndex = branchList.IndexOf(item);
                branchList.Insert(branchIndex, newItem);

                int baseIndex = baseList.IndexOf(item);
                baseList.Insert(baseIndex, newItem);

                if (OrderMatters)
                {
                    ChangeLogText += $"Adding item: {newItem} at index {branchIndex}\n";
                }
                else
                {
                    ChangeLogText += $"Adding item: {newItem}\n";
                }
                MadeAdditions++;
            }
            else if (action == ElementAction.SHIFT)
            {
                int oldIndex = baseList.IndexOf(item);

                // odstráň podľa indexu (očakávam že sú na rovnakej pozícii)
                baseList.RemoveAt(oldIndex);
                branchList.RemoveAt(oldIndex);

                int maxShiftRange = baseList.Count - oldIndex;
                if (maxShiftRange <= 0)
                    return; // nič za tým už nie je

                int minCandidate = int.MaxValue;

                for (int i = 0; i < 3; i++)
                {
                    int offset = Random.Shared.Next(1, maxShiftRange + 1);
                    int candidate = oldIndex + offset;

                    if (candidate < minCandidate)
                        minCandidate = candidate;
                }

                int newIndex = minCandidate;

                branchList.Insert(newIndex, item);
                baseList.Insert(newIndex, item);

                ChangeLogText += $"Shifting item: '{item}' from index {oldIndex} to {newIndex}\n";
                MadeShifts++;
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

        public static ElementAction GetAction(List<string> branchList)
        {
            // ked nastane remove, dalsia akcia musi byt keep, inak merger nerozozna poradie prvkov
            if (ShouldNextActionBeKeep())
            {
                return ElementAction.KEEP;
            }

            var allowed = new List<ElementAction> {};

            if (CanBeActionExecuted(ElementAction.SHIFT, BaseList, branchList))
            {
                allowed.Add(ElementAction.SHIFT);

            }
            if (CanBeActionExecuted(ElementAction.REMOVAL, BaseList, branchList))
            {
                allowed.Add(ElementAction.REMOVAL);
            }
            if (CanBeActionExecuted(ElementAction.ADDITION, BaseList, branchList))
            {
                allowed.Add(ElementAction.ADDITION);
            }

            // vyber náhodnú akciu z povolených, alebo KEEP ak žiadna modifikácia není povolená
            return allowed.Count > 0 ? allowed[Random.Shared.Next(allowed.Count)] : ElementAction.KEEP;
        }

        public static int GetRemainingActions(ElementAction action)
        {
            return action switch
            {
                ElementAction.ADDITION => AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0,
                ElementAction.REMOVAL => AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0,
                ElementAction.SHIFT => AllowShift ? MaxAllowedShifts - MadeShifts : 0,
                ElementAction.KEEP => int.MaxValue, // Keep není omezený, protože není modifikací
                _ => throw new InvalidOperationException($"Unexpected action: {action}")
            };
        }

        public static bool CanBeActionExecuted(ElementAction action, List<string> branch1, List<string> branch2)
        {
            if (GetRemainingActions(action) == 0)
            {
                return false;
            }

            if (action == ElementAction.SHIFT)
            {
                // je posledny element?
                if (branch1.IndexOf(ActualElement) >= branch1.Count - 2 || branch2.IndexOf(ActualElement) >= branch2.Count - 2) return false;

                // rovnaka pozicia v listoch?
                if (BaseList.IndexOf(ActualElement) != RightList.IndexOf(ActualElement) || BaseList.IndexOf(ActualElement) != LeftList.IndexOf(ActualElement)) return false;
            }

            return true;
        }

        public static bool ShouldNextActionBeKeep()
        {
            // Ak nastane REMOVE, ďalšia akcia musí být KEEP, jinak merger nerozozna pořadí prvků
            if (NextWillBeKeep)
            {
                NextWillBeKeep = false;
                return true;
            }

            int remaningAdd = GetRemainingActions(ElementAction.ADDITION);
            int remaningShift = GetRemainingActions(ElementAction.SHIFT);
            int remaningRemove = GetRemainingActions(ElementAction.REMOVAL);

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            // Ak zostáva len jedna možná modifikácia, znížime pravdepodobnosť jej výberu
            // dôvod: ak napr. testujem či akcia funguje a teda Max operacie dam 1, nechem aby sa vykonala hned na zaciatku
            if (remainingModifications == 1)
            {
                int randomValue = Random.Shared.Next(5);
                if (randomValue != 0) return true; // 80% že bude Keep 
            }

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
    }
}