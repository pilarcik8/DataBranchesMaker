using System.Text;
using System.Xml.Serialization;

namespace Shared
{
    public static class FileOutput
    {
        private static readonly HashSet<string> ClearedLogFiles = new();

        // Zápis do súboru, po riadku
        public static void WriteTxtSingleRow(string fileName, string row, int iteration, string outputDir)
        {
            try
            {
                outputDir = Path.Combine(outputDir, iteration.ToString());
                Directory.CreateDirectory(outputDir);
                string path = Path.Combine(outputDir, $"{fileName}{iteration}.txt");

                //vymazanie obsahu súboru pri prvej iterácii
                if (!ClearedLogFiles.Contains(path) && File.Exists(path))
                {
                    Console.WriteLine($"TXT uložený do: {path}");
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                    ClearedLogFiles.Add(path);
                }

                using var sw = new StreamWriter(path, append: true, encoding: Encoding.UTF8);
                sw.WriteLine(row);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba pri zápise do súboru '{fileName}': {ex.Message}");
            }
        }

        public static void Export(Object ob, string fileName, int iteration, string outputDir)
        {
            if (ob is null)
            {
                Console.Error.WriteLine("Objekt je null – export sa nevykoná.");
                return;
            }

            XmlSerializer xmlSerializer;
            if (ob is Person person)
            {
                xmlSerializer = new(typeof(Person));
            }
            else if (ob is HashSet<string>)
            {
                xmlSerializer = new(typeof(HashSet<string>));
            }
            else if (ob is List<string>)
            {
                xmlSerializer = new(typeof(List<string>));
            }
            else
            {
                throw new ArgumentException($"Neočakávaný druh objektu: {ob.GetType()}");
            }
            
            try
            {
                outputDir = Path.Combine(outputDir, iteration.ToString());
                Directory.CreateDirectory(outputDir);

                string xmlPath = Path.Combine(outputDir, $"{fileName}{iteration}.xml");
                using (var writer = new StreamWriter(xmlPath))
                {
                    xmlSerializer.Serialize(writer, ob);
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