using Bogus;
using Shared;

namespace ListMaker
{
    public enum ListAction
    {
        KEEP,
        REMOVE,
        ADD,
        SHIFT
    }

    public static class ListMaker
    {
        private static int MadeRemovals = 0;
        private static int MadeAdditions = 0;
        private static int MadeShifts = 0;

        public static int Iterations { get; set; } = 5;
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdd { get; set; } = true;
        public static bool AllowShift { get; set; } = true;
        public static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        public static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        public static int MaxAllowedShifts { get; set; } = int.MaxValue;
        public static int MaxResultSize { get; set; } = 10;
        public static int MinResultSize { get; set; } = 10;
        public static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ListMakerOutput");

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, bool allowShifts, string outputDirectory, int minResultSize, int maxResultSize)
        {
            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            Iterations = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            AllowShift = allowShifts;
            OutputDirectory = outputDirectory;
        }

        public static void SetAllowedMax(int maxRemovals, int maxAdditions, int maxShifts)
        {
            MaxAllowedRemovals = maxRemovals;
            MaxAllowedAdditions = maxAdditions;
            MaxAllowedShifts = maxShifts;
        }

        public static void Main()
        {
            var faker = new Faker();
            MadeAdditions= 0; 
            MadeRemovals = 0; 
            MadeShifts = 0;

            int targetCount = Random.Shared.Next(MinResultSize, MaxResultSize);

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                var resultList = new List<string>();

                while (resultList.Count < targetCount)
                {
                    string value = faker.Random.Word();
                    if (!resultList.Contains(value))
                    {
                        resultList.Add(value);
                    }
                }

                var rightList = new List<string>(resultList);
                var leftList = new List<string>(resultList);
                var baseList = new List<string>(resultList);

                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                double rightKeepProbability = 1.0 - leftKeepProbability;
                Console.WriteLine($"Left KEEP probability: {leftKeepProbability:P0}, Right KEEP probability: {rightKeepProbability:P0}");

                foreach (string item in resultList)
                {
                    ListAction leftAct, rightAct;
                    string massage;
                    if (Random.Shared.NextDouble() > leftKeepProbability)
                    {
                        leftAct = ListAction.KEEP;
                        rightAct = GetAction();
                    }
                    else
                    {
                        leftAct = GetAction();
                        rightAct = ListAction.KEEP;
                    }

                    if (leftAct == ListAction.KEEP && rightAct == ListAction.KEEP)
                    {
                        massage = "L, R, B:";
                        ExecuteAction(rightList, baseList, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == ListAction.KEEP)
                    {
                        massage = "R, B:";
                        ExecuteAction(rightList, baseList, item, rightAct, faker, iteration);
                    }
                    else if (rightAct == ListAction.KEEP)
                    {
                        massage = "L, B:";
                        ExecuteAction(leftList, baseList, item, leftAct, faker, iteration);
                    }
                    else
                    {
                        Console.Error.WriteLine("Nezanama akcia");
                        return;
                    }
                    FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);
                }

                FileOutput.Export(leftList, "left", iteration, OutputDirectory);
                FileOutput.Export(rightList, "right", iteration, OutputDirectory);
                FileOutput.Export(baseList, "base", iteration, OutputDirectory);
                FileOutput.Export(resultList, "expectedResult", iteration, OutputDirectory);
                Console.WriteLine("--------------------------------------------------");

            }

        }

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, ListAction action, Faker faker, int iteration)
        {
            if (action == ListAction.KEEP)
            {
                var massage = $"Keeping item: {item}";
                FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);
            }
            else if (action == ListAction.REMOVE)
            {
                var massage = $"Removing item: {item}";
                FileOutput.WriteTxtSingleRow("changeLog", massage, iteration, OutputDirectory);

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ListAction.ADD)
            {
                string newItem = faker.Random.Word();
                while (branchList.Contains(newItem))
                {
                    newItem = faker.Random.Word();
                }

                int currentIndex = branchList.IndexOf(item);
                if (currentIndex < 0)
                {
                    throw new InvalidOperationException($"Item '{item}' not found in branch list - cannot insert relative to it.");
                }

                branchList.Insert(currentIndex, newItem);

                int baseIndex = baseList.IndexOf(item);
                if (baseIndex >= 0)
                {
                    baseList.Insert(baseIndex, newItem);
                }
                else
                {
                    baseList.Add(newItem);
                }

                string message = $"Adding item: {newItem} at index {currentIndex}";
                FileOutput.WriteTxtSingleRow("changeLog", message, iteration, OutputDirectory);
                MadeAdditions++;
            }
            else if (action == ListAction.SHIFT)
            {
                int count = branchList.Count;

                if (count <= 1)
                {
                    var msg = $"Cannot shift item '{item}' in a list with <= 1 element.";
                    FileOutput.WriteTxtSingleRow("changeLog", msg, iteration, OutputDirectory);
                    return;
                }

                int currentIndex = branchList.IndexOf(item);
                if (currentIndex < 0)
                {
                    throw new InvalidOperationException($"Item '{item}' not found in branch list - cannot shift.");

                }

                int targetIndex = Random.Shared.Next(count);

                while (targetIndex == currentIndex)
                {
                    targetIndex = Random.Shared.Next(count);
                }

                branchList.RemoveAt(currentIndex);
                int insertIndex = targetIndex > currentIndex ? targetIndex - 1 : targetIndex;
                insertIndex = Math.Clamp(insertIndex, 0, branchList.Count);
                branchList.Insert(insertIndex, item);

                int baseIndex = baseList.IndexOf(item);
                if (baseIndex >= 0 && baseList.Count > 1)
                {
                    int baseCount = baseList.Count;
                    int baseTarget = Math.Min(targetIndex, baseCount - 1);
                    baseList.RemoveAt(baseIndex);
                    int baseInsert = baseTarget > baseIndex ? baseTarget - 1 : baseTarget;
                    baseInsert = Math.Clamp(baseInsert, 0, baseList.Count);
                    baseList.Insert(baseInsert, item);
                }

                var message = $"Shifting item: '{item}' from index {currentIndex} to {insertIndex}";
                FileOutput.WriteTxtSingleRow("changeLog", message, iteration, OutputDirectory);
                MadeShifts++;
            }
        }

        public static ListAction GetAction()
        {
            List<ListAction> allowed = [ListAction.KEEP];

            int remaningAdd = AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0;
            int remaningShift = AllowShift ? MaxAllowedShifts - MadeShifts : 0;
            int remaningRemove = AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0;

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            if (remainingModifications == 1)
            {
                int randomValue = Random.Shared.Next(5);
                if (randomValue != 0) return ListAction.KEEP;
            }

            if (remaningShift > 0)
            {
                allowed.Add(ListAction.SHIFT);
            }
            if (remaningRemove > 0)
            {
                allowed.Add(ListAction.REMOVE);
            }
            if (remaningAdd > 0)
            {
                allowed.Add(ListAction.ADD);
            }

            int index = Random.Shared.Next(allowed.Count);
            return allowed[index];
        }
    }
}