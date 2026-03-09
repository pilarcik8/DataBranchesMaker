using Bogus;
using Bogus.DataSets;
using Microsoft.VisualBasic;
using Shared;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ListMaker
{
    internal enum ElementAction
    {
        KEEP,
        REMOVAL,
        ADDITION,
        SHIFT
    }

    internal struct Step
    {
        public string name { get; }
        public List<string> listOfState { get; }
        public string pathToExport { get; }

        public Step(string name, List<string> listOfState, string pathToExport)
        {
            this.name = name;
            this.listOfState = new List<string>(listOfState);
            this.pathToExport = pathToExport;
        }
    }

    public static class ListMaker
    {
        // Počet vykonaných akcií v iterácii, resetuje sa na začiatku každej iterácie
        private static int MadeRemovals = 0;
        private static int MadeAdditions = 0;
        private static int MadeShifts = 0;

        public static int EndIteration { get; set; } = 5;
        // Povolenie jednotlivých akcií
        public static bool AllowRemove { get; set; } = true;
        public static bool AllowAdd { get; set; } = true;
        public static bool AllowShift { get; set; } = true;
        // Maximálny počet povolených akcií (globálne pre všetky iterácie)
        public static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        public static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        public static int MaxAllowedShifts { get; set; } = int.MaxValue;

        // Veľkosť očakávaného výsledku (originálneho xml)
        public static int MaxResultSize { get; set; } = 10;
        public static int MinResultSize { get; set; } = 10;
        public static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ListMakerOutput");
        public static string ChangeLogText = "";
        public static bool WriteSteps { get; set; } = false;

        // Pomocná premenná, ktorá zabezpečí, že po REMOVE musí nasledovat KEEP
        // pri odstraneny 2 za sebou iducich elementov merger nevie presne poradie = xmldiff nahodne vyberie ale sharpdifflib oznaci ako konflikt
        public static bool LastElementWasRemovedFromPosition = false;

        public static HashSet<string> UsedShiftItems = new HashSet<string>();

        public static List<string> ResultList = new List<string>();
        public static List<string> RightList = new List<string>();
        public static List<string> LeftList = new List<string>();
        public static List<string> BaseList = new List<string>();

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, bool allowShifts, string outputDirectory, int minResultSize, int maxResultSize, bool writeSteps)
        {
            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            EndIteration = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            AllowShift = allowShifts;
            OutputDirectory = outputDirectory;
            WriteSteps = writeSteps;
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
            for (int iteration = 0; iteration < EndIteration; iteration++)
            {
                Init(faker);
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]

                int leftModificationCount = 0;
                int rightModificationCount = 0;

                Stack<Step> steps = new Stack<Step>();
                int elementCount = ResultList.Count;

                // Akcie sa vyberu a rovno vykonaju
                for (int i = 0; i < elementCount; i++)
                {
                    var pathToSteps = Path.Combine(OutputDirectory, iteration.ToString(), "steps");
                    var pathToStepsL = Path.Combine(pathToSteps, "left");
                    var pathToStepsR = Path.Combine(pathToSteps, "right");
                    var pathToStepsB = Path.Combine(pathToSteps, "base");

                    string leftStepName = "left_step" + i;
                    string rightStepName = "right_step" + i;
                    string baseStepName = "base_step" + i;

                    var item = ResultList[i];
                    int remainingPositions = elementCount - i;

                    (ElementAction, ElementAction) leftAndRightAction = ChooseLeftRightAction(leftModificationCount, rightModificationCount, leftKeepProbability, remainingPositions, item);
                    ElementAction leftAct = leftAndRightAction.Item1;
                    ElementAction rightAct = leftAndRightAction.Item2;

                    if (leftAct == ElementAction.KEEP && rightAct == ElementAction.KEEP)
                    {
                        ChangeLogText += "L, R, B:\n";
                        // či dám left/right nezalezi
                        ExecuteAction(RightList, BaseList, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == ElementAction.KEEP)
                    {
                        ChangeLogText += "R, B:\n";
                        ExecuteAction(RightList, BaseList, item, rightAct, faker, iteration);
                        rightModificationCount++;
                        if (WriteSteps)
                        {
                            steps.Push(new Step(rightStepName, RightList, pathToStepsR));
                            steps.Push(new Step(baseStepName, BaseList, pathToStepsB));
                        }
                    }
                    else if (rightAct == ElementAction.KEEP)
                    {
                        ChangeLogText += "L, B:\n";
                        ExecuteAction(LeftList, BaseList, item, leftAct, faker, iteration);
                        leftModificationCount++;
                        if (WriteSteps)
                        {
                            steps.Push(new Step(leftStepName, LeftList, pathToStepsL));
                            steps.Push(new Step(baseStepName, BaseList, pathToStepsB));
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

                XMLOutput.Export(LeftList, "left", iteration, OutputDirectory);
                XMLOutput.Export(RightList, "right", iteration, OutputDirectory);
                XMLOutput.Export(BaseList, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultList, "expectedResult", iteration, OutputDirectory);
                
                // Export changelogu do txt
                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                string head = SharedMethods.GetHeadForChangeLog(testingOneActionTwice: TestingOneActionTwice(),
                                                                leftKeepProbability: leftKeepProbability,
                                                                iteration: iteration,
                                                                leftModsCount: leftModificationCount, rightModsCount: rightModificationCount,
                                                                allowAdd: AllowAdd, allowRemove: AllowRemove, allowShift: AllowShift,
                                                                madeAdditions: MadeAdditions, madeRemovals: MadeRemovals, madeShifts: MadeShifts,
                                                                maxAllowedAdditions: MaxAllowedAdditions, maxAllowedRemovals: MaxAllowedRemovals, maxAllowedShifts: MaxAllowedShifts);
                ChangeLogText = head + ChangeLogText;
                File.WriteAllText(Path.Combine(iterationDir, $"changeLog{iteration}.txt"), ChangeLogText, Encoding.UTF8);
            }
        }

        private static (ElementAction, ElementAction) ChooseLeftRightAction(int leftModificationCount, int rightModificationCount, double leftKeepProbability, int remainingPositions, string item) {
            ElementAction leftAct, rightAct;

            if (SharedMethods.NextModificationIsOnLeft(TestingOneActionTwice(), leftModificationCount, rightModificationCount, leftKeepProbability))
            {
                leftAct = ChooseAction(LeftList, remainingPositions, item);
                rightAct = ElementAction.KEEP;
            }
            else
            {
                leftAct = ElementAction.KEEP;
                rightAct = ChooseAction(RightList, remainingPositions, item);
            }
            return (leftAct, rightAct);
        }

        private static void Init(Faker faker)
        {
            // Inicializácia
            MadeAdditions = 0;
            MadeRemovals = 0;
            MadeShifts = 0;

            ChangeLogText = string.Empty;
            UsedShiftItems.Clear();

            int elementCount = Random.Shared.Next(MinResultSize, MaxResultSize + 1);

            ResultList = CreateStartingList(faker, elementCount);
            RightList = new List<string>(ResultList);
            LeftList = new List<string>(ResultList);
            BaseList = new List<string>(ResultList);
            RightList = new List<string>(ResultList);
        }

        private static List<string> CreateStartingList(Faker faker, int elementCount)
        {
            var list = new List<string>();

            while (list.Count < elementCount)
            {
                string value = faker.Random.Word();
                if (!list.Contains(value))
                {
                    list.Add(value);
                }
            }
            return list;
        }

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, ElementAction action, Faker faker, int iteration)
        {
            if (action == ElementAction.KEEP)
            {
                ChangeLogText += $"Keeping item: '{item}'\n";
            }
            else if (action == ElementAction.REMOVAL)
            {
                int baseIndex = baseList.IndexOf(item);
                int branchIndex = branchList.IndexOf(item);

                LastElementWasRemovedFromPosition = true;
                ChangeLogText += $"Removing item: '{item}' at base index: '{baseIndex}' and branch index: '{branchIndex}'\n";

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ElementAction.ADDITION)
            {
                string newItem = SharedMethods.GetNewUniqueWord(faker, BaseList, LeftList, RightList, ResultList);

                int branchIndex = branchList.IndexOf(item);
                int baseIndex = baseList.IndexOf(item);

                baseList.Insert(baseIndex, newItem);
                branchList.Insert(branchIndex, newItem);

                ChangeLogText += $"Adding item: '{newItem}' at base index: '{baseIndex}' and branch index: '{branchIndex} behind element '{item}'\n";

                MadeAdditions++;
            }
            else if (action == ElementAction.SHIFT)
            {
                UsedShiftItems.Add(item);
                int elementAtBaseIndex = baseList.IndexOf(item);

                // výmena (swap) susediacich elementov spolu s dalším shiftom môže spôsobiť poradie elementov ktoré sa nedá nájť deteminicky
                if (!TestingOneActionOnce())
                {
                    if (elementAtBaseIndex > 0)
                    {
                        UsedShiftItems.Add(baseList[elementAtBaseIndex - 1]);
                    }
                    if (elementAtBaseIndex < baseList.Count - 1)
                    {
                        UsedShiftItems.Add(baseList[elementAtBaseIndex + 1]);
                    }
                }
                // ziskam elementy ktore su v base aj branch a neboli pouzite pri inom shifte
                List<string> baseListExceptDangerousTargets = new List<string>(baseList).Except(UsedShiftItems).ToList();
                List<string> elementsThatArentInBranch = baseListExceptDangerousTargets.Except(branchList).ToList();
                baseListExceptDangerousTargets = baseListExceptDangerousTargets.Except(elementsThatArentInBranch).ToList();

                var count = baseListExceptDangerousTargets.Count();
                if (count == 0) throw new InvalidOperationException("Žiadne elemnty neboli nájdené ako target");
                var targetItem = baseListExceptDangerousTargets[Random.Shared.Next(count)];

                int oldBaseIndex = baseList.IndexOf(item);
                int oldBranchIndex = branchList.IndexOf(item);

                UsedShiftItems.Add(targetItem);

                baseList.Remove(item);
                branchList.Remove(item);

                //uložíme item za targetItem
                int baseIndex = baseList.IndexOf(targetItem) + 1;
                int branchIndex = branchList.IndexOf(targetItem) + 1;

                baseList.Insert(baseIndex, item);
                branchList.Insert(branchIndex, item);

                var itemBeforeBase = baseList[baseIndex - 1];
                var itemBeforeBranch = branchList[branchIndex - 1];

                ChangeLogText += $"Shifting element: '{item}' from index Base:'{oldBaseIndex}', Branch:'{oldBranchIndex}' to 'Base:'{baseIndex}', Branch:'{branchIndex}' behind element Base:'{itemBeforeBase}'\n";
                MadeShifts++;

                // dalsí element sa nezmie modifikovať, lebo nastane rovnaký problém ako keď odstránime 2 prvky idúce za sebou vo vedlajsich vetviach (nedeterministicke poradie)
                LastElementWasRemovedFromPosition = true;
            }
        }

        private static ElementAction ChooseAction(List<string> branchList, int remainingPositions, string actualElement)
        {
            int remAdd = GetRemainingNumberOfUsesOfAction(ElementAction.ADDITION);
            int remShift = GetRemainingNumberOfUsesOfAction(ElementAction.SHIFT);
            int remRem = GetRemainingNumberOfUsesOfAction(ElementAction.REMOVAL);
            int remainingMods = remAdd + remShift + remRem;

            if ((remainingMods == 2 && remainingPositions <= 2) || (remainingMods == 1 && remainingPositions <= 1))
            {
                var forced = new List<ElementAction>();
                if (remShift > 0 && CanBeActionExecuted(ElementAction.SHIFT, actualElement, BaseList, branchList)) forced.Add(ElementAction.SHIFT);
                if (remRem > 0 && CanBeActionExecuted(ElementAction.REMOVAL, actualElement, BaseList, branchList)) forced.Add(ElementAction.REMOVAL);
                if (remAdd > 0 && CanBeActionExecuted(ElementAction.ADDITION, actualElement, BaseList, branchList)) forced.Add(ElementAction.ADDITION);

                if (forced.Count > 0)
                {
                    LastElementWasRemovedFromPosition = false;
                    return forced[Random.Shared.Next(forced.Count)];
                }
            }
            
            if (ShouldNextActionBeKeep(remainingPositions))
            {
                LastElementWasRemovedFromPosition = false;
                return ElementAction.KEEP;
            }

            var allowed = new List<ElementAction> { };

            if (CanBeActionExecuted(ElementAction.SHIFT, actualElement, BaseList, branchList))
            {
                allowed.Add(ElementAction.SHIFT);
            }
            if (CanBeActionExecuted(ElementAction.REMOVAL, actualElement, BaseList, branchList))
            {
                allowed.Add(ElementAction.REMOVAL);
            }
            if (CanBeActionExecuted(ElementAction.ADDITION, actualElement, BaseList, branchList))
            {
                allowed.Add(ElementAction.ADDITION);
            }

            LastElementWasRemovedFromPosition = false;
            return allowed.Count > 0 ? allowed[Random.Shared.Next(allowed.Count)] : ElementAction.KEEP;
        }

        private static int GetRemainingNumberOfUsesOfAction(ElementAction action)
        {
            return action switch
            {
                ElementAction.ADDITION => AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0,
                ElementAction.REMOVAL => AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0,
                ElementAction.SHIFT => AllowShift ? MaxAllowedShifts - MadeShifts : 0,
                ElementAction.KEEP => int.MaxValue, // KEEP nie je obmedzený
                _ => throw new InvalidOperationException($"Unexpected action: {action}")
            };
        }

        private static bool CanBeActionExecuted(ElementAction action, string actualElement, List<string> baseList, List<string> branchList)
        {
            if (GetRemainingNumberOfUsesOfAction(action) == 0) return false;

            if (UsedShiftItems.Contains(actualElement)) return false; // shift tu pridal prvok

            if (action == ElementAction.REMOVAL)
            {
                if (LastElementWasRemovedFromPosition)
                {
                    return false;
                }
                if (UsedShiftItems.Contains(actualElement)) return false;
            }

            else if (action == ElementAction.SHIFT)
            {
                if (LastElementWasRemovedFromPosition)
                {
                    return false;
                }
                if (UsedShiftItems.Contains(actualElement)) return false;                
                if (ResultList.Count - UsedShiftItems.Distinct().Count() <= 3) return false;
            }
            return true; // add, keep
        }

        private static bool ShouldNextActionBeKeep(int remainingPositions)
        {
            int remaningAdd = GetRemainingNumberOfUsesOfAction(ElementAction.ADDITION);
            int remaningShift = GetRemainingNumberOfUsesOfAction(ElementAction.SHIFT);
            int remaningRemove = GetRemainingNumberOfUsesOfAction(ElementAction.REMOVAL);

            int remainingModifications = remaningAdd + remaningShift + remaningRemove;

            int madeMofifications = MadeAdditions + MadeRemovals + MadeShifts;
            int evenChance = Math.Max(1, remainingPositions);

            // jedna modifikacia
            if (TestingOneActionOnce())
            {
                if (remainingPositions == 1 && remainingModifications == 1)
                {
                    return false;
                }
                return Random.Shared.Next(evenChance) != 0;            

            }
            // dve modifikacie
            else if (TestingOneActionTwice())
            {

                if (AllowRemove && remainingPositions <= 3 && remainingModifications != 0)
                {
                    return false;
                }
                if (AllowShift && remainingPositions < ResultList.Count / 3 && remainingModifications != 1)
                {
                    return false;
                }
                if (AllowShift && remainingPositions < ResultList.Count - 2 && remainingModifications != 0)
                {
                    return false;
                }
                if (AllowAdd && remainingPositions <= 2 && remainingModifications != 0)
                {
                    return false;
                }
                return Random.Shared.Next(evenChance) != 0;
            }

            // normalny
            var allowed = new List<ElementAction> { ElementAction.KEEP };
            if (remaningAdd > 0)
            {
                allowed.Add(ElementAction.ADDITION);
            }
            if (remaningRemove > 0)
            {
                allowed.Add(ElementAction.REMOVAL);
            }
            if (remaningShift > 0)
            {
                allowed.Add(ElementAction.SHIFT);
            }

            return allowed[Random.Shared.Next(allowed.Count)] == ElementAction.KEEP;
        }

        private static bool TestingOneActionOnce()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 1;
        }

        private static bool TestingOneActionTwice()
        {
            int allowed = 0;
            if (AllowAdd) allowed++;
            if (AllowRemove) allowed++;
            if (AllowShift) allowed++;

            if (allowed != 1) return false;

            return MaxActionsSum() == 2;
        }

        private static long MaxActionsSum()
        {
            long allowedNumberOfActions = 0;
            allowedNumberOfActions += AllowAdd ? MaxAllowedAdditions : 0;
            allowedNumberOfActions += AllowShift ? MaxAllowedShifts : 0;
            allowedNumberOfActions += AllowRemove ? MaxAllowedRemovals : 0;
            return allowedNumberOfActions;
        }
    }
}