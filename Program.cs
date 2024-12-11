using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace ParallelNameSort
{
      class SystemInfo
    {
        public static int GetCoreCount()
        {
            // Check the only locgical cores count to use parallel threads and improve performance with correct number of threads
            // TPL uses .NET's Thread Pool, which creates and distributes tasks based on the number of logical cores.
            // That's why we count the logical core count to optimize parallel processing. <Checked>
            return Environment.ProcessorCount;
        }

        public static int PrintCoreCount()
        {
            int coreCount = GetCoreCount();
            // Print the result
            Console.WriteLine($"Checking your computer's core count. Your core count is: {coreCount}");
            // Return the core count to optimize thread count for best performance
            // This will be used to parallelize the sorting process efficiently
            // Core count == multi thread count 
            return coreCount;
        }
    }

    public class NameMethod
    {
        public string FirstName { get; }
        public string LastName { get; }

        public NameMethod(string firstName, string lastName)
        {
            // Check if the first name or last name is empty and set to "Unknown" if true
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "Unknown" : firstName;
            LastName = string.IsNullOrWhiteSpace(lastName) ? "Unknown" : lastName;
        }

        public override string ToString() => $"{FirstName} {LastName}";
    }

    public class ParallelNameSorter
    {
        // Thread-related settings
        private readonly int maxThreadCount;    // Maximum number of threads to use
        private int activeThreadCount;          // Number of currently active threads

        // We will optimize performance by using threads equal to the user's core count.
        // Since users have different numbers of cores, we'll check the core count to optimize accordingly.

        
        // core 수를 체크해서 최적화 시킬것이다.
        public ParallelNameSorter(int numberOfThreads)
        {
            maxThreadCount = numberOfThreads;
            activeThreadCount = 0;
        }

        
        // Sort starting point
        // This method is the entry point for parallel name sorting
        public List<NameMethod> SortNames(List<NameMethod> unsortedNames)
        {
            // Return empty list if input is null or empty
            if (unsortedNames == null || unsortedNames.Count == 0)
            {
                return new List<NameMethod>();
            }

            // Reset thread counter before starting sort
            activeThreadCount = 0;
            // Start the parallel sorting process <Starting point>
            return StartParallelSort(unsortedNames);
        }

        private List<NameMethod> StartParallelSort(List<NameMethod> names)
        {
            // Base case: if list has 1 or fewer elements, no need to sort
            if (names.Count <= 1) 
            {
                return names;
            }

            // Split the list into two halves
            var (leftHalf, rightHalf) = SplitList(names);

            // If we can create new threads, use parallel processing
            if (CanCreateNewThread())
            {
                return SortWithNewThreads(leftHalf, rightHalf);
            }
            // Otherwise, fall back to sequential processing
            else
            {
                return SortSequentially(leftHalf, rightHalf);
            }
        }

        // Helper method to split the list into two halves
        private (List<NameMethod> left, List<NameMethod> right) SplitList(List<NameMethod> names)
        {
            int middlePoint = names.Count / 2;
            var leftHalf = names.GetRange(0, middlePoint);
            var rightHalf = names.GetRange(middlePoint, names.Count - middlePoint);
            return (leftHalf, rightHalf);
        }

        // Check if we can create a new thread based on active thread count
        private bool CanCreateNewThread()
        {
            return activeThreadCount < maxThreadCount;
        }

        // Sort using new threads for parallel processing
        private List<NameMethod> SortWithNewThreads(List<NameMethod> left, List<NameMethod> right)
        {
            // Sort left half in new thread
            IncreaseThreadCount();
            var leftTask = Task.Run(() => 
            {
                var result = StartParallelSort(left);
                DecreaseThreadCount();
                return result;
            });

            // Sort right half in new thread
            IncreaseThreadCount();
            var rightTask = Task.Run(() => 
            {
                var result = StartParallelSort(right);
                DecreaseThreadCount();
                return result;
            });

            // Wait for both sorting tasks to complete
            var sortedLeft = leftTask.Result;
            var sortedRight = rightTask.Result;

            // Merge the sorted halves
            return MergeSortedLists(sortedLeft, sortedRight);
        }

        // Sort sequentially when no more threads can be created
        private List<NameMethod> SortSequentially(List<NameMethod> left, List<NameMethod> right)
        {
            var sortedLeft = StartParallelSort(left);
            var sortedRight = StartParallelSort(right);
            return MergeSortedLists(sortedLeft, sortedRight);
        }

        // Thread count management using atomic operations
        private void IncreaseThreadCount() => Interlocked.Increment(ref activeThreadCount);
        private void DecreaseThreadCount() => Interlocked.Decrement(ref activeThreadCount);

        // Merge two sorted lists while maintaining sort order
        private List<NameMethod> MergeSortedLists(List<NameMethod> left, List<NameMethod> right)
        {
            var mergedList = new List<NameMethod>();
            int leftIndex = 0, rightIndex = 0;

            // Compare and merge elements from both lists in order
            while (leftIndex < left.Count && rightIndex < right.Count)
            {
                if (ShouldTakeFromLeft(left[leftIndex], right[rightIndex]))
                {
                    mergedList.Add(left[leftIndex]);
                    leftIndex++;
                }
                else
                {
                    mergedList.Add(right[rightIndex]);
                    rightIndex++;
                }
            }

            // Add any remaining elements
            AddRemainingItems(mergedList, left, leftIndex);
            AddRemainingItems(mergedList, right, rightIndex);

            return mergedList;
        }

        // Helper method to add remaining items from a source list
        private void AddRemainingItems(List<NameMethod> result, List<NameMethod> source, int startIndex)
        {
            result.AddRange(source.Skip(startIndex));
        }

        // Compare two names for sorting order
        private bool ShouldTakeFromLeft(NameMethod left, NameMethod right)
        {
            // First compare by LastName
            int lastNameComparison = string.Compare(left.LastName, right.LastName, StringComparison.Ordinal);
            
            // If LastNames are equal, compare by FirstName
            if (lastNameComparison == 0)
            {
                return string.Compare(left.FirstName, right.FirstName, StringComparison.Ordinal) <= 0;
            }

            return lastNameComparison < 0;
        }
    }

   

    class Program
    {
        static void Main(string[] args)
        {
          
           int cores = SystemInfo.PrintCoreCount();
           List<NameMethod> validNames = new List<NameMethod>();
           // Names without first name or last name are moved to unsortedNames list
           // This separation ensures the sorting process continues to work properly
           // <Note> This is not a project requirement but added as a precautionary measure
           // When dealing with large datasets, manual verification of each entry becomes impractical
           // This ensures the program continues running even if some data entries are invalid
           List<NameMethod> unsortedNames = new List<NameMethod>();


            // Step 1: Read names from file => Multi-threading is not used here because the project excludes file reading from time measurement
            using (StreamReader sr = new StreamReader("random names.txt"))
            {
                while (sr!.Peek() >= 0)
                {
                    string[] nameParts = sr!.ReadLine()!.Split(' ');
                    var firstName = nameParts[0].Trim();
                    var lastName = nameParts[1].Trim();
                    
                    var nameEntry = new NameMethod(firstName, lastName);
                    // Check if the first name or last name is empty and set to "Unknown" if true from NameMethod class
                    if (nameEntry.FirstName == "Unknown" || nameEntry.LastName == "Unknown")
                    {
                        // Add the name to the unsortedNames list
                        // We will skip the name sort process for these names since they are not valid
                        unsortedNames.Add(nameEntry);
                    }
                    else
                    {
                        validNames.Add(nameEntry);
                    }
                }
            }
            // Starting point of sorting
            Console.WriteLine("Sorting...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            // Step 2: Sort the names
            var sorter = new ParallelNameSorter(cores);
            validNames = sorter.SortNames(validNames);


            // End point of sorting
            stopwatch.Stop();
             Console.WriteLine("Sorting finished...");
             Console.WriteLine("Code took {0} milliseconds ({1:F6} seconds) to execute",
                stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000.0);

             Console.WriteLine($"Number of valid names: {validNames.Count}");
             // Print the unsortedNames list
             Console.WriteLine($"Number of unsorted names: {unsortedNames.Count}");
             // Print total number of names (valid + unsorted)
             Console.WriteLine($"Total number of names: {validNames.Count + unsortedNames.Count}");
        
             // Example sorting method (using LINQ OrderBy)
             Console.WriteLine("\nExample sorting method (using LINQ OrderBy):");
             Stopwatch stopwatch2 = Stopwatch.StartNew();
             List<NameMethod> sortedNames = validNames.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
             stopwatch2.Stop();
             Console.WriteLine("Example sorting took {0} milliseconds ({1:F6} seconds) to execute",
                 stopwatch2.ElapsedMilliseconds,
                 stopwatch2.ElapsedMilliseconds / 1000.0);

            // Compare sorting times
            if (stopwatch.ElapsedMilliseconds < stopwatch2.ElapsedMilliseconds)
            {
                Console.WriteLine("\nCustom sorting was faster by {0} milliseconds",
                    stopwatch2.ElapsedMilliseconds - stopwatch.ElapsedMilliseconds);
            }
            else if (stopwatch.ElapsedMilliseconds > stopwatch2.ElapsedMilliseconds)
            {
                Console.WriteLine("\nLINQ sorting was faster by {0} milliseconds",
                    stopwatch.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds);
            }
            else
            {
                Console.WriteLine("\nBoth sorting methods took the same time");
            }

            // Compare sorting results
            Console.WriteLine("\nComparing first 10 items:");
            Console.WriteLine("Custom Sort\t\tLINQ Sort");
            Console.WriteLine("----------------------------------------");
            
            bool isMatchingSort = true;
            for (int i = 0; i < Math.Min(10, validNames.Count); i++)
            {
                if (validNames[i].ToString() != sortedNames[i].ToString())
                {
                    isMatchingSort = false;
                }
                Console.WriteLine($"{validNames[i]}\t\t{sortedNames[i]}");
            }

            Console.WriteLine("\nSorting results match: " + (isMatchingSort ? "Yes" : "No"));

            Console.WriteLine("Press Return to exit");
        }
    }
    
}
