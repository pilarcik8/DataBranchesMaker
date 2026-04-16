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
    public static class ListMaker
    {
        private enum ListAction
        {
            KEEP,
            REMOVE,
            ADD,
            SHIFT
        }

        // Kedže negenerujeme sekvenciu akcií, ak kroky tvorime počas prococesu modifikácií, ak budeme musieť opakovat iteraciu, kroky sa nám nezamiesajú so starímy
        private class Step
        {
            public string name { get; }
            public List<string> stateOfListInCurrentIter { get; }
            public string pathToExport { get; }

            public Step(string name, List<string> listOfState, string pathToExport)
            {
                this.name = name;
                this.stateOfListInCurrentIter = new List<string>(listOfState);
                this.pathToExport = pathToExport;
            }
        }

        // Počet vykonaných akcií v iterácii, resetuje sa na začiatku každej iterácie
        private static int MadeRemovals = 0;
        private static int MadeAdditions = 0;
        private static int MadeShifts = 0;

        private static int EndIteration { get; set; } = 5;
        // Povolenie jednotlivých akcií
        private static bool AllowRemove { get; set; } = true;
        private static bool AllowAdd { get; set; } = true;
        private static bool AllowShift { get; set; } = true;
        // Maximálny počet povolených akcií (globálne pre všetky iterácie)
        private static int MaxAllowedRemovals { get; set; } = int.MaxValue;
        private static int MaxAllowedAdditions { get; set; } = int.MaxValue;
        private static int MaxAllowedShifts { get; set; } = int.MaxValue;

        // Veľkosť očakávaného výsledku (originálneho xml)
        private static int MaxResultSize { get; set; } = 10;
        private static int MinResultSize { get; set; } = 10;
        private static string OutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ListMakerOutput");
        private static string ChangeLogText = "";
        private static bool WriteSteps { get; set; } = false;

        // pri odstraneny 2 za sebou iducich elementov merger nevie presne poradie
        private static bool LastElementWasRemovedFromPosition = false;

        private static HashSet<string> NotUsebleShiftTargets = new HashSet<string>();

        private static List<string> ResultList = new List<string>();
        private static List<string> RightList = new List<string>();
        private static List<string> LeftList = new List<string>();
        private static List<string> BaseList = new List<string>();
        private static bool TestingOneActionTwice = false;
        private static bool TestingOneActionOnce = false;

        public static void SetParameters(int numberIterations, bool removingAllowed, bool addingAllowed, bool shiftingAllowed, string outputDirectory, int minResultSize, int maxResultSize, bool writeSteps)
        {
            if (numberIterations <= 0)
            {
                throw new Exception("Nízka hodnota iterácí");
            }
            if (!removingAllowed && !addingAllowed && !shiftingAllowed)
            {
                throw new Exception("Žiadna operácia nie je povolená");
            }

            if (maxResultSize <= 0 || minResultSize <= 0)
            {
                throw new Exception("Nízka hodnota veľkosti výsledku");
            }

            if (maxResultSize < minResultSize)
            {
                throw new Exception("Maximálna veľkosť výsledku musí být väčšia alebo rovná ako minimálna velikosť výsledku");
            }

            MinResultSize = minResultSize;
            MaxResultSize = maxResultSize;
            EndIteration = numberIterations;
            AllowRemove = removingAllowed;
            AllowAdd = addingAllowed;
            AllowShift = shiftingAllowed;
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
            int nunOfAllowedActions = SharedMethods.GetNumberOfAllowedActions(isAllowedAdd: AllowAdd, isAllowedRemove: AllowRemove, isAllowedShift: AllowShift);
            long numbOfMaxMods = SharedMethods.GetMaxActionsSum(isAllowedAdd: AllowAdd, isAllowedRemove: AllowRemove, isAllowedShift: AllowShift,
                                                                maxAdd: MaxAllowedAdditions, maxRem: MaxAllowedRemovals, maxShift: MaxAllowedShifts);
            TestingOneActionOnce = SharedMethods.LearnIfCurrentlyTestingOneActionOnce(nunOfAllowedActions, numbOfMaxMods);
            if (numbOfMaxMods <= 0)
            {
                throw new Exception("Maximálny počet modifikácií musí být väčší ako 0");
            }

            for (int iteration = 0; iteration < EndIteration; iteration++)
            {
                Init(faker);
                double leftKeepProbability = Random.Shared.NextDouble() * 0.6 + 0.2; // [0.2, 0.8]
                int elementCount = ResultList.Count;

                TestingOneActionTwice = SharedMethods.LearnIfCurrentlyTestingOneActionTwice(elementCount, nunOfAllowedActions, numbOfMaxMods);

                int leftModificationCount = 0;
                int rightModificationCount = 0;

                Stack<Step> steps = new Stack<Step>();

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

                    (ListAction, ListAction) leftAndRightAction = ChooseLeftRightAction(leftModificationCount, rightModificationCount, leftKeepProbability, remainingPositions, item);
                    ListAction leftAct = leftAndRightAction.Item1;
                    ListAction rightAct = leftAndRightAction.Item2;

                    if (leftAct == ListAction.KEEP && rightAct == ListAction.KEEP)
                    {
                        ChangeLogText += "L, R, B:\n";
                        // či dám left/right nezalezi
                        ExecuteAction(RightList, BaseList, item, rightAct, faker, iteration);
                    }
                    else if (leftAct == ListAction.KEEP)
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
                    else if (rightAct == ListAction.KEEP)
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
                if (!SharedMethods.IsValidXmlOuput(TestingOneActionOnce, leftModificationCount, rightModificationCount))
                {
                    steps.Clear();
                    iteration--;
                    continue;
                }

                // Export krokov - musi byt po ujisteni 3way merge, inak kroky stare + nove sa zmiesaju
                while (steps.Count > 0)
                {
                    var step = steps.Pop();
                    XMLOutput.Export(step.stateOfListInCurrentIter, step.name, null, step.pathToExport);
                }

                XMLOutput.Export(LeftList, "left", iteration, OutputDirectory);
                XMLOutput.Export(RightList, "right", iteration, OutputDirectory);
                XMLOutput.Export(BaseList, "base", iteration, OutputDirectory);
                XMLOutput.Export(ResultList, "expectedResult", iteration, OutputDirectory);
                
                // Export changelogu do txt
                string iterationDir = Path.Combine(OutputDirectory, iteration.ToString());
                Directory.CreateDirectory(iterationDir);
                string head = SharedMethods.GetHeadForChangeLog(
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

        private static (ListAction, ListAction) ChooseLeftRightAction(int leftModificationCount, int rightModificationCount, double leftKeepProbability, int remainingPositions, string item) {
            ListAction leftAct, rightAct;

            if (SharedMethods.ShouldNextModificationBeOnLeft(TestingOneActionTwice, leftModificationCount, rightModificationCount, leftKeepProbability))
            {
                leftAct = ChooseAction(LeftList, remainingPositions, item);
                rightAct = ListAction.KEEP;
            }
            else
            {
                leftAct = ListAction.KEEP;
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
            NotUsebleShiftTargets.Clear();

            LastElementWasRemovedFromPosition = false;

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

        private static void ExecuteAction(List<string> branchList, List<string> baseList, string item, ListAction action, Faker faker, int iteration)
        {
            if (action == ListAction.KEEP)
            {
                ChangeLogText += $"Keeping item: '{item}'\n";
            }
            else if (action == ListAction.REMOVE)
            {
                if (AllowShift)
                {
                    NotUsebleShiftTargets.Add(item); // nechceme aby niekto mal target kde je odstraneny element
                }
                int baseIndex = baseList.IndexOf(item);
                int branchIndex = branchList.IndexOf(item);

                LastElementWasRemovedFromPosition = true;
                ChangeLogText += $"Removing item: '{item}' at base index: '{baseIndex}' and branch index: '{branchIndex}'\n";

                baseList.Remove(item);
                branchList.Remove(item);
                MadeRemovals++;
            }
            else if (action == ListAction.ADD)
            {
                if (AllowShift)
                {
                    NotUsebleShiftTargets.Add(item); // nechceme aby niekto mal target kde je add
                }
                string newItem = SharedMethods.GetNewUniqueWord(faker, BaseList, LeftList, RightList, ResultList);

                int branchIndex = branchList.IndexOf(item);
                int baseIndex = baseList.IndexOf(item);

                baseList.Insert(baseIndex, newItem);
                branchList.Insert(branchIndex, newItem);

                ChangeLogText += $"Adding item: '{newItem}' at base index: '{baseIndex}' and branch index: '{branchIndex} behind element '{item}'\n";

                MadeAdditions++;
            }
            else if (action == ListAction.SHIFT)
            {
                NotUsebleShiftTargets.Add(item); // zdroj                

                // zakazeme predchadzajuci element, lebo ak ho vyberieme ako target, neposunieme item
                // takziez tymto zabranime swapu keby dva shifty mali target druheho source - vtedy by mohol nastat false conflict
                int indexElementAtResult = ResultList.IndexOf(item);
                if (indexElementAtResult > 0)
                {
                    var previousElement = ResultList[indexElementAtResult - 1];
                    NotUsebleShiftTargets.Add(previousElement);
                }

                List<string> exceptDangerousTargets = ResultList.Except(NotUsebleShiftTargets).ToList();

                var count = exceptDangerousTargets.Count();
                if (count == 0) throw new InvalidOperationException("Žiadne elemnty neboli nájdené ako target");
                var targetItem = exceptDangerousTargets[Random.Shared.Next(count)];
                
                int oldBaseIndex = baseList.IndexOf(item);
                int oldBranchIndex = branchList.IndexOf(item);

                NotUsebleShiftTargets.Add(targetItem); // target

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

        private static ListAction ChooseAction(List<string> branchList, int remainingPositions, string actualElement)
        {
            if (ShouldNextActionBeKeep())
            {
                LastElementWasRemovedFromPosition = false;
                return ListAction.KEEP;
            }

            var allowed = new List<ListAction> { };

            if (CanBeActionExecuted(ListAction.SHIFT, actualElement))
            {
                allowed.Add(ListAction.SHIFT);
            }
            if (CanBeActionExecuted(ListAction.REMOVE, actualElement))
            {
                allowed.Add(ListAction.REMOVE);
            }
            if (CanBeActionExecuted(ListAction.ADD, actualElement))
            {
                allowed.Add(ListAction.ADD);
            }

            LastElementWasRemovedFromPosition = false;
            return allowed.Count > 0 ? allowed[Random.Shared.Next(allowed.Count)] : ListAction.KEEP;
        }

        private static int GetRemainingNumberOfUsesOfAction(ListAction action)
        {
            return action switch
            {
                ListAction.ADD => AllowAdd ? MaxAllowedAdditions - MadeAdditions : 0,
                ListAction.REMOVE => AllowRemove ? MaxAllowedRemovals - MadeRemovals : 0,
                ListAction.SHIFT => AllowShift ? MaxAllowedShifts - MadeShifts : 0,
                ListAction.KEEP => int.MaxValue, // KEEP nie je obmedzený
                _ => throw new InvalidOperationException($"Unexpected action: {action}")
            };
        }

        private static bool CanBeActionExecuted(ListAction action, string actualElement)
        {
            if (GetRemainingNumberOfUsesOfAction(action) == 0) return false;

            // je to už shift target
            if (NotUsebleShiftTargets.Contains(actualElement)) return false; 

            if (action == ListAction.REMOVE)
            {
                if (LastElementWasRemovedFromPosition) return false;
            }
            else if (action == ListAction.SHIFT)
            {
                if (LastElementWasRemovedFromPosition) return false;

                // je miesto pre shift?
                // predcházdajúci element pred zdrojom + zdroj + target = 3
                if (ResultList.Except(NotUsebleShiftTargets).Count() < 3) return false;
            }
            return true; // add, keep
        }

        private static bool ShouldNextActionBeKeep()
        {
            int remaningAdd = GetRemainingNumberOfUsesOfAction(ListAction.ADD);
            int remaningShift = GetRemainingNumberOfUsesOfAction(ListAction.SHIFT);
            int remaningRemove = GetRemainingNumberOfUsesOfAction(ListAction.REMOVE);

            int evenChance = Math.Max(1, ResultList.Count);

            // nepekný fix aby pri 1 - 2 operaciach to bolo viacej roztiahnute
            if (TestingOneActionOnce || TestingOneActionTwice)
            {
                return Random.Shared.Next(evenChance) != 0;          
            }

            // normalny
            var allowed = new List<ListAction> { ListAction.KEEP };
            if (remaningAdd > 0)
            {
                allowed.Add(ListAction.ADD);
            }
            if (remaningRemove > 0)
            {
                allowed.Add(ListAction.REMOVE);
            }
            if (remaningShift > 0)
            {
                allowed.Add(ListAction.SHIFT);
            }

            return allowed[Random.Shared.Next(allowed.Count)] == ListAction.KEEP;
        }
    }
}