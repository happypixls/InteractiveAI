using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static InteractiveAI.Utilities.ConsoleUtils;

namespace InteractiveAI.BehaviourScripts
{
    public class ZigZagPersuitBehaviour : IBehaviour
    {
        private float speed = 400;
        private float zigZagIntensity = 75f;

        private Vector2 agent = new Vector2(100, 100);
        private Vector2 target = new Vector2(150, 150);

        public void Start()
        {
            PrintMessage("Hello from start ZigZag chase behaviour", ConsoleColor.Cyan);
        } 

        public void Update()
        {
            Vector2 direction = target - agent;
            float distance = direction.Length();

            //speed = distance > 200 ? 100 : 400f;// When in range speed up while approaching
            speed = distance > 200f ? 400 : 100f; //When in range slow down while approaching

            if (IsMouseButtonDown(MouseButton.Left))
                target = GetMousePosition();

            DrawLineEx(agent, target, 2, Color.Magenta);
            DrawCircleV(target, 10, Color.Green);
            DrawCircleV(agent, 10, Color.Red);

            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
            float zigzagFactor = (float)Math.Sin(Environment.TickCount / 100) * zigZagIntensity * 0.01f;
            direction += perpendicular * zigzagFactor;
            direction = Vector2.Normalize(direction);

            if(Vector2.DistanceSquared(agent, target) > 100)
                agent += direction * speed * GetFrameTime();
        }
    }
}