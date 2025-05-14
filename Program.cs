using Raylib_cs;
using InteractiveAI.DynamicScriptLoader;
using static Raylib_cs.Raylib;
using static InteractiveAI.Utilities.ConsoleUtils;

namespace InteractiveAI
{
    class Program
    {
        static void Main(string[] args)
        {
            SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
            SetTargetFPS(60);
            
            InitWindow(1280, 720, "Interactive AI");
            
            var scriptLoader = new ScriptsLoader("./BehaviourScripts");

            while (!WindowShouldClose())
            {
                if ((IsKeyDown(KeyboardKey.LeftControl) ||
                     IsKeyDown(KeyboardKey.RightControl))
                    && IsKeyPressed(KeyboardKey.D))
                {
                    scriptLoader.DisableWarnings = !scriptLoader.DisableWarnings;
                    var message = scriptLoader.DisableWarnings ? "====> Disabled warning display" : "====> Enabled warning display";
                    PrintMessage(message, ConsoleColor.Cyan);
                    
                }

                BeginDrawing();
                ClearBackground(Color.Gray);

                for (var i = 0; i < scriptLoader.Behaviours.Count; i++)
                {
                    var behaviour = scriptLoader.Behaviours[i];
                    behaviour.Update();
                }

                DrawFPS(880, 14);
                DrawText("Interactive AI in action : Merging Classical Game AI and Generative AI in Real-Time", 12, 14, 20, Color.RayWhite);
                
                EndDrawing();
            }

            CloseWindow();
        }
    }
}

