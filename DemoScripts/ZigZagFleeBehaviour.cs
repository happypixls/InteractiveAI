using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static InteractiveAI.Utilities.ConsoleUtils;

namespace InteractiveAI.BehaviourScripts
{
    public class ZigZagFleeBehaviour : IBehaviour
    {
        private float speed = 400;
        private float zigZagIntensity = 75f;
        private bool zigZag = true;

        private Vector2 agent = new Vector2(400, 400);
        private Vector2 target = new Vector2(150, 150);

        public void Start()
        {
            PrintMessage("Hello from start ZigZag flee behaviour", ConsoleColor.Cyan);
        } 

        public void Update()
        {
            CheckBorders(GetScreenWidth(), GetScreenHeight());
            Vector2 direction = agent - target;
            float distance = direction.Length();

            speed = distance > 200 ? 100 : 400f;

            if (IsMouseButtonDown(MouseButton.Left))
                target = GetMousePosition();

            DrawLineEx(agent, target, 2, Color.DarkGray);
            DrawCircleV(target, 10, Color.Green);
            DrawCircleV(agent, 10, Color.Red);

            if (zigZag)
            {
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                float zigzagFactor = (float)Math.Sin(Environment.TickCount / 100) * zigZagIntensity * 0.01f;
                direction += perpendicular * zigzagFactor;
            }
            direction = Vector2.Normalize(direction);

            if(Vector2.DistanceSquared(agent, target) > 50)
                // only flee if "too close" (distance² < threshold²)

            if (Vector2.DistanceSquared(agent, target) < 400 * 400)
             agent += direction * speed * GetFrameTime();
        }

        public void CheckBorders(int screenWidth, int screenHeight)
        {
            if (agent.X < 0) agent.X = screenWidth / 2;
            if (agent.X > screenWidth) agent.X = screenWidth / 2;
            if (agent.Y < 0) agent.Y = screenHeight / 2;
            if (agent.Y > screenHeight) agent.Y = screenHeight / 2;
        }
    }
}