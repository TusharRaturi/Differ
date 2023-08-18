using System.Diagnostics.CodeAnalysis;

namespace Differ
{
    internal class Item : IComparable<Item>
    {
        private string itemString;
        private Item? parent;
        private int level;

        private int structurePathIdx = 0;

        public int ParentAppendLevel { get; set; } = 1;

        public string ItemString { get => itemString; set => itemString = value; }
        public Item? Parent { get => parent; set => parent = value; }
        public int Level { get => level; set => level = value; }

        public Item(string itemString, Item? parent, int itemLevel, int structurePathIdx)
        {
            this.itemString = itemString;
            this.parent = parent;
            level = itemLevel;
            this.structurePathIdx = structurePathIdx;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not Item) return false;
            return ToString().Equals(obj.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        private static string GetParentAppendString([DisallowNull] Item item, int appendLevel = 0)
        {
            Item? itemParent = item.Parent;

            if (appendLevel <= 0)
            {
                if (itemParent == null)
                    return "";
                else return GetParentAppendString(itemParent, --appendLevel) + "\\" + itemParent.ItemString;
            }

            if (itemParent == null)
                return "";

            if (appendLevel == 1)
                return itemParent.ItemString;
            else
            {
                string recur = GetParentAppendString(itemParent, --appendLevel);
                return (recur == "" ? "" : recur + "\\") + itemParent.ItemString;
            }
        }

        public override string ToString()
        {
            if (parent == null)
                return "[L" + level + "]: " + itemString;
            else
                return "[L" + level + "]: " + GetParentAppendString(this, ParentAppendLevel) + "\\" + itemString;
        }

        public string DefaultString()
        {
            return "[L" + level + "]: " + (parent != null ? parent.ItemString + "\\" : "") + itemString;
        }

        public string CompletePath()
        {
            return GetParentAppendString(this) + "\\" + itemString;
        }

        public string FullPath()
        {
            return FSStructure.GetPathPrepend(structurePathIdx) + CompletePath();
        }

        public int CompareTo(Item? other)
        {
            if (other == null) return 0;
            return other.ToString().CompareTo(ToString());
        }
    }

    internal class Level
    {
        private readonly Dictionary<string, Item> items;
        private int levelIdx;

        public int Count => items.Count;

        public Dictionary<string, Item> Items => items;

        public Dictionary<string, List<string>> Duplicates { get => duplicates; set => duplicates = value; }
        public HashSet<string> DuplicateValues { get => duplicateValues; set => duplicateValues = value; }

        private Dictionary<string, List<string>> duplicates;

        private HashSet<string> duplicateValues;

        private static int totalLevels = 0;

        public Level(int levelIdx)
        {
            items = new();
            duplicates = new();
            duplicateValues = new();
            this.levelIdx = levelIdx;

            totalLevels++;
        }

        public Item AddToLevel(string itemString, Item? parent, int structurePathIdx)
        {
            Item it = new(itemString, parent, levelIdx, structurePathIdx);

            string defaultStr = it.ToString();

            bool addToSameItems = false;
            while (items.ContainsKey(it.ToString()) && it.ParentAppendLevel < totalLevels)
            {
                it.ParentAppendLevel++;
                addToSameItems = true;
            }

            string itStr = it.ToString();

            items.Add(itStr, it);
            if (addToSameItems)
            {
                if (duplicates.ContainsKey(defaultStr))
                    duplicates[defaultStr].Add(itStr);
                else duplicates.Add(defaultStr, new List<string>() { itStr });

                duplicateValues.Add(itStr);
            }

            return it;
        }
    }

    internal class FSStructure
    {
        private List<Level> levels;

        public List<Level> Levels => levels;

        private static char[] pathExtractorHelper = new char[500];

        private int structurePathIdx = 0;
        private static List<string> structurePathPrepends = new();

        private static void AddStructurePathPrepend(string path)
        {
            string prependToAdd = path.Substring(0, path.LastIndexOf('\\'));
            structurePathPrepends.Add(prependToAdd);
        }

        public static string GetPathPrepend(int structureIdx)
        {
            return structurePathPrepends[structureIdx];
        }

        public FSStructure(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            directoryPath = directoryPath.Replace("/", "\\");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            levels = new();

            structurePathIdx = structurePathPrepends.Count;
            AddStructurePathPrepend(directoryPath);

            ReadStructure(directoryPath, 0, null);
        }

        public void ReadStructure(string path, int level, Item? parent)
        {
            string current = GetObjectNameFromPath(path);
            Item currentItem = Add(level, current, parent);

            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs);

            for (int i = 0; i < dirs.Length; i++)
            {
                ReadStructure(dirs[i], level + 1, currentItem);
            }

            string[] files = Directory.GetFiles(path);
            Array.Sort(files);

            for (int i = 0; i < files.Length; i++)
            {
                Add(level + 1, GetObjectNameFromPath(files[i]), currentItem);
            }
        }

        private static string GetObjectNameFromPath(string path)
        {
            int c = 499;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '\\')
                    break;
                pathExtractorHelper[c--] = path[i];
            }

            return new string(pathExtractorHelper, c + 1, 499 - c);
        }

        private Item Add(int level, string value, Item? parent)
        {
            if (level < 0 || string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot add \"" + value + "\" to structure at level " + level);

            if (level < levels.Count)
            {
                return levels[level].AddToLevel(value, parent, structurePathIdx);
            }
            else
            {
                levels.Add(new Level(levels.Count));
                return levels[level].AddToLevel(value, parent, structurePathIdx);
            }
        }

        private static string ApplyIndent(int level)
        {
            char[] indent = new char[level];

            for (int i = 0; i < level; i++)
                indent[i] = '\t';

            return new string(indent);
        }

        public void DebugPrint(bool onlyDuplicates = false)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                List<Item> items = levels[i].Items.Values.ToList();

                for (int c = 0; c < items.Count; c++)
                {
                    string defStr = items[c].DefaultString();
                    string itStr = items[c].ToString();
                    
                    if (!onlyDuplicates)
                    {
                        Console.Write(ApplyIndent(i) + defStr);
                        Printer.PrintColored(levels[i].Duplicates.ContainsKey(defStr) ? " [ HasDuplicates, actual key = " + itStr + " ] , Path = " + items[c].FullPath() : "", ConsoleColor.Cyan);
                    }
                    else if (levels[i].Duplicates.ContainsKey(defStr))
                    {
                        Printer.PrintColored(ApplyIndent(i) + defStr + " [ HasDuplicates, actual key = " + itStr + " ] , Path = " + items[c].FullPath(), ConsoleColor.Cyan);
                    }
                }
            }
        }
    }
}
