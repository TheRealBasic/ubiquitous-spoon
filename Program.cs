using System;

namespace NightclubSim
{
    /// <summary>
    /// Entry point for the MonoGame application.
    /// </summary>
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();
        }
    }
}
