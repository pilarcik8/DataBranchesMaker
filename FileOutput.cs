csharp Main\IO\FileOutput.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace PersonMaker
{
    public static class FileOutput
    {
        // Tracks which files were cleared already during this run (prevents clearing multiple times)
        private static readonly HashSet<string> ClearedLogFiles = new();

        public static void WriteLine(string baseName, string row, int iteration)
        {
            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\", String.Empty));
                string outputDir = Path.Combine(projectDir, "createdFiles", iteration.ToString());
                Directory.CreateDirectory(outputDir);
                string path = Path.Combine(outputDir, $"{baseName}{iteration}.txt");

                // Clear existing file contents once per run (keeps first-write behavior from original)
                if (!ClearedLogFiles.Contains(path) && File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                    ClearedLogFiles.Add(path);
                }

                using var sw = new StreamWriter(path, append: true, encoding: Encoding.UTF8);
                sw.WriteLine(row);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pri zápise do súboru '{baseName}': {ex.Message}");
            }
        }

        public static void ExportPerson(Person person, string fileName, int iteration)
        {
            if (person == null)
            {
                Console.WriteLine("Person je null – export sa nevykoná.");
                return;
            }

            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\", String.Empty));
                string outputDir = Path.Combine(projectDir, "createdFiles", iteration.ToString());
                Directory.CreateDirectory(outputDir);

                string xmlPath = Path.Combine(outputDir, $"{fileName}{iteration}.xml");
                XmlSerializer xmlSerializer = new(typeof(Person));
                using (var writer = new StreamWriter(xmlPath))
                {
                    xmlSerializer.Serialize(writer, person);
                }
                Console.WriteLine($"XML uložený do: {xmlPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pri exporte: {ex.Message}");
            }
        }
    }
}