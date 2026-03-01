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
        public static Person? BasePerson { get; set; }
        public static Person? LeftPerson { get; set; }
        public static Person? RightPerson { get; set; }
        public static Person? ResultPerson { get; set; }


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
            if (!addingAllowed && !changesAllowed && !removingAllowed)
            {
                throw new Exception("Žiadná operácie nebola povolená");
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
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                MadeChanges = 0;
                MadeRemovals = 0;
                MadeAdditions = 0;

                ChangeLogText = string.Empty;
                Console.WriteLine($"Iteration {iteration}:");
                // Vytvorenie vyslednej osoby
                var faker = new Faker("en");

                ResultPerson = CreatePersonWithPlaceholders();

                // Tvorba 2 branchov a ich predka
                LeftPerson = ResultPerson.Clone();
                RightPerson = ResultPerson.Clone();
                BasePerson = ResultPerson.Clone();

                int baseAtributeCount = typeof(Person).GetProperties().Length; //null nepocita

                // Pre vasciu diferenciaciu r a l branchov pocas generovania
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";

                for (int i = 0; i < baseAtributeCount; i++)
                {
                    // Generovanie akcii pre pravy a lavy branch
                    AtributeAction actionR, actionL;
                    bool leftIsKeep = Random.Shared.NextDouble() < leftKeepProbability;

                    int remainingPositions = baseAtributeCount - i; // include current

                    if (leftIsKeep)
                    {
                        actionL = AtributeAction.KEEP;
                        actionR = GetAtributeAction(remainingPositions);
                    }
                    else
                    {
                        actionL = GetAtributeAction(remainingPositions);
                        actionR = AtributeAction.KEEP;
                    }

                    if (actionR == AtributeAction.KEEP && actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "Left, Right and Base\n";
                        ExecuteAction(LeftPerson, BasePerson, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        ChangeLogText += "Left and Base:\n";
                        ExecuteAction(LeftPerson, BasePerson, i, actionL, faker, iteration);
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "Right and Base:\n";
                        ExecuteAction(RightPerson, BasePerson, i, actionR, faker, iteration);
                    }
                }

                XMLOutput.Export(RightPerson, "right", iteration, OutputDirectory);
                XMLOutput.Export(LeftPerson, "left", iteration, OutputDirectory);
                XMLOutput.Export(BasePerson, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultPerson, "expectedResult", iteration, OutputDirectory);

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
                string changeValue = branchPerson.ChangeAttribute(i, faker);

                ChangeLogText += $"Changed attribute: '{branchPerson.GetAttributeName(i)}' to '{changeValue}'\n";

                basePerson.SetAttribute(i, changeValue);
                MadeChanges++;
            }
            else if (action == AtributeAction.REMOVE)
            {
                var oldValue = branchPerson.GetAttribute(i);
                var attrName = branchPerson.GetAttributeName(i);
                ChangeLogText += $"Removed attribute: '{attrName}' with value '{oldValue}'\n";

                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
                MadeRemovals++;
            }
            else if (action == AtributeAction.ADD)
            {
                var attributeBeforeAdd = branchPerson.GetAttributeName(i);
                var valueAndNameOfNewAttribute = branchPerson.AddAttribute(i, faker);
                string valueOfNewAttribute = valueAndNameOfNewAttribute[0];
                string nameOfNewAttribute = valueAndNameOfNewAttribute[1];

                basePerson.AddAttribute(i, faker, valueOfNewAttribute);

                ChangeLogText += $"Added new attribute named '{nameOfNewAttribute}' with value '{valueOfNewAttribute}' was added before attribute '{attributeBeforeAdd}'\n";

                MadeAdditions++;
            }
        }

        // Ak su vsetky vycerpane alebo vypnute, vrati KEEP
        public static AtributeAction GetAtributeAction(int remainingPositions)
        {
            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningChange = AllowChange ? MaxAllowedChanges - MadeChanges : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningChange + remaningRemove;

            // ak zostava jedna modifikacia, rovnomerna pravdepodobnost pre kazdy atribut
            if (remainingModifications == 1)
            {
                remainingPositions = Math.Max(1, remainingPositions);
                if (Random.Shared.Next(remainingPositions) != 0)
                {
                    return AtributeAction.KEEP;
                }
            }

            var availableMods = new List<AtributeAction>();
            if (remaningChange > 0) availableMods.Add(AtributeAction.CHANGE);
            if (remaningRemove > 0) availableMods.Add(AtributeAction.REMOVE);
            if (remaningAdd > 0) availableMods.Add(AtributeAction.ADD);


            var madeModifications = MadeAdditions + MadeChanges + MadeChanges;
            
            // týmto sa ujistíme, že aspoň jedna modifikácia nastane pred koncom iterácie
            if (remainingPositions <= 1 && madeModifications == 0 && availableMods.Count > 0)
            {
                return availableMods[Random.Shared.Next(availableMods.Count)];
            }

            var allowed = new List<AtributeAction> { AtributeAction.KEEP };
            allowed.AddRange(availableMods);

            int index = Random.Shared.Next(allowed.Count);
            return allowed[index];
        }

        private static Person CreatePersonWithPlaceholders()
        {
            var f = new Faker("en");
            var pf = new Faker<Person>("en")
                // primary properties
                .RuleFor(p => p.Title, _ => f.Name.Prefix())
                .RuleFor(p => p.FirstName, _ => f.Name.FirstName())
                .RuleFor(p => p.LastName, _ => f.Name.LastName())
                .RuleFor(p => p.Email, _ => f.Internet.Email())
                .RuleFor(p => p.Phone, _ => f.Phone.PhoneNumber())
                .RuleFor(p => p.Gender, _ => f.PickRandom(new[] { "Male", "Female", "Other" }))
                .RuleFor(p => p.Company, _ => f.Company.CompanyName())
                .RuleFor(p => p.JobTitle, _ => f.Name.JobTitle())
                .RuleFor(p => p.CreditCardNumber, _ => f.Finance.CreditCardNumber())
                .RuleFor(p => p.Street, _ => f.Address.StreetName())
                .RuleFor(p => p.StreetNumber, _ => f.Address.SecondaryAddress())
                .RuleFor(p => p.City, _ => f.Address.City())
                .RuleFor(p => p.County, _ => f.Address.County())
                .RuleFor(p => p.State, _ => f.Address.State())
                .RuleFor(p => p.ZipCode, _ => f.Address.ZipCode())
                .RuleFor(p => p.Country, _ => f.Address.Country());

            return pf.Generate();
        }
    }
}