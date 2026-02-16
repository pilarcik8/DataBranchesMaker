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

    public static class Program
    {
        static int MadeRemovals;
        static int MadeAdditions;

        //Konfigurácia generovania dát
        const int MAX_RESULT_SET_SIZE = 10;
        const int MIN_RESULT_SET_SIZE = 10;

        const int ITERATIONS = 5;

        const bool ALLOW_REMOVE = true;
        const bool ALLOW_ADD = true;

        const int MAX_ALLOWED_REMOVALS = int.MaxValue;
        const int MAX_ALLOWED_ADDITIONS = int.MaxValue;

        public static void Main()
        {
            // Validácia konfigurácie
            if (MAX_RESULT_SET_SIZE < MIN_RESULT_SET_SIZE || 
                MIN_RESULT_SET_SIZE <= 0 ||
                MAX_RESULT_SET_SIZE <= 0)
            {
                Console.Error.WriteLine("Set s takymi velkostami nie je mozne vytvoriť");
                return;
            }

            if (!ALLOW_ADD && !ALLOW_REMOVE)
            {
                Console.WriteLine("Nie je možné provést žádné změny, oba ALLOW_ADD a ALLOW_REMOVE jsou nastaveny na false.");
                // return; // Po validacii KEEP odkomentuj!
            }
            var faker = new Faker();
            MadeRemovals = 0;
            MadeAdditions = 0;

            int targetCount = faker.Random.Int(MIN_RESULT_SET_SIZE, MAX_RESULT_SET_SIZE);

            // Vytvorenie základneho (vysledkovy) setu
            for (int iteration = 0; iteration < ITERATIONS; iteration++) // pocet skupin vetiev
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
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == SetAction.KEEP)
                    {
                        var massage = "R, B:";
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration);
                        ExecuteAction(rightSet, baseSet, item, rightAct, faker, iteration);
                    }
                    else if (rightAct == SetAction.KEEP)
                    {
                        var massage = "L, B:";
                        FileOutput.WriteTxtSingleRow("changeLog", massage, iteration);
                        ExecuteAction(leftSet, baseSet, item, leftAct, faker, iteration);
                    }
                    else
                    {
                        Console.Error.WriteLine("Setmaker - nenašla sa vetva s KEEP akciou\nRA: " + rightAct.ToString() + "LA: " + leftAct.ToString());
                        return;
                    }
                }

                // Vytvor adresár pre aktuálnu iteráciu a exportuj seti
                FileOutput.Export(leftSet, "left", iteration);
                FileOutput.Export(rightSet, "right", iteration);
                FileOutput.Export(baseSet, "base", iteration);
                FileOutput.Export(resultSet, "result", iteration);
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
            FileOutput.WriteTxtSingleRow("changeLog", messege, iteration);
        }

        public static SetAction GetElementAction()
        {
            List<SetAction> allowed = [SetAction.KEEP];
            if (ALLOW_REMOVE && MAX_ALLOWED_REMOVALS > MadeRemovals) {
                allowed.Add(SetAction.REMOVE); 
            }
            if (ALLOW_ADD && MAX_ALLOWED_ADDITIONS > MadeAdditions)
            {
                allowed.Add(SetAction.ADD);
            }

            int index = new Random().Next(allowed.Count);

            return allowed.ElementAt(index);
        }
    }
}