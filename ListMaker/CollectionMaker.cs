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

        // True = poradie záleží (List), False = poradie nezáleží (Set)
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
            StartListOutputTest();
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
                    }
                    else if (rightAct == ElementAction.KEEP)
                    {
                        message = "L, B:";
                        ChangeLogText += message + "\n";
                        ExecuteAction(LeftList, BaseList, item, leftAct, faker, iteration);
                        leftModificationCount++;
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
                int index = baseList.IndexOf(item);

                var otherBranch = branchList == LeftList ? RightList : LeftList;

                int top = Math.Min(Math.Min(baseList.Count, branchList.Count), otherBranch.Count);
                var rnd = Random.Shared.Next(index + 1, top - 1);
                bool found = false;

                int attempts = 0;
                while ((baseList[rnd] != branchList[rnd] || otherBranch[rnd] != baseList[rnd]) &&
                    (baseList[rnd + 1] != branchList[rnd + 1] || otherBranch[rnd + 1] != baseList[rnd + 1]))
                {
                    if (attempts == 20) break;

                    rnd = Random.Shared.Next(index + 1, top - 1);
                    attempts++;
                }
                if (attempts == 20)
                {
                    for (int i = index + 1; i < top; i++)
                    {
                        if (baseList[i] == branchList[i] && otherBranch[i] == baseList[i])
                        {
                            rnd = i;
                            found = true;
                            break;
                        }
                    }
                }
                else
                {
                    found = true;
                }

                if (!found) throw new Exception("not found index to land on Shift");

                baseList.Remove(item);
                branchList.Remove(item);
                baseList.Insert(rnd, item);
                branchList.Insert(rnd, item);

                ChangeLogText += $"Shifting item: '{item}' from index {index} to '{rnd}'\n";
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

        public static ElementAction ChooseAction(List<string> branchList, int remainingPositions)
        {
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
                if (baseIndex != branchIndex) return false;
            }

            if (action == ElementAction.SHIFT)
            {
                var otherBranch = branchList == LeftList ? RightList : LeftList;
                // je posledný element?
                if (baseList.IndexOf(ActualElement) >= baseList.Count - 2 ||
                    branchList.IndexOf(ActualElement) >= branchList.Count - 2 ||
                    otherBranch.IndexOf(ActualElement) >= otherBranch.Count - 2) return false;

                // rovnaká pozícia v zoznamoch?
                if (baseList.IndexOf(ActualElement) != branchList.IndexOf(ActualElement) ||
                    baseList.IndexOf(ActualElement) != otherBranch.IndexOf(ActualElement)) return false;

                int top = Math.Min(Math.Min(baseList.Count, branchList.Count), otherBranch.Count) - 1;
                for (int i = baseList.IndexOf(ActualElement) + 1; i < top; i++)
                {
                    if (baseList[i] == branchList[i] && otherBranch[i] == baseList[i])
                    {
                        // skúsiť posun o dve pozície aby sa predišlo opakovaniu na rovnakom mieste
                        if (baseList[i + 1] != branchList[i + 1] || otherBranch[i + 1] != baseList[i + 1]) continue;
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        public static bool ShouldNextActionBeKeep(int remainingPositions)
        {
            // Ak nastane REMOVE, ďalšia akcia musí byť KEEP
            if (NextWillBeKeep)
            {
                NextWillBeKeep = false;
                return true;
            }

            int remaningAdd = GetRemainingActions(ElementAction.ADDITION);
            int remaningShift = GetRemainingActions(ElementAction.SHIFT);
            int remaningRemove = GetRemainingActions(ElementAction.REMOVAL);

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            int madeMofifications = MadeAdditions + MadeRemovals + MadeShifts;

            // ak zostáva jedna alebo dve modifikácie, spravme rozloženie rovnomerne
            if (remainingModifications <= 2 && remainingModifications > 0)
            {
                // ak máme presne 2 modifikácie a zostávajú 2 pozície a ešte žiadna modifikácia nebola vykonaná, vynútime modifikáciu
                if (remainingPositions == 2 && remainingModifications == 2 && madeMofifications == 0)
                {
                    return false;
                }
                // ak sme na poslednej pozícii a už bola vykonaná aspoň jedna modifikácia, vynútime druhú
                if (remainingPositions == 1 && remainingModifications == 1 && madeMofifications >= 1)
                {
                    return false;
                }

                remainingPositions = Math.Max(1, remainingPositions);
                if (Random.Shared.Next(remainingPositions) != 0)
                {
                    return true;
                }
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

        // METÓDY PRE TESTOVANIE
        private static void StartListOutputTest()
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

            for (int line = 4; line < changeLogLines.Length; line += 2)
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
                    Match match = Regex.Match(actionText, @"Adding item:\s*'([^']+)'.*base index:\s*'(\d+)'.*branch index:\s*'(\d+)'");

                    if (!match.Success) throw new Exception("Nenajdena hodnota pri Adding");

                    string newItem = match.Groups[1].Value;
                    int baseIndex = int.Parse(match.Groups[2].Value);
                    int branchIndex = int.Parse(match.Groups[3].Value);

                    baseList.Insert(baseIndex, newItem);
                    branchList.Insert(branchIndex, newItem);
                }
                else
                {
                    throw new Exception("Neznama akcia v changelogu alebo chyba pri citani");
                }
            }
            XMLOutput.Export(leftList, "left", 0, OutputDirectory);
            XMLOutput.Export(rightList, "right", 0, OutputDirectory);
            XMLOutput.Export(baseList, "base", 0, OutputDirectory);
        }

        private static List<string> CreateListFromXml(string xmlPath)
        {
            if (string.IsNullOrWhiteSpace(xmlPath)) throw new ArgumentException("xmlPath is null or empty.", nameof(xmlPath));
            if (!File.Exists(xmlPath)) throw new FileNotFoundException("XML file not found.", xmlPath);

            string xml = File.ReadAllText(xmlPath, Encoding.UTF8);

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