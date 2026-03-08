using Bogus;
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


        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, string outputDirectory, int minResultSize, int maxResultSize, bool shuffle)
        {
            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
            ShuffleBaseLeftRight = shuffle;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
        }

        public static void EnableWriteSteps(bool enable)
        {
            WriteSteps = enable;
        }

        public static void Main()
        {
            var faker = new Faker();
            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < Iterations; iteration++) // pocet skupin vetiev
            {
                MadeRemovals = 0;
                MadeAdditions = 0;
                ChangeLogText = string.Empty;
                int targetCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

                ResultSet = MakeResultSet(targetCount, faker);
                RightSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);
                LeftSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);
                BaseSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;

                int leftModificationCount = 0;
                int rightModificationCount = 0;
                Stack<Step> steps = new Stack<Step>();

                int i = 0;
                foreach (string item in ResultSet)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    SetAction leftAct, rightAct;
                    if (Random.Shared.NextDouble() < leftKeepProbability)
                    {
                        leftAct = SetAction.KEEP;
                        rightAct = GetElementAction();
                    }
                    else
                    {
                        leftAct = GetElementAction();
                        rightAct = SetAction.KEEP;
                    }

                    if (leftAct == SetAction.KEEP && rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Left, Right and Base\n";
                        ExecuteAction(RightSet, BaseSet, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Right and Base\n";
                        rightModificationCount++;
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
                        leftModificationCount++;
                        ExecuteAction(LeftSet, BaseSet, item, leftAct, faker, iteration);
                        if (WriteSteps)
                        {
                            steps.Push(new Step(leftStepName, LeftSet, pathToStepsL));
                            steps.Push(new Step(baseStepName, BaseSet, pathToStepsB));
                        }
                    }
                    i++;
                }

                if (!ValidOutput(leftModificationCount, rightModificationCount))
                {
                    steps.Clear();
                    iteration--;
                    continue;
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
                ChangeLogText = WriteHeadForChangeLog(leftKeepProbability, iteration, leftModificationCount, rightModificationCount) + ChangeLogText;
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static bool ValidOutput(int leftModificationCount, int rightModificationCount)
        {
            if (TestingOneActionOnce())
            {
                return (leftModificationCount + rightModificationCount == 1);
            }
            return (leftModificationCount > 0 && rightModificationCount > 0);
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
                string newItem = faker.Random.Word();
                while (BaseSet.Contains(newItem) || LeftSet.Contains(newItem) || RightSet.Contains(newItem) || ResultSet.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }
                baseSet.Add(newItem);
                branchSet.Add(newItem);
                MadeAdditions++;
            }
            ChangeLogText += messege + "\n";
        }

        private static string WriteHeadForChangeLog(double leftKeepProbability, int iteration, int leftModsCount, int rightModsCount)
        {
            string head = "Allowed Actions: ";
            if (AllowRemove) head += "Remove ";
            if (AllowAdd) head += "Add ";
            head += "\n";

            head += "Max Allowed Actions: ";
            if (AllowRemove) head += $" Remove: {MaxAllowedRemovals} ";
            if (AllowAdd) head += $"Add: {MaxAllowedAdditions} ";
            head += "\n";

            head += "Total modifications: ";
            if (AllowRemove) head += $"Removals: {MadeRemovals} ";
            if (AllowAdd) head += $"Additions: {MadeAdditions} ";
            head += "\n\n";

            if (TestingOneActionTwice())
            {
                head += $"Iteration {iteration}: One modification for Left and Base, another for Right and Base\n";
            }
            else
            {
                head += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {1 - leftKeepProbability:P0}\n";
            }
            head += $"Number of modifications: Left: {leftModsCount}, Right: {rightModsCount}\n\n";
            return head;
        }

        private static SetAction GetElementAction()
        {
            var allowed = new List<SetAction> { SetAction.KEEP };

            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningRemove;

            if (remainingModifications == 1)
            {
                int randomValue = Random.Shared.Next(5);
                if (randomValue != 0) return SetAction.KEEP;
            }

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