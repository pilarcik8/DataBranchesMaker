using Bogus;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class SharedMethods
    {
        public static bool IsValidOutput(bool testingOneActionOnce, int leftModificationCount, int rightModificationCount)
        {
            if (testingOneActionOnce)
            {
                return (leftModificationCount + rightModificationCount == 1);
            }
            return (leftModificationCount > 0 && rightModificationCount > 0);
        }

        public static bool NextModificationIsOnLeft(bool testingOneActionTwice, int leftModificationsCount, int rightModificationsCount, double leftKeepProbability)
        {
            // ak left keep prob je 0.7, tak 70% šanca že ďalšia modifikácia bude na pravej strane, a 30% šanca že bude na ľavej strane
            if (!testingOneActionTwice) return (Random.Shared.NextDouble() >= leftKeepProbability);

            if (leftModificationsCount == 0 && rightModificationsCount == 1) return true;
            else if (leftModificationsCount == 1 && rightModificationsCount == 0) return false;

            return (Random.Shared.NextDouble() >= leftKeepProbability);
        }

        public static string GetNewUniqueWord(Faker faker, List<string> baseList, List<string> leftList, List<string> rightList, List<string> resultList)
        {
            var word = faker.Random.Word();
            int loopCounter = 0;
            while (baseList.Contains(word) || leftList.Contains(word) || rightList.Contains(word) || resultList.Contains(word))
            {
                word = faker.Random.Word();
                if (loopCounter > 10)
                {
                    word += " " + faker.UniqueIndex;
                }
                loopCounter++;
            }
            return word;
        }

        public static string GetHeadForChangeLog(bool testingOneActionTwice, double leftKeepProbability, int iteration, 
            int leftModsCount, int rightModsCount, 
            bool allowAdd = false, bool allowRemove = false, bool allowChange = false, bool allowShift = false,
            int maxAllowedRemovals = 0, int maxAllowedChanges = 0, int maxAllowedAdditions = 0, int maxAllowedShifts = 0,
            int madeRemovals = 0, int madeChanges = 0, int madeAdditions = 0, int madeShifts = 0)
        {
            if (allowRemove && maxAllowedRemovals <= 0) throw new ArgumentException("Povolený Remove ale max menší alebo rovný ako nula");
            if (allowChange && maxAllowedChanges <= 0) throw new ArgumentException("Povolený Change ale max menší alebo rovný ako nula");
            if (allowAdd && maxAllowedAdditions <= 0) throw new ArgumentException("Povolený Add ale max menší alebo rovný ako nula");
            if (allowShift && maxAllowedShifts <= 0) throw new ArgumentException("Povolený Shift ale max menší alebo rovný ako nula");

            if (!allowAdd && !allowChange && !allowRemove && !allowShift) throw new ArgumentException("Aspoň jedna modifikácia musí byť povolená");

            string head = "Allowed Actions: ";
            if (allowRemove) head += "Remove ";
            if (allowAdd) head += "Add ";
            if (allowShift) head += "Shift ";
            if (allowChange) head += "Change ";

            head += "\n";

            head += "Max Allowed Actions: ";
            if (allowRemove) head += $" Remove: {maxAllowedRemovals} ";
            if (allowAdd) head += $"Add: {maxAllowedAdditions} ";
            if (allowShift) head += $"Shift: {maxAllowedShifts} ";
            if (allowChange) head += $"Change: {maxAllowedChanges} ";

            head += "\n";

            head += "Total modifications: ";
            if (allowRemove) head += $"Removals: {madeRemovals} ";
            if (allowAdd) head += $"Additions: {madeAdditions} ";
            if (allowShift) head += $"Shifts: {madeShifts} ";
            if (allowChange) head+= $"Change: {madeChanges} "; 

            head += "\n\n";

            if (testingOneActionTwice)
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
    }
}
