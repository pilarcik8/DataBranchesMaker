using Bogus;
using Shared;
using System.Text;
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

        public static string ChangeLogText = "";


        public static void SetParameters(int numberIterations, bool changesAllowed, bool removingAllowed, bool addingAllowed, string outputDirectory)
        {
            if (numberIterations <= 0)
            {
                throw new Exception("Zlá nízka hodnota iterácí");
            }
            if (!changesAllowed && !removingAllowed && !addingAllowed)
            {
                throw new Exception("Žiadna operácia nie je povolená");
            }
            Iterations = numberIterations;
            AllowChange = changesAllowed;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
        }

        public static void SetMaxAllowed(int maxChanges, int maxRemovals, int maxAdditions)
        {
            if (maxChanges <= 0 || maxRemovals <= 0 || maxAdditions <= 0) // v gui to ide menit iba ak je povolena operacia cize 0 nikdy nenastane kedze inicializujem s maxvalue
            {
                throw new Exception("Hodnata maximálneho výskytu premeny nemôže byť záporná alebo nula");
            }

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
                ChangeLogText = string.Empty;
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
                ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";

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
                        ChangeLogText += "Left, Right and Base\n";
                        ExecuteAction(leftPerson, basePeson, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        ChangeLogText += "Left and Base:\n";
                        ExecuteAction(leftPerson, basePeson, i, actionL, faker, iteration);
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "Right and Base:\n";
                        ExecuteAction(rightPerson, basePeson, i, actionR, faker, iteration);
                    }
                }
                XMLOutput.Export(rightPerson, "right", iteration, OutputDirectory);
                XMLOutput.Export(leftPerson, "left", iteration, OutputDirectory);
                XMLOutput.Export(basePeson, "base", iteration, OutputDirectory);
                XMLOutput.Export(resultPerson, "expectedResult", iteration, OutputDirectory);

                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static void ExecuteAction(Person branchPerson, Person basePerson, int i, AtributeAction action, Faker faker, int iteration)
        {
            if (action == AtributeAction.KEEP)
            {
                ChangeLogText += $"Kept attribute: '{branchPerson.GetAttributeName(i)}'\n";
            }
            else if (action == AtributeAction.CHANGE)
            {
                string changeResponse = branchPerson.ChangeAttribute(i, faker);

                string[] parts = changeResponse.Split('|');
                var change = parts[0];
                var log = parts[1];

                ChangeLogText += $"Changed attribute: '{branchPerson.GetAttributeName(i)}' to '{change}'\n";

                basePerson.SetAttribute(i, change);
                MadeChanges++;
            }
            else if (action == AtributeAction.REMOVE)
            {
                var oldValue = branchPerson.GetAttribute(i);
                ChangeLogText += $"Removed attribute: '{branchPerson.GetAttributeName(i)}' with value '{oldValue}'\n";
                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
                MadeRemovals++;
            }

            else if (action == AtributeAction.ADD)
            {
                var valueAndNameOfNewAttribute = branchPerson.AddAttribute(i, faker);
                string valueOfNewAttribute = valueAndNameOfNewAttribute[0];
                string nameOfNewAttribute = valueAndNameOfNewAttribute[1];

                ChangeLogText += $"Added new attribute before attribute '{branchPerson.GetAttributeName(i)}': named '{nameOfNewAttribute}' with value '{valueOfNewAttribute}'\n";
                basePerson.AddAttribute(i, faker, valueOfNewAttribute);
                MadeAdditions++;
            }
            else
            {
                throw new Exception("Neznáma akcia nájdená");
            }

        }

        // Ak su vsetky vycerpane alebo vypnute, vrati KEEP
        public static AtributeAction GetAtributeAction()
        {
            var allowed = new List<AtributeAction> { AtributeAction.KEEP };
            
            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningChange = AllowChange ? MaxAllowedChanges - MadeChanges : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningChange + remaningRemove;

            // Keď je povolená len jedna posledna modifikacia, chceme aby KEEP padal častejšie
            // Špecialne hlavne ak od zaciatku je iba jedna modifikacia povolena, modifikacia by padala prilis skoro a potom by sa uz len KEEP opakoval
            if (remainingModifications == 1)
            {
                int randomValue = Random.Shared.Next(5);
                if (randomValue != 0) return AtributeAction.KEEP;
            }

            // Normany vyber akcii, v zavislosti od toho co je este povolene a kolko z toho este moze padat
            if (remaningChange > 0)
            {
                allowed.Add(AtributeAction.CHANGE);
            }
            if (remaningRemove > 0)
            {
                allowed.Add(AtributeAction.REMOVE);
            }
            if (remaningAdd > 0)
            {
                allowed.Add(AtributeAction.ADD);
            }

            int index = Random.Shared.Next(allowed.Count);
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