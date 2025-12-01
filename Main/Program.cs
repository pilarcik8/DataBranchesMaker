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
            Stack<AtributeAction> RightAtributeActions = new Stack<AtributeAction>(atCount);
            Stack<AtributeAction> LeftAtributeActions = new Stack<AtributeAction>(atCount);
            Stack<AtributeAction> BaseAtributeActions = new Stack<AtributeAction>(atCount);


            for (int i = 0; i < atCount; i++)
            {
                // Generovanie akcii pre pravy a lavy branch
                AtributeAction actionR, actionL;
                if (i % 2 == 0)
                {
                    actionR = GetAtributeAction();
                    actionL = GetAtributeActionWitourConfilct(actionR);
                }
                else
                {

                    actionL = GetAtributeAction();
                    actionR = GetAtributeActionWitourConfilct(actionL);
                }

                //ulozenie akcii do stacku
                AtributeAction actionB = GetBaseWinningAcion(actionR, actionL);

                RightAtributeActions.Push(actionR);
                LeftAtributeActions.Push(actionL);
                BaseAtributeActions.Push(actionB);

                //prebehnutie zmien v triedach
                string? rightChange = null;
                string? leftChange = null;
                string? sameChange = null;

                //zmeni pre L a R
                if (actionR != actionL)
                {
                    //jedna z akcii je KEEP
                    Console.WriteLine($"Right action");
                    rightChange = ExecuteAction(rightPerson, i, actionR, faker);
                    Console.WriteLine($"Left action");
                    leftChange = ExecuteAction(leftPerson, i, actionL, faker);
                }
                else
                {
                    Console.WriteLine($"Both action");
                    sameChange = ExecuteSameAction(rightPerson, leftPerson, i, actionR, faker);
                }

                // zmena pre B
                Console.WriteLine($"Base action");
                if (actionB == AtributeAction.KEEP)
                {
                    // nothing to do
                }
                else if (actionB == AtributeAction.CHANGE)
                {
                    // Determine which branch's change to apply to base
                    string? valueToApply = null;

                    if (actionR == actionB && rightChange != null)
                        valueToApply = rightChange;
                    else if (actionL == actionB && leftChange != null)
                        valueToApply = leftChange;
                    else if (actionR == actionL && sameChange != null)
                        valueToApply = sameChange;

                    if (valueToApply != null)
                    {
                        basePeson.SetAttribute(i, valueToApply);
                    }
                    else
                    {
                        // fallback: if no change value captured, perform a change on base
                        basePeson.ChangeAttribute(i, faker);
                    }
                }
                else if (actionB == AtributeAction.REMOVE)
                {
                    // TODO: implement remove behavior
                }
                else if (actionB == AtributeAction.ADD)
                {
                    // TODO: implement add behavior
                }

                //TODO: base ma mat akciu, ktora je identicka s jednou z branchov
            }
            ExportPerson(resultPerson, "res");
            ExportPerson(rightPerson, "right");
            ExportPerson(leftPerson, "left");
            ExportPerson(basePeson, "base");
        }

        private static string? ExecuteSameAction(Person rightPerson, Person leftPerson, int i, AtributeAction action, Faker faker)
        {
            if (action == AtributeAction.KEEP)
                return null;

            else if (action == AtributeAction.CHANGE)
            {
                // change right, copy to left and return the new value so base can reuse it
                string change = rightPerson.ChangeAttribute(i, faker);
                leftPerson.SetAttribute(i, change);
                return change;
            }

            else if (action == AtributeAction.REMOVE)
            {
                // potrebujem odstranit dany atribut z triedy (aspon nastavit aby sa neulozil do xml ked ho expornem)
                return null;
            }

            else if (action == AtributeAction.ADD)
            {
                // potrebujem pridat novy atribut do triedy pred tento atribut (aspon nastavit aby sa ulozil do xml ked ho expornem)
                return null;
            }

            return null;
        }

        private static string? ExecuteAction(Person person, int i, AtributeAction action, Faker faker)
        {
            if (action == AtributeAction.KEEP)
                return null;

            else if (action == AtributeAction.CHANGE)
            {
                // return new value so caller can propagate it to base when needed
                return person.ChangeAttribute(i, faker);
            }

            else if (action == AtributeAction.REMOVE)
            {
                // potrebujem odstranit dany atribut z triedy (aspon nastavit aby sa neulozil do xml ked ho expornem)
                return null;
            }

            else if (action == AtributeAction.ADD)
            {
                // potrebujem pridat novy atribut do triedy pred tento atribut (aspon nastavit aby sa ulozil do xml ked ho expornem)
                return null;
            }

            return null;
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