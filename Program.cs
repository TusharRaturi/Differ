namespace Differ
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FSStructure fs1 = new("C:\\Users\\Tushar\\Documents\\Unity Projects\\Hypercasual-Template\\Assets\\Zenida Packages");
            FSStructure fs2 = new("C:\\Users\\Tushar\\Documents\\Unity Projects\\Merge-Warrior-3D-CPI\\Assets\\Zenida Packages");

            Console.WriteLine("Diffs Found: ");
            Console.WriteLine();

            Differ fs12Differ = new(fs1, fs2);
            fs12Differ.FindDiffs();
            fs12Differ.Print();
        }
    }
}