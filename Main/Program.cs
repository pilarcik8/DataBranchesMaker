using Bogus;
using Bogus.Bson;
using System;
using System.IO;
using System.Xml.Serialization;


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
            Person result = CreateFakePerson(faker);
            ExportPerson(result, "res");

            // Tvorba 3 branchov
            Person leftPerson = result;
            Person rightPerson = result;
            Person basePeson = result;

            int atCount = typeof(Person).GetProperties().Length;
            Stack<AtributeAction> RightAtributeActions = new Stack<AtributeAction>(atCount);
            Stack<AtributeAction> LeftAtributeActions = new Stack<AtributeAction>(atCount);
            Stack<AtributeAction> BaseAtributeActions = new Stack<AtributeAction>(atCount);


            for (int i = 0; i < atCount; i++)
            {
                var propType = result.GetTypeOfAtribute(i);
                object value;

                AtributeAction actionR = GetAtributeAction();
                AtributeAction actionL = GetAtributeActionWitourConfilct(actionR);
                AtributeAction actionB = GetBaseWinningAcion(actionR, actionL);

                RightAtributeActions.Push(actionR);
                LeftAtributeActions.Push(actionL);
                BaseAtributeActions.Push(actionB);

                ExecuteAction(rightPerson, i, actionR, faker);
                ExecuteAction(leftPerson, i, actionL, faker);
                ExecuteAction(basePeson, i, actionL, faker);

                if (propType == typeof(int))
                {
                    value = result.GetAtribute<int>(i);
                }
                else if (propType == typeof(string))
                {
                    value = result.GetAtribute<string>(i);
                }
                else
                {
                    string erMessage = $"Neznami typ nastal na indexe: {i} (typ: {propType.FullName})";
                    throw new InvalidOperationException(erMessage);
                }

                Console.WriteLine($"[{i}] {propType.Name} = {value}");
            }
        }

        private static void ExecuteAction(Person person, int i, AtributeAction action, Faker faker)
        {
            if (action == AtributeAction.KEEP)
                return;

            if (action == AtributeAction.CHANGE)
            {
                person.ChangeAttribute(i, faker);
            }

            if (action == AtributeAction.REMOVE)
            {
                // potrebujem odstranit dany atribut z triedy (aspon nastavit aby sa neulozil do xml ked ho expornem)
            }

            if (action == AtributeAction.ADD)
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

        // Ak je RIGHT KEEP, tak L moze byt hocico, inak L je KEEP alebo identicke s R
        private static AtributeAction GetAtributeActionWitourConfilct(AtributeAction actionR)
        {
            if (actionR == AtributeAction.KEEP)
                return GetAtributeAction();

            return Random.Shared.Next(2) == 0 ? AtributeAction.KEEP : actionR;
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
                string outputDir = Path.Combine(projectDir, "vytvoreneSubory", "Base");

                // Vytvorenie priečinku, ak neexistuje
                Directory.CreateDirectory(outputDir);

                Console.WriteLine($"Výstupné súbory budú uložené do: {outputDir}");

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