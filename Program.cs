using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParallelNameSort
{
      class SystemInfo
    {
        public static int GetCoreCount()
        {
            // Check the CPU core count to use parallel threads and improve performance with correct number of threads
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

            Console.WriteLine("Press Return to exit");
            Console.ReadLine();
        }
    }
    
}
