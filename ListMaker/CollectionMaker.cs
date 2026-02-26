using Bogus;
using Microsoft.VisualBasic;
using Shared;
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

            StartTest(faker);
            return;
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
                        rightAct = ChooseAction(RightList);
                    }
                    else
                    {
                        leftAct = ChooseAction(LeftList);
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
                ChangeLogText += $"Keeping item: '{item}'\n";
            }
            else if (action == ElementAction.REMOVAL)
            {
                if (OrderMatters)
                {
                    NextWillBeKeep = true;
                }

                ChangeLogText += $"Removing item: '{item}'\n";
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
                    ChangeLogText += $"Adding item: '{newItem}' at index '{branchIndex}'\n";
                }
                else
                {
                    ChangeLogText += $"Adding item: '{newItem}'\n";
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

                // Ak už nejde previesť inú akciu ako Shift
                // znížime rozsah, čo zvýši počet krát kedy shift bude
                //      - potrebujeme aby L R a B pozicie boli rovnaké čo nastava až po indexe novej pozicie posledneho Shiftu
                if (GetRemainingActions(ElementAction.REMOVAL) == 0 && GetRemainingActions(ElementAction.ADDITION) == 0)
                {
                    if (maxShiftRange > 15) {
                        maxShiftRange = 15;
                    }
                }

                int minCandidate = int.MaxValue;
                for (int t = 0; t < 3; t++)
                {
                    int offset = Random.Shared.Next(1, maxShiftRange + 1);
                    int candidate = oldIndex + offset;

                    if (candidate < minCandidate)
                        minCandidate = candidate;
                }

                int newIndex = minCandidate;

                branchList.Insert(newIndex, item);
                baseList.Insert(newIndex, item);

                ChangeLogText += $"Shifting item: '{item}' from index {oldIndex} to '{newIndex}'\n";
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

        public static ElementAction ChooseAction(List<string> branchList)
        {
            // ked nastane remove, dalsia akcia musi byt keep, inak merger nerozozna poradie prvkov
            if (ShouldNextActionBeKeep())
            {
                return ElementAction.KEEP;
            }

            var allowed = new List<ElementAction> {};

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
            // Ak nastane REMOVE, ďalšia akcia musí být KEEP, jinak merger nerozozna pořadí prvkov
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

        // METODY PRE TESTOVANIE
        private static void StartTest(Faker faker)
        {
            Console.WriteLine("Zadaj cestu k expectedResult súboru");
            string filePath = Console.ReadLine().Trim('"');

            while (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("Súbor neexistuje, skúste znova");
                filePath = Console.ReadLine().Trim('"');
            }

            List<string> expectedResult = CreateListFromXml(filePath);
            if (expectedResult.Count == 0)
            {
                Console.WriteLine("Bud prázdny alebo nie validný xml súbor - list/set so stringami");
                return;
            }

            var rightList = new List<string>(expectedResult);
            var leftList = new List<string>(expectedResult);
            var baseList = new List<string>(expectedResult);

            Console.WriteLine("Zadajte cestu k changeLog súboru");
            string changeLogPath = Console.ReadLine().Trim('"');

            while (string.IsNullOrWhiteSpace(changeLogPath) || !File.Exists(changeLogPath))
            {
                Console.WriteLine("Súbor neexistuje, skúste znova");
                changeLogPath = Console.ReadLine().Trim('"');
            }

            string[] changeLogLines = File.ReadAllLines(changeLogPath, Encoding.UTF8);
            string nextBranch = "";
            for (int line = 1; line < changeLogLines.Length; line+=2)
            {
                List<string> branchList;

                if (changeLogLines[line].Trim() == "L, R, B:") 
                {
                    continue;
                }
                else if (changeLogLines[line].Trim() == "L, B:")
                {
                    branchList = leftList;
                }
                else if (changeLogLines[line].Trim() == "R, B:")
                {
                    branchList = rightList;
                }
                else
                {
                    throw new Exception("Problem s citanim vetvi");
                }

                string actionText = changeLogLines[line + 1].Trim();
                if (actionText.Contains("Keeping"))
                {
                    continue;
                }
                else if (actionText.Contains("Removing"))
                {
                    Match match = Regex.Match(actionText, @"Removing item:\s*'([^']+)'");

                    if (!match.Success) throw new Exception("Nenajdena hodnota pri Remove");

                    string item = match.Groups[1].Value;

                    branchList.Remove(item);
                    baseList.Remove(item);
                }
                else if (actionText.Contains("Shifting"))
                {
                    Match match = Regex.Match(actionText, @"Shifting item:\s*'([^']+)'.*to\s*'(\d+)'");

                    if (!match.Success) throw new Exception("Nenajdena hodnota pri Shifting");

                    string item = match.Groups[1].Value;
                    int newIndex = int.Parse(match.Groups[2].Value);

                    baseList.Remove(item);
                    branchList.Remove(item);
                    baseList.Insert(newIndex, item);
                    branchList.Insert(newIndex, item);

                }
                else if (actionText.Contains("Adding"))
                {
                    Match match = Regex.Match(actionText, @"Adding item:\s*'([^']+)'\s*at index\s*'(\d+)'");

                    if (!match.Success) throw new Exception("Nenajdena hodnota pri Adding");

                    string newItem = match.Groups[1].Value;
                    int index = int.Parse(match.Groups[2].Value);

                    baseList.Insert(index, newItem);
                    branchList.Insert(index, newItem);
                }
                else
                {
                    throw new Exception("Neznama akcia v changelogu alebo chyba pri citani");
                }
            }
            XMLOutput.Export(leftList, "left", 69, OutputDirectory);
            XMLOutput.Export(rightList, "right", 69, OutputDirectory);
            XMLOutput.Export(baseList, "base", 69, OutputDirectory);
        }

        public static List<string> CreateListFromXml(string xmlPath)
        {
            if (string.IsNullOrWhiteSpace(xmlPath)) throw new ArgumentException("xmlPath is null or empty.", nameof(xmlPath));
            if (!File.Exists(xmlPath)) throw new FileNotFoundException("XML file not found.", xmlPath);

            string xml = File.ReadAllText(xmlPath, Encoding.UTF8);

            // Try List<string>
            try
            {
                var serializer = new XmlSerializer(typeof(List<string>));
                using (var reader = new StringReader(xml))
                {
                    if (serializer.Deserialize(reader) is List<string> list)
                    {
                        return list;
                    }
                }
                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}