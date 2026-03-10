using Bogus;
using Shared;
using System.Text;
using System.Xml;
using Person = Shared.Person;

namespace PersonMaker
{
    internal enum AtributeAction
    {
        KEEP,
        CHANGE,
        REMOVE,
        ADD
    }

    internal struct Step
    {
        public string name { get; }
        public Person listOfState { get; }
        public string pathToExport { get; }

        public Step(string name, Person personOfState, string pathToExport)
        {
            this.name = name;
            this.listOfState = personOfState.Clone();
            this.pathToExport = pathToExport;
        }
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
        public static bool WriteSteps { get; set; } = false;

        public static void SetParameters(int numberIterations, bool changesAllowed, bool removingAllowed, bool addingAllowed, string outputDirectory, bool writeSteps)
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
            WriteSteps = writeSteps;
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
            var faker = new Faker("en");

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Init();

                int baseAtributeCount = typeof(Person).GetProperties().Length; //null nepocita

                // Pre vasciu diferenciaciu r a l branchov pocas generovania
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                int leftModificationCount = 0;
                int rightModificationCount = 0;

                Stack<Step> steps = new Stack<Step>();
                for (int i = 0; i < baseAtributeCount; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    // Generovanie akcii pre pravy a lavy branch
                    AtributeAction actionR, actionL;

                    int remainingPositions = baseAtributeCount - i;

                    if (SharedMethods.NextModificationIsOnLeft(TestingOneActionTwice(), leftModificationCount, rightModificationCount, leftKeepProbability))
                    {
                        actionL = GetAtributeAction(remainingPositions);
                        actionR = AtributeAction.KEEP;
                    }
                    else
                    {
                        actionL = AtributeAction.KEEP;
                        actionR = GetAtributeAction(remainingPositions);
                    }

                    if (actionR == AtributeAction.KEEP && actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "L, R, B:\n";
                        ExecuteAction(LeftPerson!, BasePerson!, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        leftModificationCount++;
                        ChangeLogText += "L, B:\n";
                        ExecuteAction(LeftPerson!, BasePerson!, i, actionL, faker, iteration);
                        if (WriteSteps)
                        {
                            steps.Push(new Step(leftStepName, LeftPerson!, pathToStepsL));
                            steps.Push(new Step(baseStepName, BasePerson!, pathToStepsB));
                        }
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        rightModificationCount++;
                        ChangeLogText += "R, B:\n";
                        ExecuteAction(RightPerson!, BasePerson!, i, actionR, faker, iteration);
                        if (WriteSteps)
                        {
                            steps.Push(new Step(rightStepName, RightPerson!, pathToStepsR));
                            steps.Push(new Step(baseStepName, BasePerson!, pathToStepsB));
                        }
                    }
                }

                // ujisti sa ze sme vytvorili 3way vetvi - ak nie opakuj iteraciu = prepis base/right/left
                if (!SharedMethods.IsValidOutput(TestingOneActionOnce(), leftModificationCount, rightModificationCount))
                {
                    steps.Clear();
                    iteration--;
                    continue;
                }

                // Export krokov - musi byt po ujisteni 3way merge, inak kroky stare + nove sa zmiesaju
                while (steps.Count > 0)
                {
                    var step = steps.Pop();
                    XMLOutput.Export(step.listOfState, step.name, null, step.pathToExport);
                }

                string head = SharedMethods.GetHeadForChangeLog(testingOneActionTwice: TestingOneActionTwice(),
                                                leftKeepProbability: leftKeepProbability,
                                                iteration: iteration,
                                                leftModsCount: leftModificationCount, rightModsCount: rightModificationCount,
                                                allowAdd: AllowAdd, allowRemove: AllowRemove, allowChange: AllowChange,
                                                madeAdditions: MadeAdditions, madeRemovals: MadeRemovals, madeChanges: MadeChanges,
                                                maxAllowedAdditions: MaxAllowedAdditions, maxAllowedRemovals: MaxAllowedRemovals, maxAllowedChanges: MaxAllowedChanges);
                ChangeLogText = head + ChangeLogText;

                XMLOutput.Export(RightPerson!, "right", iteration, OutputDirectory);
                XMLOutput.Export(LeftPerson!, "left", iteration, OutputDirectory);
                XMLOutput.Export(BasePerson!, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultPerson!, "expectedResult", iteration, OutputDirectory);

                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static void Init()
        {
            MadeChanges = 0;
            MadeRemovals = 0;
            MadeAdditions = 0;

            ChangeLogText = string.Empty;

            ResultPerson = CreatePersonWithPlaceholders();

            // Tvorba 2 branchov a ich predka
            LeftPerson = ResultPerson.Clone();
            RightPerson = ResultPerson.Clone();
            BasePerson = ResultPerson.Clone();
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
        private static AtributeAction GetAtributeAction(int remainingPositions)
        {
            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningChange = AllowChange ? MaxAllowedChanges - MadeChanges : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningChange + remaningRemove;

            var availableMods = new List<AtributeAction>();
            if (remaningChange > 0) availableMods.Add(AtributeAction.CHANGE);
            if (remaningRemove > 0) availableMods.Add(AtributeAction.REMOVE);
            if (remaningAdd > 0) availableMods.Add(AtributeAction.ADD);


            var madeModifications = MadeAdditions + MadeChanges + MadeRemovals;

            // týmto sa ujistíme, že aspoň jedna modifikácia nastane pred koncom iterácie
            if (remainingPositions == 1 && madeModifications == 0 && availableMods.Count > 0)
            {
                return availableMods[Random.Shared.Next(availableMods.Count)];
            }

            // ak zostava jedna modifikacia, rovnomerna pravdepodobnost pre kazdy atribut
            if (remainingModifications == 1)
            {
                remainingPositions = Math.Max(1, remainingPositions);
                if (Random.Shared.Next(remainingPositions) != 0)
                {
                    return AtributeAction.KEEP;
                }
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
        private static bool TestingOneActionOnce()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowChange) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 1;
        }

        private static bool TestingOneActionTwice()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowChange) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 2;
        }

        private static long MaxActionsSum()
        {
            long allowedNumberOfActions = 0;
            allowedNumberOfActions += AllowAdd ? MaxAllowedAdditions : 0;
            allowedNumberOfActions += AllowRemove ? MaxAllowedRemovals : 0;
            return allowedNumberOfActions;
        }
    }
}