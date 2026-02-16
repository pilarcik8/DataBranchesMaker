using Bogus;
using Shared;

using Person = Shared.Person;

namespace PersonMaker
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
        static int MadeChanges;
        static int MadeRemovals;
        static int MadeAdditions;

        // Nastavenia generovania
        const int ITERATIONS = 5;

        const bool ALLOW_CHANGE = true;
        const bool ALLOW_REMOVE = true;
        const bool ALLOW_ADD = true;

        const int MAX_ALLOWED_CHANGES = int.MaxValue;
        const int MAX_ALLOWED_REMOVALS = int.MaxValue;
        const int MAX_ALLOWED_ADDITIONS = int.MaxValue;

        public static void Main()
        {
            if (!ALLOW_ADD && !ALLOW_REMOVE && !ALLOW_CHANGE)
            {
                Console.WriteLine("Nie je možné provést žádné změny, oba ALLOW_ADD a ALLOW_REMOVE jsou nastaveny na false.");
                // return; // Po validacii KEEP odkomentuj!
            }

            // Generovanie testovacich dat
            MadeChanges = 0;
            MadeRemovals = 0;
            MadeAdditions = 0;

            for (int iteration = 0; iteration < ITERATIONS; iteration++)
            {
                Console.WriteLine($"Iteration {iteration}:");
                // Vytvorenie vyslednej osoby
                var faker = new Faker("en");
                Person resultPerson = CreateFakePerson();
                int baseAtributeCount = typeof(Person).GetProperties().Length; //null nepocita

                // Tvorba 2 branchov a ich predka
                Person leftPerson = resultPerson.Clone();
                Person rightPerson = resultPerson.Clone();
                Person basePeson = resultPerson.Clone();

                // Pre vasciu diferenciaciu r a l branchov pocas generovania
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

                for (int i = 0; i < baseAtributeCount; i++)
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
                        FileOutput.WriteTxtSingleRow("changeLogger", "Left, Right and Base:", iteration);
                        ExecuteSameAction(leftPerson, basePeson, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        FileOutput.WriteTxtSingleRow("changeLogger", "Left and Base:", iteration);
                        ExecuteSameAction(leftPerson, basePeson, i, actionL, faker, iteration);
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        FileOutput.WriteTxtSingleRow("changeLogger", "Right and Base:", iteration);
                        ExecuteSameAction(rightPerson, basePeson, i, actionR, faker, iteration);
                    }
                }
                FileOutput.Export(resultPerson, "result", iteration);
                FileOutput.Export(rightPerson, "right", iteration);
                FileOutput.Export(leftPerson, "left", iteration);
                FileOutput.Export(basePeson, "base", iteration);
                Console.WriteLine("-----------------------------------------------------");
            }
        }

        private static void ExecuteSameAction(Person branchPerson, Person basePerson, int i, AtributeAction action, Faker faker, int iteration)
        {

            if (action == AtributeAction.KEEP)
            {
                FileOutput.WriteTxtSingleRow("changeLogger", $"Kept attribute: '{branchPerson.GetAttributeName(i)}'", iteration);

            }
            else if (action == AtributeAction.CHANGE)
            {
                string changeResponse = branchPerson.ChangeAttribute(i, faker);

                string[] parts = changeResponse.Split('|');
                var change = parts[0];
                var log = parts[1];
                string step = log.Replace("Changed", "Change");
                step = step.Replace("'{old}'", "'{newValue}'");

                FileOutput.WriteTxtSingleRow("changeLogger", log, iteration);

                basePerson.SetAttribute(i, change);
            }
            else if (action == AtributeAction.REMOVE)
            {
                var oldValue = branchPerson.GetAttribute(i);
                FileOutput.WriteTxtSingleRow("changeLogger", $"Removed attribute: '{branchPerson.GetAttributeName(i)}'", iteration);
                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
            }

            else if (action == AtributeAction.ADD)
            {
                var valueAndNameOfNewAttribute = branchPerson.AddAttribute(i, faker);
                string valueOfNewAttribute = valueAndNameOfNewAttribute[0];
                string nameOfNewAttribute = valueAndNameOfNewAttribute[1];

                FileOutput.WriteTxtSingleRow("changeLogger", $"Added new attribute before attribute '{branchPerson.GetAttributeName(i)}': named '{nameOfNewAttribute}' with value '{valueOfNewAttribute}'", iteration);
                basePerson.AddAttribute(i, faker, valueOfNewAttribute);
            }
            else             {
                Console.Error.WriteLine($"Neznámá akce: {action}");
            }
        }

        // Ak su vsetky vycerpane alebo vypnute, vrati KEEP
        // Rekurziva
        public static AtributeAction GetAtributeAction()
        {
            int randomValue = new Random().Next(4);

            switch (randomValue)
            {
                case 0:
                    return AtributeAction.KEEP;

                case 1:
                    if (ALLOW_CHANGE && MAX_ALLOWED_CHANGES > MadeChanges)
                    {
                        MadeChanges++;
                        return AtributeAction.CHANGE;
                    }
                    return GetAtributeAction();

                case 2:
                    if (ALLOW_REMOVE && MAX_ALLOWED_REMOVALS > MadeRemovals)
                    {
                        MadeRemovals++;
                        return AtributeAction.REMOVE;
                    }
                    return GetAtributeAction();

                case 3:
                    if (ALLOW_ADD && MAX_ALLOWED_ADDITIONS > MadeAdditions)
                    {
                        MadeAdditions++;
                        return AtributeAction.ADD;
                    }
                    return GetAtributeAction();
            }
            return GetAtributeAction();
        }

        public static AtributeAction GetElementAction()
        {
            List<AtributeAction> allowed = [AtributeAction.KEEP];
            if (ALLOW_REMOVE && MAX_ALLOWED_REMOVALS > MadeRemovals)
            {
                allowed.Add(AtributeAction.REMOVE);
            }
            if (ALLOW_ADD && MAX_ALLOWED_ADDITIONS > MadeAdditions)
            {
                allowed.Add(AtributeAction.ADD);
            }
            if (ALLOW_CHANGE && MAX_ALLOWED_CHANGES > MadeChanges) { 
                allowed.Add(AtributeAction.CHANGE); 
            }

            int index = new Random().Next(allowed.Count);

            return allowed.ElementAt(index);
        }

        private static Person CreateFakePerson()
        {
            var personFaker = new Faker<Person>("en")
                .RuleFor(p => p.Title, f => f.Name.Prefix())
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.Email, f => f.Internet.Email())
                .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(p => p.Gender, f => f.PickRandom(new[] { "Male", "Female", "Other" }))
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
    }
}