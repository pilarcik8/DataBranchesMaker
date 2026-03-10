using Bogus;
using Microsoft.VisualBasic;
using Shared;
using System.Text;

namespace SetMaker
{
    internal enum SetAction
    {
        KEEP,
        REMOVE,
        ADD
    }

    internal struct Step
    {
        public string name { get; }
        public HashSet<string> listOfState { get; }
        public string pathToExport { get; }

        public Step(string name, HashSet<string> hashOfState, string pathToExport)
        {
            this.name = name;
            this.listOfState = new HashSet<string>(hashOfState);
            this.pathToExport = pathToExport;
        }
    }

    public static class SetMaker
    {
        static int MadeRemovals = 0;
        static int MadeAdditions = 0;

        //Konfigurácia generovania dát
        public static int MaxResultSize { get; set; } = 10;
        public static int MinResultSize { get; set; } = 10;
        public static int Iterations { get; set; } = 5;
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdd { get; set; } = true;
        public static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        public static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        public static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SetMakerOutput");
        public static string ChangeLogText = "";
        public static bool WriteSteps { get; set; } = false;
        public static bool ShuffleBaseLeftRight { get; set; } = false;
        public static HashSet<string> BaseSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> LeftSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> RightSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> ResultSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);


        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, string outputDirectory, int minResultSize, int maxResultSize, bool shuffle, bool writeSteps)
        {
            if (numberIterations <= 0)
            {
                throw new Exception("Zlá nízka hodnota iterácí");
            }
            if (!removingAllowed && !addingAllowed)
            {
                throw new Exception("Žiadna operácia nie je povolená");
            }

            if (maxResultSize <= 0 || minResultSize <= 0)
            {
                throw new Exception("Zlá nízka hodnota veľkosti výsledku");
            }

            if (maxResultSize < minResultSize)
            {
                throw new Exception("Maximálna veľkosť výsledku musí být větší nebo rovna minimální velikosti výsledku");
            }

            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
            ShuffleBaseLeftRight = shuffle;
            WriteSteps = writeSteps;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
        }

        public static void Main()
        {
            var faker = new Faker();

            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < Iterations; iteration++) // pocet skupin vetiev
            {
                Init(faker);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                Stack<Step> steps = new Stack<Step>();
                var (leftActions, rightActions) = GetActionsForItem(leftKeepProbability);

                for (int i = 0; i < ResultSet.Count; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    string item = ResultSet.ElementAt(i);
                    var leftAct = leftActions[i];
                    var rightAct = rightActions[i];

                    if (leftAct == SetAction.KEEP && rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Left, Right and Base\n";
                        ExecuteAction(RightSet, BaseSet, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Right and Base\n";
                        ExecuteAction(RightSet, BaseSet, item, rightAct, faker, iteration);
                        if (WriteSteps)
                        {
                            steps.Push(new Step(rightStepName, RightSet, pathToStepsR));
                            steps.Push(new Step(baseStepName, BaseSet, pathToStepsB));
                        }
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Left and Base\n";
                        ExecuteAction(LeftSet, BaseSet, item, leftAct, faker, iteration);
                        if (WriteSteps)
                        {
                            steps.Push(new Step(leftStepName, LeftSet, pathToStepsL));
                            steps.Push(new Step(baseStepName, BaseSet, pathToStepsB));
                        }
                    }
                }

                while (steps.Count > 0)
                {
                    var step = steps.Pop();
                    XMLOutput.Export(step.listOfState, step.name, null, step.pathToExport);
                }

                if (ShuffleBaseLeftRight)
                {
                    LeftSet = new HashSet<string>(Shuffle(LeftSet.ToList()), StringComparer.Ordinal);
                    RightSet = new HashSet<string>(Shuffle(RightSet.ToList()), StringComparer.Ordinal);
                    BaseSet = new HashSet<string>(Shuffle(BaseSet.ToList()), StringComparer.Ordinal);
                }
                XMLOutput.Export(LeftSet, "left", iteration, OutputDirectory);
                XMLOutput.Export(RightSet, "right", iteration, OutputDirectory);
                XMLOutput.Export(BaseSet, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultSet, "expectedResult", iteration, OutputDirectory);

                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);

                leftActions.RemoveAll(x => x == SetAction.KEEP);
                rightActions.RemoveAll(x => x == SetAction.KEEP);

                string head = SharedMethods.GetHeadForChangeLog(testingOneActionTwice: TestingOneActionTwice(),
                                                leftKeepProbability: leftKeepProbability,
                                                iteration: iteration,
                                                leftModsCount: leftActions.Count, rightModsCount: rightActions.Count,
                                                allowAdd: AllowAdd, allowRemove: AllowRemove,
                                                madeAdditions: MadeAdditions, madeRemovals: MadeRemovals,
                                                maxAllowedAdditions: MaxAllowedAdditions, maxAllowedRemovals: MaxAllowedRemovals);
                ChangeLogText = head + ChangeLogText;
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static void Init(Faker faker)
        {
            BaseSet = MakeResultSet(Random.Shared.Next(MinResultSize, MaxResultSize + 1), faker);
            LeftSet = new HashSet<string>(BaseSet, StringComparer.Ordinal);
            RightSet = new HashSet<string>(BaseSet, StringComparer.Ordinal);
            ResultSet = new HashSet<string>(BaseSet, StringComparer.Ordinal);

            MadeRemovals = 0;
            MadeAdditions = 0;
            ChangeLogText = string.Empty;
        }

        private static (List<SetAction> leftActions, List<SetAction> rightActions) GetActionsForItem(double leftKeepProbability)
        {
            List<SetAction> leftActions = new List<SetAction>();
            List<SetAction> rightActions = new List<SetAction>();

            int leftModificationCount = 0;
            int rightModificationCount = 0;

            for (int i = 0; i < ResultSet.Count; i++)
            {
                leftActions.Add(SetAction.KEEP);
                rightActions.Add(SetAction.KEEP);
            }

            // helper
            SetAction GetNonKeepAction()
            {
                if (AllowRemove && AllowAdd) throw new InvalidOperationException("Obe akcie sú povolené, no program si myslý, že je iba jedna");
                if (AllowAdd) return SetAction.ADD;
                return SetAction.REMOVE;                
            }

            // jedna modifikacia
            if (TestingOneActionOnce())
            {
                int index = Random.Shared.Next(ResultSet.Count);
                var action = GetNonKeepAction();

                if (SharedMethods.NextModificationIsOnLeft(TestingOneActionTwice(), leftModificationCount, rightModificationCount, leftKeepProbability))
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
            else if (TestingOneActionTwice())
            {
                var action = GetNonKeepAction();
                var indexes = new HashSet<int>();

                for (int i = 0; i < 2; i++)
                {
                    int index = Random.Shared.Next(ResultSet.Count);
                    while (indexes.Contains(index))
                    {
                        index = Random.Shared.Next(ResultSet.Count);
                    }

                    if (SharedMethods.NextModificationIsOnLeft(TestingOneActionTwice(), leftModificationCount, rightModificationCount, leftKeepProbability))
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
                for (int j = 0; j < ResultSet.Count; j++)
                {
                    var action = GetElementAction();
                    if (SharedMethods.NextModificationIsOnLeft(TestingOneActionTwice(), leftModificationCount, rightModificationCount, leftKeepProbability))
                    {
                        leftActions[j] = action;
                        if (action != SetAction.KEEP) leftModificationCount++;
                    }
                    else
                    {
                        rightActions[j] = action;
                        if (action != SetAction.KEEP) rightModificationCount++;
                    }
                }
            }
            // ujisti sa ze sme vytvorili 3way vetvy - ak nie opakuj iteraciu = prepis base/right/left
            if (!SharedMethods.IsValidOutput(TestingOneActionOnce(), leftModificationCount, rightModificationCount))
            {
                return GetActionsForItem(leftKeepProbability);
            }
            return (leftActions, rightActions);
        }

        private static HashSet<string> MakeResultSet(int size, Faker faker)
        {
            var resultSet = new HashSet<string>(StringComparer.Ordinal);

            while (resultSet.Count < size)
            {
                string value = faker.Random.Word();
                if (!resultSet.Contains(value))
                {
                    resultSet.Add(value);
                }
            }
            return resultSet;
        }

        private static void ExecuteAction(HashSet<string> branchSet, HashSet<string> baseSet, string item, SetAction action, Faker faker, int iteration)
        {
            var messege = $"'{action}' for item: '{item}'";
            if (action == SetAction.REMOVE)
            {
                baseSet.Remove(item);
                branchSet.Remove(item);
                MadeRemovals++;
            }
            else if (action == SetAction.ADD)
            {
                string newItem = SharedMethods.GetNewUniqueWord(faker, BaseSet.ToList(), LeftSet.ToList(), RightSet.ToList(), ResultSet.ToList());
                baseSet.Add(newItem);
                branchSet.Add(newItem);
                MadeAdditions++;
            }
            ChangeLogText += messege + "\n";
        }

        private static SetAction GetElementAction()
        {
            var allowed = new List<SetAction> { SetAction.KEEP };

            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            if (remaningRemove > 0)
            {
                allowed.Add(SetAction.REMOVE);
            }
            if (remaningAdd > 0)
            {
                allowed.Add(SetAction.ADD);
            }

            int index = Random.Shared.Next(allowed.Count);
            return allowed[index];
        }

        // Fisher–Yates algoritmus pre náhodné premiešanie prvkov v zozname
        private static List<string> Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static bool TestingOneActionOnce()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 1;
        }

        private static bool TestingOneActionTwice()
        {
            if (ResultSet.Count < 2) return false;

            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;

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