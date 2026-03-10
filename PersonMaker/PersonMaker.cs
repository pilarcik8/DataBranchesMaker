using Bogus;
using Shared;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using static System.Collections.Specialized.BitVector32;
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
        public static bool TestingOneActionTwice = false;
        public static bool TestingOneActionOnce = false;
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

            Iterations = numberIterations;
            AllowChange = changesAllowed;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
            WriteSteps = writeSteps;
        }

        public static void SetMaxAllowed(int maxChanges, int maxRemovals, int maxAdditions)
        {
            MaxAllowedChanges = maxChanges;
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
        }

        public static void Main()
        {
            var faker = new Faker("en"); 
            int baseAtributeCount = typeof(Person).GetProperties().Length; //null nepocita

            int nunOfAllowedActions = SharedMethods.GetNumberOfAllowedActions(isAllowedAdd: AllowAdd, isAllowedRemove: AllowRemove, isAllowedChange: AllowChange);
            long numbOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdd, isAllowedRemove: AllowRemove, isAllowedChange: AllowChange,
                                                                maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxChange: MaxAllowedChanges);
            TestingOneActionOnce = SharedMethods.LearnIfCurrentlyTestingOneActionOnce(nunOfAllowedActions, numbOfMaxMods);
            TestingOneActionTwice = SharedMethods.LearnIfCurrentlyTestingOneActionTwice(baseAtributeCount, nunOfAllowedActions, numbOfMaxMods);

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Init();

                // Pre vasciu diferenciaciu r a l branchov pocas generovania
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                var (leftActions, rightActions) = GetActionsForItem(leftKeepProbability);

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
                    AtributeAction actionR = rightActions[i];
                    AtributeAction actionL = leftActions[i];

                    if (actionR == AtributeAction.KEEP && actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "L, R, B:\n";
                        ExecuteAction(LeftPerson!, BasePerson!, i, actionL, faker, iteration);
                        continue;
                    }
                    else if (actionR == AtributeAction.KEEP)
                    {
                        ChangeLogText += "L, B:\n";
                        ExecuteAction(LeftPerson!, BasePerson!, i, actionL, faker, iteration);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(LeftPerson!, leftStepName, null, pathToStepsL);
                            XMLOutput.Export(BasePerson!, baseStepName, null, pathToStepsB);
                        }
                    }
                    else if (actionL == AtributeAction.KEEP)
                    {
                        ChangeLogText += "R, B:\n";
                        ExecuteAction(RightPerson!, BasePerson!, i, actionR, faker, iteration);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(RightPerson!, rightStepName, null, pathToStepsR);
                            XMLOutput.Export(BasePerson!, baseStepName, null, pathToStepsB);
                        }
                    }
                }

                leftActions.RemoveAll(x => x == AtributeAction.KEEP);
                rightActions.RemoveAll(x => x == AtributeAction.KEEP);

                string head = SharedMethods.GetHeadForChangeLog(testingOneActionTwice: TestingOneActionTwice,
                                                leftKeepProbability: leftKeepProbability,
                                                iteration: iteration,
                                                leftModsCount: leftActions.Count, rightModsCount: rightActions.Count,
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

        private static (List<AtributeAction> leftActions, List<AtributeAction> rightActions) GetActionsForItem(double leftKeepProbability)
        {
            List<AtributeAction> leftActions = new List<AtributeAction>();
            List<AtributeAction> rightActions = new List<AtributeAction>();

            int leftModificationCount = 0;
            int rightModificationCount = 0;

            for (int i = 0; i < typeof(Person).GetProperties().Length; i++)
            {
                leftActions.Add(AtributeAction.KEEP);
                rightActions.Add(AtributeAction.KEEP);
            }

            // helper
            AtributeAction GetNonKeepAction()
            {
                int sum = 0;
                if (AllowAdd) sum++;
                if (AllowChange) sum++;
                if (AllowRemove) sum++;

                if (sum != 1) throw new InvalidOperationException("Nespravne pouzity GetNonKeepAction");
                if (AllowAdd) return AtributeAction.ADD;
                if (AllowChange) return AtributeAction.CHANGE;
                return AtributeAction.REMOVE;
            }

            // jedna modifikacia
            if (TestingOneActionOnce)
            {
                int index = Random.Shared.Next(typeof(Person).GetProperties().Length);
                var action = GetNonKeepAction();

                if (SharedMethods.ShouldNextModificationBeOnLeft(TestingOneActionTwice, leftModificationCount, rightModificationCount, leftKeepProbability))
                {
                    leftActions[index] = action;
                    leftModificationCount++;
                }
                else
                {
                    rightActions[index] = action;
                    rightModificationCount++;
                }
            }

            // dve modifikacie
            else if (TestingOneActionTwice)
            {
                var action = GetNonKeepAction();
                var indexes = new HashSet<int>();

                for (int i = 0; i < 2; i++)
                {
                    int index = Random.Shared.Next(typeof(Person).GetProperties().Length);
                    while (indexes.Contains(index))
                    {
                        index = Random.Shared.Next(typeof(Person).GetProperties().Length);
                    }

                    if (SharedMethods.ShouldNextModificationBeOnLeft(TestingOneActionTwice, leftModificationCount, rightModificationCount, leftKeepProbability))
                    {
                        indexes.Add(index);
                        leftActions[index] = action;
                        leftModificationCount++;
                    }
                    else
                    {
                        indexes.Add(index);
                        rightActions[index] = action;
                        rightModificationCount++;
                    }
                }
            }
            else
            {
                // normalny beh
                for (int j = 0; j < typeof(Person).GetProperties().Length; j++)
                {
                    var action = GetAtributeAction();
                    if (SharedMethods.ShouldNextModificationBeOnLeft(TestingOneActionTwice, leftModificationCount, rightModificationCount, leftKeepProbability))
                    {
                        leftActions[j] = action;
                        if (action != AtributeAction.KEEP) leftModificationCount++;
                    }
                    else
                    {
                        rightActions[j] = action;
                        if (action != AtributeAction.KEEP) rightModificationCount++;
                    }
                }
            }
            // ujisti sa ze sme vytvorili 3way vetvy - ak nie opakuj iteraciu = prepis base/right/left
            if (!SharedMethods.IsValidOutput(TestingOneActionOnce, leftModificationCount, rightModificationCount))
            {
                return GetActionsForItem(leftKeepProbability);
            }
            return (leftActions, rightActions);
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
        private static AtributeAction GetAtributeAction()
        {
            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningChange = AllowChange ? MaxAllowedChanges - MadeChanges : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            var availableMods = new List<AtributeAction>();
            if (remaningChange > 0) availableMods.Add(AtributeAction.CHANGE);
            if (remaningRemove > 0) availableMods.Add(AtributeAction.REMOVE);
            if (remaningAdd > 0) availableMods.Add(AtributeAction.ADD);


            var madeModifications = MadeAdditions + MadeChanges + MadeRemovals;

            int evenChance = Math.Max(1, typeof(Person).GetProperties().Length);

            if (TestingOneActionOnce || TestingOneActionTwice)
            {
               if (Random.Shared.Next(evenChance) != 0) return AtributeAction.KEEP;
            } 
            else
            {
                availableMods.Add(AtributeAction.KEEP);
            }

            int index = Random.Shared.Next(availableMods.Count);
            return availableMods.Count == 0 ? AtributeAction.KEEP : availableMods[index];
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
    }
}