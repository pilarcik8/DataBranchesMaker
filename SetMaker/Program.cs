using Bogus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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
        const int MAX_RESULT_SET_SIZE = 34;
        const int MIN_RESULT_SET_SIZE = 17;
        const int ITERATIONS = 1;

        static int ActualIteration;
        static HashSet<string> ClearedLogFiles = new HashSet<string>();

        public static void Main()
        {
            var faker = new Faker();

            int targetCount = faker.Random.Int(MIN_RESULT_SET_SIZE, MAX_RESULT_SET_SIZE);

            for (int i = 0; i < ITERATIONS; i++)
            {
                ActualIteration = i;
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
                        rightAct = GetAction();
                    }
                    else
                    {
                        leftAct = GetAction();
                        rightAct = SetAction.KEEP;
                    }

                    if (leftAct == SetAction.KEEP && rightAct == SetAction.KEEP)
                    {
                        var massage = "L, R, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        var massage = "R, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker);
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        var massage = "L, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(leftSet, baseSet, item, leftAct, faker);
                    }
                }

                // build iteration directory and export files there
                string iterDir = Path.Combine("createdFiles", (i).ToString());
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
                Console.WriteLine(massage);
            }
            else if (action == SetAction.REMOVE)
            {
                var massage = $"Removing item: {item}";
                WriteToFile("changeLog", massage);
                Console.WriteLine(massage);

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
                Console.WriteLine(message);
                WriteToFile("changeLog", message);
                baseSet.Add(newItem);
                branchSet.Add(newItem);
            }
        }

        private static SetAction GetAction()
        {
            int randomValue = new Random().Next(3);
            switch (randomValue)
            {
                case 0:
                    return SetAction.KEEP;
                case 1:
                    return SetAction.REMOVE;
                default:
                    return SetAction.ADD;
            }
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