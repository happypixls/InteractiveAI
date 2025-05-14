using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace InteractiveAI.BehaviourScripts
{
    public class PersuitBehaviour : IBehaviour
    {
        private readonly float speed = 100;
        private Vector2 agent = new Vector2(200, 200);
        private Vector2 target = new Vector2(250, 250);
        
        public void Start()
        {
            Console.WriteLine("Basic persuit behaviour");
        } 

        public void Update()
        {
            if (IsMouseButtonDown(MouseButton.Left))
                target = GetMousePosition();

            DrawCircleV(target, 10, Color.LightGray);
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