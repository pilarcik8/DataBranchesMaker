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

    public static class PersonMaker
    {
        private static int MadeChanges = 0;
        private static int MadeRemovals = 0;
        private static int MadeAdditions = 0;

        public static int Iterations { get; set; } = 5;
        public static bool AllowChange { get; set; } = true;
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdd { get; set; } = true;
        public static int MaxAllowedChanges { get; set; } = int.MaxValue;
        public static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        public static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        public static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PersonMakerOutput");

 
        public static void SetParameters(int numberIterations, bool changesAllowed, bool removingAllowed, bool addingAllowed, string outputDirectory)
        {
            Iterations = numberIterations;
            AllowChange = changesAllowed;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
        }

        public static void SetMaxAllowed(int maxChanges, int maxRemovals, int maxAdditions)
        {
            MaxAllowedChanges = maxChanges;
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
        }

        public static void Main()
        {
            MadeChanges = 0;
            MadeRemovals = 0;
            MadeAdditions = 0;

            for (int iteration = 0; iteration < Iterations; iteration++)
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
                        FileOutput.WriteTxtSingleRow("changeLogger", "Left, Right and Base:", iteration, OutputDirectory);
                        ExecuteAction(leftPerson, basePeson, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        FileOutput.WriteTxtSingleRow("changeLogger", "Left and Base:", iteration, OutputDirectory);
                        ExecuteAction(leftPerson, basePeson, i, actionL, faker, iteration);
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        FileOutput.WriteTxtSingleRow("changeLogger", "Right and Base:", iteration, OutputDirectory);
                        ExecuteAction(rightPerson, basePeson, i, actionR, faker, iteration);
                    }
                }
                FileOutput.Export(resultPerson, "result", iteration, OutputDirectory);
                FileOutput.Export(rightPerson, "right", iteration, OutputDirectory);
                FileOutput.Export(leftPerson, "left", iteration, OutputDirectory);
                FileOutput.Export(basePeson, "base", iteration, OutputDirectory);
                Console.WriteLine("-----------------------------------------------------");
            }
        }

        private static void ExecuteAction(Person branchPerson, Person basePerson, int i, AtributeAction action, Faker faker, int iteration)
        {
            if (action == AtributeAction.KEEP)
            {
                FileOutput.WriteTxtSingleRow("changeLogger", $"Kept attribute: '{branchPerson.GetAttributeName(i)}'", iteration, OutputDirectory);
            }
            else if (action == AtributeAction.CHANGE)
            {
                string changeResponse = branchPerson.ChangeAttribute(i, faker);

                string[] parts = changeResponse.Split('|');
                var change = parts[0];
                var log = parts[1];

                FileOutput.WriteTxtSingleRow("changeLogger", log, iteration, OutputDirectory);

                basePerson.SetAttribute(i, change);
            }
            else if (action == AtributeAction.REMOVE)
            {
                var oldValue = branchPerson.GetAttribute(i);
                FileOutput.WriteTxtSingleRow("changeLogger", $"Removed attribute: '{branchPerson.GetAttributeName(i)}'", iteration, OutputDirectory);
                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
            }

            else if (action == AtributeAction.ADD)
            {
                var valueAndNameOfNewAttribute = branchPerson.AddAttribute(i, faker);
                string valueOfNewAttribute = valueAndNameOfNewAttribute[0];
                string nameOfNewAttribute = valueAndNameOfNewAttribute[1];

                FileOutput.WriteTxtSingleRow("changeLogger", $"Added new attribute before attribute '{branchPerson.GetAttributeName(i)}': named '{nameOfNewAttribute}' with value '{valueOfNewAttribute}'", iteration, OutputDirectory);
                basePerson.AddAttribute(i, faker, valueOfNewAttribute);
            }
            else
            {
                Console.Error.WriteLine($"Neznámá akce: {action}");
            }
        }

        // Ak su vsetky vycerpane alebo vypnute, vrati KEEP
        public static AtributeAction GetAtributeAction()
        {
            var allowed = new List<AtributeAction> { AtributeAction.KEEP };
            if (AllowChange && MaxAllowedChanges > MadeChanges)
            {
                allowed.Add(AtributeAction.CHANGE);
            }
            if (AllowRemove && MaxAllowedRemovals > MadeRemovals)
            {
                allowed.Add(AtributeAction.REMOVE);
            }
            if (AllowAdd && MaxAllowedAdditions > MadeAdditions)
            {
                allowed.Add(AtributeAction.ADD);
            }

            int index = new Random().Next(allowed.Count);
            return allowed[index];
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