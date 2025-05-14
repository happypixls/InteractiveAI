using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace InteractiveAI.BehaviourScripts
{
    /// <summary>
    /// This class is a sample script used by the demo
    /// that gets loaded automatically from the BehaviourScripts folder beside the executable.
    /// Scripts can reference each other btw ;)
    /// Be sure to instantiate them because the runtime doesn't do that for you. 
    /// </summary>
    public class SampleBehaviourScript : IBehaviour
    {
        private readonly float speed = 1000;
        private Vector2 agent = new Vector2(100, 100);
        private Vector2 target = new Vector2(150, 150);
        
        public void Start()
        {
            Console.WriteLine("Hello from start SampleBehaviourScript");
        } 

        public void Update()
        {
            if (IsMouseButtonDown(MouseButton.Left))
                target = GetMousePosition();

            DrawCircleV(target, 10, Color.Green);
            DrawCircleV(agent, 10, Color.Red);
            DrawLineEx(agent, target, 2, Color.Magenta);

            var direction = Vector2.Normalize(target - agent);
            
            if(Vector2.DistanceSquared(agent, target) > 100)
            {
                agent += direction * speed * GetFrameTime();
            }
        }
    }
}