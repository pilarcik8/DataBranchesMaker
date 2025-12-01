using Bogus;
using Bogus.Bson;
using System;
using System.IO;
using System.Xml.Serialization;
using static System.Collections.Specialized.BitVector32;


namespace TestKniznice
{
    public enum AtributeAction
    {
        KEEP,
        CHANGE,
        REMOVE,
        ADD
    }

    public static class Program
    {
        public static void Main()
        {
            // Vytvorenie vyslednej osoby
            var faker = new Faker("en");
            Person resultPerson = CreateFakePerson(faker);

            // Tvorba 2 branchov a ich predka
            Person leftPerson = resultPerson.Clone();
            Person rightPerson = resultPerson.Clone();
            Person basePeson = resultPerson.Clone();

            int atCount = typeof(Person).GetProperties().Length;

            // Pre vasciu diferenciaciu r a l branchov pocas generovania
            double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
            double rightKeepProbability = 1.0 - leftKeepProbability;
            Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

            for (int i = 0; i < atCount; i++)
            {
                // Generovanie akcii pre pravy a lavy branch
                AtributeAction actionR, actionL;
                bool leftIsKeep = Random.Shared.NextDouble() < leftKeepProbability;

                if (leftIsKeep)
                {
                    actionL = AtributeAction.KEEP;
                    actionR = GetAtributeAction();
                }
                else
                {
                    actionL = GetAtributeAction();
                    actionR = AtributeAction.KEEP;

                }

                if (actionR == AtributeAction.KEEP && actionL == AtributeAction.KEEP)
                {
                    Console.WriteLine($"\n{i + 1}. R + L + B action:");
                    // nezalezi ktory branch sa vyberie, lebo oba maju KEEP
                    ExecuteSameAction(rightPerson, basePeson, i, actionR, faker);
                }
                else if (actionR == AtributeAction.KEEP)
                {
                    Console.WriteLine($"\n{i + 1}. L + B action:");
                    ExecuteSameAction(leftPerson, basePeson, i, actionL, faker);
                }
                else if (actionL == AtributeAction.KEEP)
                {
                    Console.WriteLine($"\n {i + 1}.R + B action:");
                    ExecuteSameAction(rightPerson, basePeson, i, actionR, faker);
                }
            }
            Console.WriteLine();
            ExportPerson(resultPerson, "res");
            ExportPerson(rightPerson, "right");
            ExportPerson(leftPerson, "left");
            ExportPerson(basePeson, "base");
        }

        private static void ExecuteSameAction(Person branchPerson, Person basePerson, int i, AtributeAction action, Faker faker)
        {
            if (action == AtributeAction.KEEP)
            {
                Console.WriteLine($"    Kept attribute: '{branchPerson.GetAttributeName(i)}'");
                return;

            }
            else if (action == AtributeAction.CHANGE)
            {
                string change = branchPerson.ChangeAttribute(i, faker);
                basePerson.SetAttribute(i, change);
            }
            else if (action == AtributeAction.REMOVE)
            {
                Console.WriteLine($"    Removed attribute: '{branchPerson.GetAttributeName(i)}'");
                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
            }

            else if (action == AtributeAction.ADD)
            {
                // potrebujem pridat novy atribut do triedy pred tento atribut (aspon nastavit aby sa ulozil do xml ked ho expornem)
            }
        }

        // Mozu byt bud identicke alebo aspon jeden z nich musi byt KEEP
        private static AtributeAction GetBaseWinningAcion(AtributeAction actionR, AtributeAction actionL)
        {
            if (actionL == AtributeAction.KEEP)
                return actionR;
            return actionL;
        }

        public static AtributeAction GetAtributeAction()
        {
            return (AtributeAction)new Random().Next(4);
        }


        private static Person CreateFakePerson(Faker faker)
        {
            var personFaker = new Faker<Person>("en")
                .RuleFor(p => p.Title, f => f.Name.Prefix())
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.Email, f => f.Internet.Email())
                .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(p => p.Gender, f => f.PickRandom(new[] { "Male", "Female", "Other" }))
                .RuleFor(p => p.Age, f => f.Random.Int(18, 80))
                .RuleFor(p => p.Company, f => f.Company.CompanyName())
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.CreditCardNumber, f => f.Finance.CreditCardNumber())
                .RuleFor(p => p.Street, f => f.Address.StreetName())
                .RuleFor(p => p.StreetNumber, f => f.Address.SecondaryAddress())
                .RuleFor(p => p.City, f => f.Address.City())
                .RuleFor(p => p.County, f => f.Address.County())
                .RuleFor(p => p.State, f => f.Address.State())
                .RuleFor(p => p.ZipCode, f => f.Address.ZipCode())
                .RuleFor(p => p.Country, f => f.Address.Country());

            return personFaker.Generate();
        }

        private static void ExportPerson(Person person, string fileName)
        {
            if (person == null)
            {
                Console.WriteLine("Person je null – export sa nevykoná.");
                return;
            }

            try
            {
                // Relatívna cesta ku koreňu projektu
                string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                string outputDir = Path.Combine(projectDir, "createdFiles");

                // Vytvorenie priečinku, ak neexistuje
                Directory.CreateDirectory(outputDir);

                string xmlPath = Path.Combine(outputDir, $"{fileName}.xml");
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Person));
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