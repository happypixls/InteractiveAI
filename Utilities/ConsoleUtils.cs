namespace InteractiveAI.Utilities;

public static class ConsoleUtils
{
    public static void PrintMessage(string message, ConsoleColor color = ConsoleColor.Black)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}