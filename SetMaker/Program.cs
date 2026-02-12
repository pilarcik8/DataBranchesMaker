using Bogus;
using System.Text;
using System.Xml.Serialization;

namespace TestKniznice
{
    public enum SetAction
    {
        KEEP,
        REMOVE,
        ADD
    }

    public static class Program
    {
        static int MadeRemovals;
        static int MadeAdditions;
        static int ActualIteration;
        static HashSet<string> ClearedLogFiles = new HashSet<string>();

        //Konfigurácia generovania dát
        const int MAX_RESULT_SET_SIZE = 10;
        const int MIN_RESULT_SET_SIZE = 10;

        const int ITERATIONS = 1;

        const bool ALLOW_REMOVE = true;
        const bool ALLOW_ADD = true;

        const int MAX_ALLOWED_REMOVALS = int.MaxValue;
        const int MAX_ALLOWED_ADDITIONS = int.MaxValue;

        public static void Main()
        {
            // Validácia konfigurácie
            if (MAX_RESULT_SET_SIZE < MIN_RESULT_SET_SIZE || 
                MIN_RESULT_SET_SIZE <= 0 ||
                MAX_RESULT_SET_SIZE <= 0)
            {
                Console.Error.WriteLine("Set s takymi velkostami nie je mozne vytvoriť");
                return;
            }

            if (!ALLOW_ADD && !ALLOW_REMOVE)
            {
                Console.WriteLine("Nie je možné provést žádné změny, oba ALLOW_ADD a ALLOW_REMOVE jsou nastaveny na false.");
                // return; // Po validacii KEEP odkomentuj!
            }
            var faker = new Faker();
            MadeRemovals = 0;
            MadeAdditions = 0;

            int targetCount = faker.Random.Int(MIN_RESULT_SET_SIZE, MAX_RESULT_SET_SIZE);

            // Vytvorenie základneho (vysledkovy) setu
            for (ActualIteration = 0; ActualIteration < ITERATIONS; ActualIteration++) // pocet skupin vetiev
            {
                var resultSet = new HashSet<string>(StringComparer.Ordinal);

                while (resultSet.Count < targetCount)
                {
                    string value = faker.Random.Word();
                    if (!resultSet.Contains(value))
                    {
                        resultSet.Add(value);
                    }
                }

                var rightSet = new HashSet<string>(resultSet, StringComparer.Ordinal);
                var leftSet = new HashSet<string>(resultSet, StringComparer.Ordinal);
                var baseSet = new HashSet<string>(resultSet, StringComparer.Ordinal);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

                foreach (string item in resultSet)
                {
                    SetAction leftAct, rightAct;
                    if (Random.Shared.NextDouble() > leftKeepProbability)
                    {
                        leftAct = SetAction.KEEP;
                        rightAct = GetElementAction();
                    }
                    else
                    {
                        leftAct = GetElementAction();
                        rightAct = SetAction.KEEP;
                    }

                    if (leftAct == SetAction.KEEP && rightAct == SetAction.KEEP)
                    {
                        var massage = "L, R, B:";
                        WriteToFile("changeLog", massage);
                        //Console.WriteLine(massage);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        var massage = "R, B:";
                        WriteToFile("changeLog", massage);
                        //Console.WriteLine(massage);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker);
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        var massage = "L, B:";
                        WriteToFile("changeLog", massage);
                        //Console.WriteLine(massage);
                        ExecuteAction(leftSet, baseSet, item, leftAct, faker);
                    }
                    else
                    {
                        Console.Error.WriteLine("Setmaker - nenašla sa vetva s KEEP akciou\nRA: " + rightAct.ToString() + "LA: " + leftAct.ToString());
                        return;
                    }
                }

                // Vytvor adresár pre aktuálnu iteráciu a exportuj seti
                string iterDir = Path.Combine("createdFiles", (ActualIteration).ToString());
                ExportSet(leftSet, "left");
                ExportSet(rightSet, "right");
                ExportSet(baseSet, "base");
                ExportSet(resultSet, "result");
                Console.WriteLine("--------------------------------------------------");

            }

        }

        private static void ExecuteAction(HashSet<string> branchSet, HashSet<string> baseSet, string item, SetAction action, Faker faker)
        {
            if (action == SetAction.KEEP)
            {
                var massage = $"Keeping item: {item}";
                WriteToFile("changeLog", massage);
                //Console.WriteLine(massage);
            }
            else if (action == SetAction.REMOVE)
            {
                var massage = $"Removing item: {item}";
                WriteToFile("changeLog", massage);
                //Console.WriteLine(massage);

                baseSet.Remove(item);
                branchSet.Remove(item);
            }
            else if (action == SetAction.ADD)
            {
                string newItem = faker.Random.Word();
                while (branchSet.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }
                string message = $"Adding item: {newItem}";
                //Console.WriteLine(message);
                WriteToFile("changeLog", message);
                baseSet.Add(newItem);
                branchSet.Add(newItem);
            }
        }

        public static SetAction GetElementAction()
        {
            List<SetAction> allowed = new List<SetAction>() { SetAction.KEEP };
            if (ALLOW_REMOVE && MAX_ALLOWED_REMOVALS > MadeRemovals) {
                allowed.Add(SetAction.REMOVE); 
            }
            if (ALLOW_ADD && MAX_ALLOWED_ADDITIONS > MadeAdditions)
            {
                allowed.Add(SetAction.ADD);
            }

            int index = new Random().Next(allowed.Count);

            return allowed.ElementAt(index);
        }

        private static void ExportSet(HashSet<string> set, string fileName)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));

            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                string outputDir = Path.Combine(projectDir, "createdFiles", ActualIteration.ToString());
                Directory.CreateDirectory(outputDir);

                string xmlPath = Path.Combine(outputDir, $"{fileName}{ActualIteration}.xml");
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(HashSet<string>));
                using (var writer = new StreamWriter(xmlPath))
                {
                    xmlSerializer.Serialize(writer, set);
                }
                Console.WriteLine($"XML uložený do: {xmlPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pri exporte: {ex.Message}");
            }
        }

        private static void WriteToFile(string fileName, string row)
        {
            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                string outputDir = Path.Combine(projectDir, "createdFiles", ActualIteration.ToString());
                Directory.CreateDirectory(outputDir);
                string path = Path.Combine(outputDir, $"{fileName}{ActualIteration}.txt");

                //vymazanie obsahu súboru pri prvej iterácii
                if (!ClearedLogFiles.Contains(path) && File.Exists(path))
                {
                    Console.WriteLine($"TXT uložený do: {path}");
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                    ClearedLogFiles.Add(path);
                }

                using (var sw = new StreamWriter(path, append: true, encoding: Encoding.UTF8))
                {
                    sw.WriteLine(row);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pri zápise do súboru '{fileName}': {ex.Message}");
            }
        }
    }
}