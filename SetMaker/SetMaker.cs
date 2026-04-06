using Bogus;
using Microsoft.VisualBasic;
using Shared;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SetMaker
{
    internal enum SetAction
    {
        KEEP,
        REMOVE,
        ADD
    }
    struct RowAction
    {
        public SetAction Right;
        public SetAction Left;
        public SetAction Base => Right == SetAction.KEEP ? Left : Right;
    }

    public static class SetMaker
    {
        static int PreperedRemovals = 0;
        static int PreperedAdditions = 0;

        //Konfigurácia generovania dát
        public static int MaxResultSize { get; set; } = 10;
        public static int MinResultSize { get; set; } = 10;
        public static int Iterations { get; set; } = 5;
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdditions { get; set; } = true;
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
        public static bool TestingOneActionOnce = false;

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, string outputDirectory, int minResultSize, int maxResultSize, bool shuffle, bool writeSteps)
        {
            if (numberIterations <= 0)
            {
                throw new Exception("Nízka hodnota iterácí");
            }
            if (!removingAllowed && !addingAllowed)
            {
                throw new Exception("Žiadna operácia nie je povolená");
            }

            if (maxResultSize <= 0 || minResultSize <= 0)
            {
                throw new Exception("Nízka hodnota veľkosti výsledku");
            }

            if (maxResultSize < minResultSize)
            {
                throw new Exception("Maximálna veľkosť výsledku musí být větší nebo rovna minimální velikosti výsledku");
            }

            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdditions = addingAllowed;
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
            int nunOfAllowedActions = SharedMethods.GetNumberOfAllowedActions(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove);
            long numbOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals);
            TestingOneActionOnce = SharedMethods.LearnIfCurrentlyTestingOneActionOnce(nunOfAllowedActions, numbOfMaxMods);

            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < Iterations; iteration++) // pocet skupin vetiev
            {
                Init(faker);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                var actions = GenerateRowActions(leftKeepProbability);
                int leftActionsCount = actions.Count(a => a.Left != SetAction.KEEP);
                int rightActionsCount = actions.Count(a => a.Right != SetAction.KEEP);

                for (int i = 0; i < ResultSet.Count; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    string element = ResultSet.ElementAt(i);
                    var leftAct = actions[i].Left;
                    var rightAct = actions[i].Right;
                    var baseAct = actions[i].Base;

                    if (leftAct == SetAction.KEEP && rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "L, R, B\n";
                        ExecuteAction(RightSet, BaseSet, element, rightAct, faker, iteration);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        ChangeLogText += "R, B\n";
                        ExecuteAction(RightSet, BaseSet, element, rightAct, faker, iteration);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(RightSet, rightStepName, null, pathToStepsR);
                            XMLOutput.Export(BaseSet, baseStepName, null, pathToStepsB);
                        }
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "L, B\n";
                        ExecuteAction(LeftSet, BaseSet, element, leftAct, faker, iteration);
                        if (WriteSteps)
                        {
                            XMLOutput.Export(LeftSet, leftStepName, null, pathToStepsL);
                            XMLOutput.Export(BaseSet, baseStepName, null, pathToStepsB);
                        }
                    }
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

                string head = SharedMethods.GetHeadForChangeLog(
                                                leftKeepProbability: leftKeepProbability,
                                                iteration: iteration,
                                                leftModsCount: leftActionsCount, rightModsCount: rightActionsCount,
                                                allowAdd: AllowAdditions, allowRemove: AllowRemove,
                                                madeAdditions: PreperedAdditions, madeRemovals: PreperedRemovals,
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

            PreperedRemovals = 0;
            PreperedAdditions = 0;
            ChangeLogText = string.Empty;
        }

        private static List<RowAction> GenerateRowActions(double leftKeepProbability)
        {
            var actions = new List<RowAction>();

            int leftModificationCount = 0;
            int rightModificationCount = 0;

            var sumOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdditions, isAllowedRemove: AllowRemove, maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals);
            var sealingOfMods = Math.Min(ResultSet.Count, sumOfMaxMods);
            var bottomOfMods = TestingOneActionOnce ? 1 : 2;
            var numberOfMods = Random.Shared.Next(bottomOfMods, (int)(sealingOfMods + 1));

            for (int i = 0; i < numberOfMods; i++)
            {
                if (SharedMethods.ShouldNextModificationBeOnLeft(leftModificationCount, rightModificationCount, leftKeepProbability))
                {
                    actions.Add(new RowAction { Left = GetNonKeepAction(), Right = SetAction.KEEP });
                    leftModificationCount++;
                }
                else
                {
                    actions.Add(new RowAction { Left = SetAction.KEEP, Right = GetNonKeepAction() });
                    rightModificationCount++;
                }
            }

            while (actions.Count < ResultSet.Count)
            {
                var randIndex = Random.Shared.Next(actions.Count + 1);
                actions.Insert(randIndex, new RowAction { Left = SetAction.KEEP, Right = SetAction.KEEP });
            }
            return actions;
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
            if (action == SetAction.REMOVE)
            {
                ChangeLogText += $"Removing element: '{item}'\n";
                baseSet.Remove(item);
                branchSet.Remove(item);
            }
            else if (action == SetAction.ADD)
            {
                ChangeLogText += $"Adding element: '{item}'\n";
                string newItem = SharedMethods.GetNewUniqueWord(faker, BaseSet.ToList(), LeftSet.ToList(), RightSet.ToList(), ResultSet.ToList());
                baseSet.Add(newItem);
                branchSet.Add(newItem);
            }
            else if (action == SetAction.KEEP)
            {
                ChangeLogText += $"Keeping element: '{item}'\n";
            }
        }

        private static SetAction GetNonKeepAction()
        {
            var allowed = new List<SetAction>();
            if (AllowRemove && MaxAllowedRemovals > PreperedRemovals) allowed.Add(SetAction.REMOVE);
            if (AllowAdditions && MaxAllowedAdditions > PreperedAdditions) allowed.Add(SetAction.ADD);

            if (allowed.Count == 0) throw new InvalidOperationException("Všetky akcie boli už spotrebované");

            var action = allowed[Random.Shared.Next(allowed.Count)];
            switch (action)
            {
                case SetAction.ADD:
                    PreperedAdditions++;
                    break;
                case SetAction.REMOVE:
                    PreperedRemovals++;
                    break;
            }
            return action;
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
    }
}