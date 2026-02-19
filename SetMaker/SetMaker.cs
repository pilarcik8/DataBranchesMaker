using Bogus;
using Shared;

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

            int targetCount = faker.Random.Int(MinResultSize, MaxResultSize);

            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < Iterations; iteration++) // pocet skupin vetiev
            {
                var resultSet = MakeResultSet(targetCount, faker);
                var rightSet = new HashSet<string>(resultSet, StringComparer.Ordinal);
                var leftSet = new HashSet<string>(resultSet, StringComparer.Ordinal);
                var baseSet = new HashSet<string>(resultSet, StringComparer.Ordinal);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

                foreach (string item in resultSet)
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
                        var massage = "L, R, B:";
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        var massage = "R, B:";
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker, iteration);
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        var massage = "L, B:";
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);
                        ExecuteAction(leftSet, baseSet, item, leftAct, faker, iteration);
                    }
                    else
                    {
                        Console.Error.WriteLine("Setmaker - nenašla sa vetva s KEEP akciou\nRA: " + rightAct.ToString() + "LA: " + leftAct.ToString());
                        return;
                    }
                }

                // Vytvor adresár pre aktuálnu iteráciu a exportuj seti
                FileOutput.Export(leftSet, "left", iteration, OutputDirectory);
                FileOutput.Export(rightSet, "right", iteration, OutputDirectory);
                FileOutput.Export(baseSet, "base", iteration, OutputDirectory);
                FileOutput.Export(resultSet, "expectedResult", iteration, OutputDirectory);
                Console.WriteLine("--------------------------------------------------");

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
            }
            else if (action == SetAction.ADD)
            {
                string newItem = faker.Random.Word();
                while (branchSet.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }
                baseSet.Add(newItem);
                branchSet.Add(newItem);
            }
            else if (action != SetAction.KEEP)
            {
                UnauthorizedAccessException ex = new($"Neznáma akcia: {action}");
                Console.Error.WriteLine(ex.Message);
                return;
            }
            FileOutput.WriteTxtSingleRow("changeLog", messege, iteration, OutputDirectory);
        }

        public static SetAction GetElementAction()
        {
            List<SetAction> allowed = [SetAction.KEEP];
            if (AllowRemove && MaxAllowedRemovals > MadeRemovals) {
                allowed.Add(SetAction.REMOVE); 
            }
            if (AllowAdd && MaxAllowedAdditions > MadeAdditions)
            {
                allowed.Add(SetAction.ADD);
            }

            int index = new Random().Next(allowed.Count);

            return allowed.ElementAt(index);
        }
    }
}