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
    public enum ListAction
    {
        KEEP,
        REMOVE,
        ADD,
        SHIFT
    }


    public static class Program
    {
        const int MAX_RESULT_LIST_SIZE = 34;
        const int MIN_RESULT_LIST_SIZE = 17;
        const int ITERATIONS = 1;

        static int ActualIteration;
        static HashSet<string> ClearedLogFiles = new HashSet<string>();

        public static void Main()
        {
            var faker = new Faker();

            int targetCount = faker.Random.Int(MIN_RESULT_LIST_SIZE, MAX_RESULT_LIST_SIZE);

            for (int i = 0; i < ITERATIONS; i++)
            {
                ActualIteration = i;
                var resultList = new List<string>();

                while (resultList.Count < targetCount)
                {
                    string value = faker.Random.Word();
                    if (!resultList.Contains(value))
                    {
                        resultList.Add(value);
                    }
                }

                var rightList = new List<string>(resultList);
                var leftList = new List<string>(resultList);
                var baseList = new List<string>(resultList);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

                foreach (string item in resultList)
                {
                    ListAction leftAct, rightAct;
                    if (Random.Shared.NextDouble() > leftKeepProbability)
                    {
                        leftAct = ListAction.KEEP;
                        rightAct = GetAction();
                    }
                    else
                    {
                        leftAct = GetAction();
                        rightAct = ListAction.KEEP;
                    }

                    if (leftAct == ListAction.KEEP && rightAct == ListAction.KEEP)
                    {
                        var massage = "L, R, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(rightList, baseList, item, rightAct, faker);
                    }
                    else if (leftAct == ListAction.KEEP)
                    {
                        var massage = "R, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(rightList, baseList, item, rightAct, faker);
                    }
                    else if (rightAct == ListAction.KEEP)
                    {
                        var massage = "L, B:";
                        WriteToFile("changeLog", massage);
                        Console.WriteLine(massage);
                        ExecuteAction(leftList, baseList, item, leftAct, faker);
                    }
                }

                // build iteration directory and export files there
                string iterDir = Path.Combine("createdFiles", (i).ToString());
                ExportList(leftList, "left");
                ExportList(rightList, "right");
                ExportList(baseList, "base");
                ExportList(resultList, "result");
                Console.WriteLine("--------------------------------------------------");

            }

        }

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, ListAction action, Faker faker)
        {
            if (action == ListAction.KEEP)
            {
                var massage = $"Keeping item: {item}";
                WriteToFile("changeLog", massage);
                Console.WriteLine(massage);
            }
            else if (action == ListAction.REMOVE)
            {
                var massage = $"Removing item: {item}";
                WriteToFile("changeLog", massage);
                Console.WriteLine(massage);

                baseList.Remove(item);
                branchList.Remove(item);
            }
            else if (action == ListAction.ADD)
            {
                string newItem = faker.Random.Word();
                while (branchList.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }

                int currentIndex = branchList.IndexOf(item);
                if (currentIndex < 0)
                {
                    throw new InvalidOperationException($"Item '{item}' not found in branch list - cannot insert relative to it.");
                }

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

                string message = $"Adding item: {newItem} at index {currentIndex}";
                Console.WriteLine(message);
                WriteToFile("changeLog", message);
            }
            else if (action == ListAction.SHIFT)
            {
                int count = branchList.Count;

                if (count <= 1)
                {
                    var msg = $"Cannot shift item '{item}' in a list with <= 1 element.";
                    WriteToFile("changeLog", msg);
                    Console.WriteLine(msg);
                    return;
                }

                int currentIndex = branchList.IndexOf(item);
                if (currentIndex < 0)
                {
                    throw new InvalidOperationException($"Item '{item}' not found in branch list - cannot shift.");

                }

                int targetIndex = Random.Shared.Next(count);

                while (targetIndex == currentIndex)
                    targetIndex = Random.Shared.Next(count);

                branchList.RemoveAt(currentIndex);
                int insertIndex = targetIndex > currentIndex ? targetIndex - 1 : targetIndex;
                insertIndex = Math.Clamp(insertIndex, 0, branchList.Count); // safe bounds
                branchList.Insert(insertIndex, item);

                // Mirror change in baseSet if item exists there
                int baseIndex = baseList.IndexOf(item);
                if (baseIndex >= 0 && baseList.Count > 1)
                {
                    int baseCount = baseList.Count;
                    int baseTarget = Math.Min(targetIndex, baseCount - 1);
                    baseList.RemoveAt(baseIndex);
                    int baseInsert = baseTarget > baseIndex ? baseTarget - 1 : baseTarget;
                    baseInsert = Math.Clamp(baseInsert, 0, baseList.Count);
                    baseList.Insert(baseInsert, item);
                }

                var message = $"Shifting item: '{item}' from index {currentIndex} to {insertIndex}";
                WriteToFile("changeLog", message);
                Console.WriteLine(message);
            }

        }

        private static ListAction GetAction()
        {
            int randomValue = new Random().Next(4);
            switch (randomValue)
            {
                case 0:
                    return ListAction.KEEP;
                case 1:
                    return ListAction.REMOVE;
                case 2:
                    return ListAction.ADD;
                default:
                    return ListAction.SHIFT;
            }
        }

        private static void ExportList(List<string> list, string fileName)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                string outputDir = Path.Combine(projectDir, "createdFiles", ActualIteration.ToString());
                Directory.CreateDirectory(outputDir);

                string xmlPath = Path.Combine(outputDir, $"{fileName}{ActualIteration}.xml");
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<string>));
                using (var writer = new StreamWriter(xmlPath))
                {
                    xmlSerializer.Serialize(writer, list);
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