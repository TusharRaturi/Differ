using System.Diagnostics.CodeAnalysis;

namespace Differ
{
    internal class Differ
    {
        private List<FSStructure> targets;
        private List<Item> diffItems;

        public Differ(FSStructure target1, FSStructure target2)
        {
            targets = new() { target1, target2 };
            diffItems = new();
        }

        private void MarkAllAsDiffAt(int targetIdx, int level)
        {
            Level lvl = targets[targetIdx].Levels[level];
            diffItems.AddRange(lvl.Items.Values.ToList());
        }

        private static bool MatchItems(Item? i1, Item? i2)
        {
            if (i1 == null && i2 == null)
                return true;
            else if (i1 == null || i2 == null)
                return false;
            
            if (i1.ItemString != i2.ItemString) return false;

            return MatchItems(i1.Parent, i2.Parent);
        }

        private static bool MatchIfFiles([DisallowNull] Item fileItem1, [DisallowNull] Item fileItem2)
        {
            string path1 = fileItem1.FullPath();
            string path2 = fileItem2.FullPath();

            bool f1Exists = File.Exists(path1);
            bool f2Exists = File.Exists(path2);

            if (f1Exists && f2Exists)
            {
                string[] file1Lines = File.ReadAllLines(path1);
                string[] file2Lines = File.ReadAllLines(path2);

                if (file1Lines.Length != file2Lines.Length) return false;

                for (int i = 0; i < file1Lines.Length; i++)
                {
                    if (file1Lines[i] != file2Lines[i])
                        return false;
                }

                return true;
            }
            else if (!f1Exists && !f2Exists) return true;

            return false;
        }

        public void FindDiffs()
        {
            int t1Levels = targets[0].Levels.Count;
            int t2Levels = targets[1].Levels.Count;

            int maxLevels = Math.Min(t1Levels, t2Levels);

            for (int i = 0; i < maxLevels; i++)
            {
                Dictionary<string, Item> t1Items = targets[0].Levels[i].Items;
                Dictionary<string, Item> t2Items = targets[1].Levels[i].Items;

                Dictionary<string, List<string>> t1Dupes = targets[0].Levels[i].Duplicates;
                Dictionary<string, List<string>> t2Dupes = targets[1].Levels[i].Duplicates;

                HashSet<string> t1DupeValues = targets[0].Levels[i].DuplicateValues;
                HashSet<string> t2DupeValues = targets[1].Levels[i].DuplicateValues;

                Dictionary<string, Item> t1Et2 = t1Items.Where(kvp => !t2Items.ContainsKey(kvp.Key) && !t1DupeValues.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                Dictionary<string, Item> t2Et1 = t2Items.Where(kvp => !t1Items.ContainsKey(kvp.Key) && !t2DupeValues.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                List<Item> uncommonItems = t1Et2.Union(t2Et1).Select(kvp => kvp.Value).ToList();

                diffItems.AddRange(uncommonItems);

                List<string> commonKeys = t1Items.Where(kvp => t2Items.ContainsKey(kvp.Key)).Select(kvp =>
                {
                    return kvp.Key;
                }).ToList();

                for (int c = 0; c < commonKeys.Count; c++)
                {
                    string itemName = commonKeys[c];

                    List<string> t1DupesList, t2DupesList;

                    t1Dupes.TryGetValue(itemName, out List<string>? dupes1);
                    t2Dupes.TryGetValue(itemName, out List<string>? dupes2);

                    if (dupes1 != null)
                        t1DupesList = dupes1;
                    else t1DupesList = new();

                    if (dupes2 != null)
                        t2DupesList = dupes2;
                    else t2DupesList = new();

                    if (t1DupesList.Count == 0 && t2DupesList.Count == 0)
                    {
                        if (!MatchItems(t1Items[itemName].Parent, t2Items[itemName].Parent) || !MatchIfFiles(t1Items[itemName], t2Items[itemName]))
                        {
                            diffItems.Add(t1Items[itemName]);
                            diffItems.Add(t2Items[itemName]);
                        }
                    }
                    else if (t1DupesList.Count > 0 && t2DupesList.Count == 0)
                    {
                        t1DupesList.Add(itemName);
                        for (int x = 0; x < t1DupesList.Count; x++)
                        {
                            if (MatchItems(t1Items[t1DupesList[x]].Parent, t2Items[itemName].Parent) && MatchIfFiles(t1Items[t1DupesList[x]], t2Items[itemName]))
                            {
                                t1DupesList.RemoveAt(x);
                                break;
                            }
                        }
                    }
                    else if (t1DupesList.Count == 0 && t2DupesList.Count > 0)
                    {
                        t2DupesList.Add(itemName);
                        for (int x = 0; x < t2DupesList.Count; x++)
                        {
                            if (MatchItems(t1Items[itemName].Parent, t2Items[t2DupesList[x]].Parent) && MatchIfFiles(t1Items[itemName], t2Items[t2DupesList[x]]))
                            {
                                t2DupesList.RemoveAt(x);
                                break;
                            }
                        }
                    }
                    else
                    {
                        t1DupesList.Add(itemName);
                        t2DupesList.Add(itemName);

                        HashSet<int> matched1 = new();
                        HashSet<int> matched2 = new();
                        
                        for (int x = 0; x < t1DupesList.Count; x++)
                        {
                            for (int y = 0; y < t2DupesList.Count; y++)
                            {
                                if (MatchItems(t1Items[t1DupesList[x]].Parent, t2Items[t2DupesList[y]].Parent) && MatchIfFiles(t1Items[t1DupesList[x]], t2Items[t2DupesList[y]]))
                                {
                                    matched1.Add(x);
                                    matched2.Add(y);
                                }
                            }
                        }

                        List<string> newT1DupesList = new();
                        List<string> newT2DupesList = new();

                        for (int x = 0; x < t1DupesList.Count; x++)
                        {
                            if (!matched1.Contains(x))
                                newT1DupesList.Add(t1DupesList[x]);
                        }

                        for (int x = 0; x < t2DupesList.Count; x++)
                        {
                            if (!matched2.Contains(x))
                                newT2DupesList.Add(t2DupesList[x]);
                        }

                        t1DupesList = newT1DupesList;
                        t2DupesList = newT2DupesList;
                    }

                    for (int x = 0; x < t1DupesList.Count; x++)
                        diffItems.Add(t1Items[t1DupesList[x]]);

                    for (int x = 0; x < t2DupesList.Count; x++)
                        diffItems.Add(t2Items[t2DupesList[x]]);
                }
            }

            if (t1Levels != t2Levels)
            {
                if (t1Levels < t2Levels)
                {
                    for (int i = t1Levels; i < t2Levels; i++)
                    {
                        MarkAllAsDiffAt(1, i);
                    }
                }
                else if (t2Levels < t1Levels)
                {
                    for (int i = t2Levels; i < t1Levels; i++)
                    {
                        MarkAllAsDiffAt(0, i);
                    }
                }
            }
        }

        public void Print()
        {
            Printer.PrintColored("Count = " + diffItems.Count, ConsoleColor.Cyan);
            Console.WriteLine();

            int ct = 0;

            for (int i = 0; i < diffItems.Count; i++)
            {
                string fullPath = diffItems[i].FullPath();

                FileAttributes attr = File.GetAttributes(fullPath);

                if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    Printer.PrintColored(fullPath, ConsoleColor.Red);
                    ct++;
                }
            }

            Printer.PrintColored("Files Changed Count = " + ct, ConsoleColor.Cyan);
        }
    }
}
