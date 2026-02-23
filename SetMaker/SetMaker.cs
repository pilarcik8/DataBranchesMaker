using Bogus;
using Shared;
using System.Text;

namespace SetMaker
{
    public enum SetAction
    {
        KEEP,
        REMOVE,
        ADD
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
        public static HashSet<string> BaseSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> LeftSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> RightSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public static HashSet<string> ResultSet { get; set; } = new HashSet<string>(StringComparer.Ordinal);


        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, string outputDirectory, int minResultSize, int maxResultSize)
        {
            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            OutputDirectory = outputDirectory;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
        }

        public static void Main()
        {
            var faker = new Faker();
            MadeRemovals = 0;
            MadeAdditions = 0;

            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < Iterations; iteration++) // pocet skupin vetiev
            {
                ChangeLogText = string.Empty;
                int targetCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

                ResultSet = MakeResultSet(targetCount, faker);
                RightSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);
                LeftSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);
                BaseSet = new HashSet<string>(ResultSet, StringComparer.Ordinal);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                ChangeLogText += $"Iteration {iteration}: Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}\n";

                foreach (string item in ResultSet)
                {
                    SetAction leftAct, rightAct;
                    if (Random.Shared.NextDouble() > leftKeepProbability)
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
                        ExecuteAction(RightSet, BaseSet, item, rightAct, faker, iteration);
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        ChangeLogText += "Left and Base\n";
                        ExecuteAction(LeftSet, BaseSet, item, leftAct, faker, iteration);
                    }
                    else
                    {
                        ChangeLogText += "Setmaker - nenašla sa vetva s KEEP akciou\nRA: " + rightAct.ToString() + "LA: " + leftAct.ToString() + "\n";
                        return;
                    }
                }

                XMLOutput.Export(LeftSet, "left", iteration, OutputDirectory);
                XMLOutput.Export(RightSet, "right", iteration, OutputDirectory);
                XMLOutput.Export(BaseSet, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultSet, "expectedResult", iteration, OutputDirectory);

                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
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
            var messege = $"Action: {action} for item: {item}";
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

        public static SetAction GetElementAction()
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
    }
}