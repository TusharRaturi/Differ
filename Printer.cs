namespace Differ
{
    internal class Printer
    {
        public static void PrintColored(string value, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            ConsoleColor fgCache = Console.ForegroundColor;
            ConsoleColor bgCache = Console.BackgroundColor;

            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            
            Console.WriteLine(value);

            Console.ForegroundColor = fgCache;
            Console.BackgroundColor = bgCache;
        }
    }
}
