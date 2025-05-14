using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Color;

namespace InteractiveAI.BehaviourScripts.BoidsWithPurposeAreaTest
{
    public class BoidsWithPurposeArea : IBehaviour
    {
        private Vector2 clickTarget;
        private bool  attractMode = true; // true = seek, false = flee
        private int ScreenWidth;
        private int ScreenHeight;
        private const int BoidCount = 300;
        private const float AttractStrength = 0.6f;
        private const float  FleeStrength   = 0.8f;

        private List<Boid> Boids = new List<Boid>();

        public void Start()
        {
            ScreenHeight = GetScreenHeight();
            ScreenWidth = GetScreenWidth();
            clickTarget = new Vector2(GetScreenWidth() / 2, GetScreenHeight() / 2);
            var rng   = new Random();
            for (int i = 0; i < BoidCount; i++)
                Boids.Add(new Boid(ScreenWidth, ScreenHeight, rng));
        }

        public void Update()
        {
            ScreenHeight = GetScreenHeight();
            ScreenWidth = GetScreenWidth();

            if (IsMouseButtonPressed(MouseButton.Left))
                clickTarget = GetMousePosition();

            if (IsKeyPressed(KeyboardKey.A))
                attractMode = !attractMode;
            
            DrawText(attractMode ? "State -> Seeking" : "State -> Fleeing", 25, 45, 20, Color.Yellow);

            foreach (var b in Boids)
            {
                DrawCircle((int)clickTarget.X, (int)clickTarget.Y, 10, Color.Black);
                
                b.Flock(Boids);
                var force = attractMode
                    ? b.SeekCircle(clickTarget, 5) * AttractStrength
                    : b.FleeCircle(clickTarget, 40, 40) *  FleeStrength;
                b.ApplyForce(force);

                b.Update();
                b.Borders(ScreenWidth, ScreenHeight);
                b.Draw();
            }
        }
    }

    public class Boid
    {
        private Vector2 Position;
        private Vector2 Velocity;
        private Vector2 Acceleration;
        private const float MaxForce = 0.05f;
        private const float MaxSpeed = 2.0f;

        public Boid(int screenWidth, int screenHeight, Random rng)
        {
            Position = new Vector2(rng.Next(screenWidth), rng.Next(screenHeight));
            
            float angle = (float)(rng.NextDouble() * Math.PI * 2);
            Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        public void Flock(List<Boid> boids)
        {
            Vector2 sep = Separate(boids)  * 2.5f;
            Vector2 ali = Align(boids)     * 1.0f;
            Vector2 coh = Cohesion(boids)  * 1.0f;

            ApplyForce(sep);
            ApplyForce(ali);
            ApplyForce(coh);
        }

        public void ApplyForce(Vector2 force)
        {
            Acceleration += force;
        }

        private Vector2 Separate(List<Boid> boids)
        {
            float desiredSeparation = 25;
            Vector2 steer = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                float d = Vector2.Distance(Position, other.Position);
                if (other != this && d < desiredSeparation && d > 0)
                {
                    Vector2 diff = Vector2.Normalize(Position - other.Position) / d;
                    steer += diff;
                    count++;
                }
            }

            if (count > 0)
                steer /= count;

            if (steer.Length() > 0)
            {
                steer = Vector2.Normalize(steer) * MaxSpeed - Velocity;
                if (steer.Length() > MaxForce)
                    steer = Vector2.Normalize(steer) * MaxForce;
            }
            return steer;
        }

        public Vector2 SeekCircle(Vector2 C, float R)
        {
            //  compute nearest boundary point:
            Vector2 toCenter = C - Position;
            float d = toCenter.Length();
            Vector2 boundary = C - Vector2.Normalize(toCenter) * R;

            // then just Seek toward that boundary:
            return Seek(boundary);
        }

        public Vector2 FleeCircle(Vector2 C, float R, float panicRadius)
        {
            Vector2 toCenter = C - Position;
            float d = toCenter.Length();

            if (d < R + panicRadius)
            {
                // aim directly away from center:
                Vector2 desired = Vector2.Normalize(Position - C) * MaxSpeed;
                Vector2 steer   = desired - Velocity;
                if (steer.Length() > MaxForce)
                    steer = Vector2.Normalize(steer) * MaxForce;
                return steer;
            }
            return Vector2.Zero;
        }

        private Vector2 Align(List<Boid> boids)
        {
            float neighborDist = 50;
            Vector2 sum = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                float d = Vector2.Distance(Position, other.Position);
                if (other != this && d < neighborDist)
                {
                    sum += other.Velocity;
                    count++;
                }
            }

            if (count > 0)
            {
                sum /= count;
                sum = Vector2.Normalize(sum) * MaxSpeed;
                Vector2 steer = sum - Velocity;
                if (steer.Length() > MaxForce)
                    steer = Vector2.Normalize(steer) * MaxForce;
                return steer;
            }
            return Vector2.Zero;
        }

        private Vector2 Cohesion(List<Boid> boids)
        {
            float neighborDist = 50;
            Vector2 sum = Vector2.Zero;
            int count = 0;

            foreach (var other in boids)
            {
                float d = Vector2.Distance(Position, other.Position);
                if (other != this && d < neighborDist)
                {
                    sum += other.Position;
                    count++;
                }
            }

            if (count > 0)
            {
                Vector2 target = sum / count;
                return Seek(target);
            }
            return Vector2.Zero;
        }

        public Vector2 Seek(Vector2 target)
        {
            Vector2 desired = target - Position;
            desired = Vector2.Normalize(desired) * MaxSpeed;
            Vector2 steer = desired - Velocity;
            if (steer.Length() > MaxForce)
                steer = Vector2.Normalize(steer) * MaxForce;
            return steer;
        }

        public Vector2 Flee(Vector2 target)
        {
            Vector2 desired = Position - target;
            desired = Vector2.Normalize(desired) * MaxSpeed;
            Vector2 steer   = desired - Velocity;
            if (steer.Length() > MaxForce)
                steer = Vector2.Normalize(steer) * MaxForce;
            return steer;
        }

        public void Update()
        {
            Velocity += Acceleration;
            if (Velocity.Length() > MaxSpeed)
                Velocity = Vector2.Normalize(Velocity) * MaxSpeed;

            Position += Velocity;
            Acceleration = Vector2.Zero;
        }

        public void Borders(int w, int h)
        {
            if (Position.X < 0) Position.X += w;
            if (Position.X > w) Position.X -= w;
            if (Position.Y < 0) Position.Y += h;
            if (Position.Y > h) Position.Y -= h;
        }

        public void Draw()
        {
            Vector2 dir = Vector2.Normalize(Velocity);
            Vector2 perp = new Vector2(dir.Y, -dir.X);

            Vector2 p1 = Position + dir * 8;            // nose
            Vector2 p2 = Position - dir * 5 + perp * 4; // left wing
            Vector2 p3 = Position - dir * 5 - perp * 4; // right wing

            DrawTriangle(p1, p2, p3, Color.RayWhite);
        }
    }
}