using System.Text;
using System.Xml.Serialization;

namespace Shared
{
    public static class XMLOutput
    {
        public static void Export(Object ob, string fileName, int? iteration, string outputDir)
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
                if (iteration.HasValue)
                {
                    outputDir = Path.Combine(outputDir, iteration.ToString());
                }
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