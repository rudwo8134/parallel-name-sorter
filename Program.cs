using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ParallelNameSort
{
    class Name
    {
        public Name(string fname, string lname)
        {
            FirstName = fname;
            LastName = lname;
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            List<Name> names = new List<Name>();
            List<Name> namesCopy = new List<Name>();  
            
            using (StreamReader sr = new StreamReader("random names.txt"))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] s = line.Split(' ');
                    if (s.Length >= 2)
                    {
                        var name = new Name(s[0], s[1]);
                        names.Add(name);
                        namesCopy.Add(name);
                    }
                }
            }

            Console.WriteLine($"Total names to sort: {names.Count}");
            Console.WriteLine("\nParallel Sorting...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            var parallelSortedNames = ParallelMergeSort(names);
            stopwatch.Stop();
            Console.WriteLine($"Parallel sort took {stopwatch.ElapsedMilliseconds} milliseconds");

            Console.WriteLine("\nBuilt-in Sorting...");
            stopwatch.Restart();
            var builtInSortedNames = namesCopy.OrderBy(n => n.LastName).ThenBy(n => n.FirstName).ToList();
            stopwatch.Stop();
            Console.WriteLine($"Built-in sort took {stopwatch.ElapsedMilliseconds} milliseconds");

            Console.WriteLine("\nFirst 10 names from Parallel Sort:");
            foreach (var name in parallelSortedNames.Take(10))
            {
                Console.WriteLine($"{name.LastName}, {name.FirstName}");
            }

            Console.WriteLine("\nFirst 10 names from Built-in Sort:");
            foreach (var name in builtInSortedNames.Take(10))
            {
                Console.WriteLine($"{name.LastName}, {name.FirstName}");
            }

            bool areEqual = parallelSortedNames.Count == builtInSortedNames.Count &&
                           parallelSortedNames.Zip(builtInSortedNames, (p, b) => 
                               p.LastName == b.LastName && p.FirstName == b.FirstName)
                           .All(x => x);
            
            Console.WriteLine($"\nBoth sorts produced identical results: {areEqual}");

            Console.WriteLine("\nPress Enter to exit");
            Console.ReadLine();
        }

        static List<Name> ParallelMergeSort(List<Name> list)
        {
            if (list.Count <= 1) return list;

            int mid = list.Count / 2;
            var left = list.Take(mid).ToList();
            var right = list.Skip(mid).ToList();

            if (list.Count > 1000)
            {
                Parallel.Invoke(
                    () => left = ParallelMergeSort(left),
                    () => right = ParallelMergeSort(right)
                );
            }
            else
            {
                left = ParallelMergeSort(left);
                right = ParallelMergeSort(right);
            }

            return Merge(left, right);
        }

        static List<Name> Merge(List<Name> left, List<Name> right)
        {
            var result = new List<Name>();
            int leftIndex = 0, rightIndex = 0;

            while (leftIndex < left.Count && rightIndex < right.Count)
            {
                int compareResult = string.Compare(left[leftIndex].LastName, right[rightIndex].LastName);
                if (compareResult == 0)
                {
                    compareResult = string.Compare(left[leftIndex].FirstName, right[rightIndex].FirstName);
                }

                if (compareResult <= 0)
                {
                    result.Add(left[leftIndex]);
                    leftIndex++;
                }
                else
                {
                    result.Add(right[rightIndex]);
                    rightIndex++;
                }
            }

            while (leftIndex < left.Count)
            {
                result.Add(left[leftIndex]);
                leftIndex++;
            }

            while (rightIndex < right.Count)
            {
                result.Add(right[rightIndex]);
                rightIndex++;
            }

            return result;
        }
    }
}
