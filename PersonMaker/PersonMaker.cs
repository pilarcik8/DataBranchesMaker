using Bogus;
using Shared;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using static System.Collections.Specialized.BitVector32;
using Person = Shared.Person;

namespace PersonMaker
{
    public static class PersonMaker
    {
        private enum AtributeAction
        {
            KEEP,
            CHANGE,
            REMOVE,
            ADD
        }
        private struct RowAction
        {
            public AtributeAction Right;
            public AtributeAction Left;
            public AtributeAction Base => Right == AtributeAction.KEEP ? Left : Right;
        }

        private static int PreperedChanges = 0;
        private static int PreperedRemovals = 0;
        private static int PreperedAdditions = 0;

        private static int Iterations { get; set; } = 5;
        private static bool AllowChange { get; set; } = true;
        private static bool AllowRemove { get; set; } = true;
        private static bool AllowAdditions { get; set; } = true;
        private static int MaxAllowedChanges { get; set; } = int.MaxValue;
        private static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        private static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        private static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PersonMakerOutput");

        private static string ChangeLogText = "";
        private static Person? BasePerson { get; set; }
        private static Person? LeftPerson { get; set; }
        private static Person? RightPerson { get; set; }
        private static Person? ResultPerson { get; set; }
        private static bool WriteSteps { get; set; } = false;
        private static bool TestingOneActionOnce = false;
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
            AllowAdditions = addingAllowed;
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

            int nunOfAllowedActions = SharedMethods.GetNumberOfAllowedActions(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedChange: AllowChange);
            long numbOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedChange: AllowChange,
                                                                maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxChange: MaxAllowedChanges);
            TestingOneActionOnce = SharedMethods.LearnIfCurrentlyTestingOneActionOnce(nunOfAllowedActions, numbOfMaxMods);
            
            if (numbOfMaxMods <= 0)
            {
                throw new Exception("Maximálny počet modifikácií musí být väčší ako 0");
            }

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Init();

                // Pre vasciu diferenciaciu r a l branchov pocas generovania
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                var actions = GenerateRowActions(leftKeepProbability);
                int leftActionsCount = actions.Count(a => a.Left != AtributeAction.KEEP);
                int rightActionsCount = actions.Count(a => a.Right != AtributeAction.KEEP);

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
                    AtributeAction actionR = actions[i].Right;
                    AtributeAction actionL = actions[i].Left;

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

                string head = SharedMethods.GetHeadForChangeLog(
                                                leftKeepProbability: leftKeepProbability,
                                                iteration: iteration,
                                                leftModsCount: leftActionsCount, rightModsCount: rightActionsCount,
                                                allowAdd: AllowAdditions, allowRemove: AllowRemove, allowChange: AllowChange,
                                                madeAdditions: PreperedAdditions, madeRemovals: PreperedRemovals, madeChanges: PreperedChanges,
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
            PreperedChanges = 0;
            PreperedRemovals = 0;
            PreperedAdditions = 0;

            ChangeLogText = string.Empty;

            ResultPerson = CreatePersonWithPlaceholders();

            // Tvorba 2 branchov a ich predka
            LeftPerson = ResultPerson.Clone();
            RightPerson = ResultPerson.Clone();
            BasePerson = ResultPerson.Clone();
        }

        private static List<RowAction> GenerateRowActions(double leftKeepProbability)
        {
            var actions = new List<RowAction>();
            int baseAtributeCount = typeof(Person).GetProperties().Length; //null nepocita

            int leftModificationCount = 0;
            int rightModificationCount = 0;

            var sumOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, isAllowedChange: AllowChange, maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxChange: MaxAllowedChanges);
            var sealingOfMods = Math.Min(baseAtributeCount, sumOfMaxMods);
            var bottomOfMods = TestingOneActionOnce ? 1 : 2;
            var numberOfMods = Random.Shared.Next(bottomOfMods, (int)(sealingOfMods + 1));

            for (int i = 0; i < numberOfMods; i++)
            {
                if (SharedMethods.ShouldNextModificationBeOnLeft(leftModificationCount, rightModificationCount, leftKeepProbability))
                {
                    actions.Add(new RowAction { Left = GetNonKeepAction(), Right = AtributeAction.KEEP });
                    leftModificationCount++;
                }
                else
                {
                    actions.Add(new RowAction { Left = AtributeAction.KEEP, Right = GetNonKeepAction() });
                    rightModificationCount++;
                }
            }

            while (actions.Count < baseAtributeCount)
            {
                var randIndex = Random.Shared.Next(actions.Count + 1);
                actions.Insert(randIndex, new RowAction { Left = AtributeAction.KEEP, Right = AtributeAction.KEEP });
            }
            return actions;
        }
        private static AtributeAction GetNonKeepAction()
        {
            var allowed = new List<AtributeAction>();
            if (AllowRemove && MaxAllowedRemovals > PreperedRemovals) allowed.Add(AtributeAction.REMOVE);
            if (AllowAdditions && MaxAllowedAdditions > PreperedAdditions) allowed.Add(AtributeAction.ADD);
            if (AllowChange && MaxAllowedChanges > PreperedChanges) allowed.Add(AtributeAction.CHANGE);

            if (allowed.Count == 0) throw new InvalidOperationException("Všetky akcie boli už spotrebované");

            var action = allowed[Random.Shared.Next(allowed.Count)];
            switch (action)
            {
                case AtributeAction.ADD:
                    PreperedAdditions++;
                    break;
                case AtributeAction.REMOVE:
                    PreperedRemovals++;
                    break;
                case AtributeAction.CHANGE:
                    PreperedChanges++;
                    break;
            }
            return action;
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
            }
            else if (action == AtributeAction.REMOVE)
            {
                var oldValue = branchPerson.GetAttribute(i);
                var attrName = branchPerson.GetAttributeName(i);
                ChangeLogText += $"Removed attribute: '{attrName}' with value '{oldValue}'\n";

                branchPerson.RemoveAtribute(i);
                basePerson.RemoveAtribute(i);
            }
            else if (action == AtributeAction.ADD)
            {
                var attributeBeforeAdd = branchPerson.GetAttributeName(i);
                var valueAndNameOfNewAttribute = branchPerson.AddAttribute(i, faker);
                string valueOfNewAttribute = valueAndNameOfNewAttribute[0];
                string nameOfNewAttribute = valueAndNameOfNewAttribute[1];

                basePerson.AddAttribute(i, faker, valueOfNewAttribute);

                ChangeLogText += $"Added new attribute named '{nameOfNewAttribute}' with value '{valueOfNewAttribute}' was added before attribute '{attributeBeforeAdd}'\n";
            }
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