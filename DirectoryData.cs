namespace Differ
{
    internal class DirectoryData
    {
        private List<string> items;
        private List<int> levelStartIndices;
        private List<int> levelEndIndices;

        private int ItemCount = 0;

        private Dictionary<string, int> itemIDMap;

        public DirectoryData(string directoryPath)
        {
            items = new();
            levelStartIndices = new();
            levelEndIndices = new();

            itemIDMap = new();

            if (string.IsNullOrEmpty(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            directoryPath = directoryPath.Replace("/", "\\");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            string objName = GetObjectNameFromPath(directoryPath);
            itemIDMap.Add(objName, ItemCount);
            AddItem(objName);

            levelStartIndices.Add(0);
            levelEndIndices.Add(0);
            
            ReadDirectory(directoryPath, 0);
        }

        private void ReadDirectory(string path, int level)
        {
            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs);
            string[] files = Directory.GetFiles(path);
            Array.Sort(files);

            if (dirs.Length == 0 && files.Length == 0)
                return;

            levelStartIndices.Add(ItemCount);

            for (int i = 0; i < dirs.Length; i++)
            {
                string objName = GetObjectNameFromPath(dirs[i]);
                itemIDMap.Add(objName, ItemCount);
                AddItem(objName);
            }

            for (int i = 0; i < files.Length; i++)
            {
                string objName = GetObjectNameFromPath(files[i]);
                itemIDMap.Add(objName, ItemCount);
                AddItem(objName);
            }

            levelEndIndices.Add(ItemCount - 1);

            for (int i = 0; i < dirs.Length; i++)
            {
                ReadDirectory(dirs[i], level + 1);
            }
        }

        private static string GetObjectNameFromPath(string path)
        {
            char[] chars = new char[10];
            int c = 9;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '\\')
                    break;
                chars[c--] = path[i];
            }

            return new string(chars);
        }

        private static string ApplyIndent(string name, int level)
        {
            for (int i = 0; i < level; i++)
                name = "|   " + name;

            return name;
        }

        private void AddItem(string item)
        {
            items.Add(item);
            ItemCount++;
        }

        /*private Item Add(int level, string value, Item? parent)
        {
            if (level < 0 || string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot add \"" + value + "\" to structure at level " + level);

            ItemCount++;

            if (level < levels.Count)
            {
                return levels[level].AddToLevel(value, parent);
            }
            else
            {
                levels.Add(new Level(levels.Count));
                return levels[level].AddToLevel(value, parent);
            }
        }*/

        private static string ApplyIndent(int level)
        {
            char[] indent = new char[level];

            for (int i = 0; i < level; i++)
                indent[i] = '\t';

            return new string(indent);
        }

        public void DebugPrint()
        {
            for (int i = 0; i < levelStartIndices.Count; i++)
            {
                for (int j = levelStartIndices[i]; j <= levelEndIndices[i]; j++)
                {
                    Console.WriteLine(itemIDMap[items[j]] + ": " + ApplyIndent(i) + items[j]);
                }
            }
        }

        public void Print()
        {
            /*for (int i = 0; i < levels.Count; i++)
            {
                for (int j = 0; j < levels[i].Count; j++)
                    Console.WriteLine(i + "-" + j + ": " + levels[i].GetItem(j));
            }*/
        }
    }
}
