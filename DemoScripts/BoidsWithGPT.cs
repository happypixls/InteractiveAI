using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using System.Net.Http;
using System.Net.Http.Headers;

using OpenAI;
using OpenAI.Models;
using OpenAI.Chat;
using Newtonsoft.Json;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Color;
using static InteractiveAI.Utilities.ConsoleUtils;

namespace InteractiveAI.BehaviourScripts.BoidsGPTTest
{
    public class BoidsWithGPT : IBehaviour
    {
        #region GPT_API_KEY
        private string apiKey = "YOUR_API_KEY_GOES_HERE";
        #endregion

        private Vector2 clickTarget;
        private bool  attractMode = true; // true = seek, false = flee
        private int ScreenWidth;
        private int ScreenHeight;
        private const int BoidCount = 200;
        private float configRadius = 200;
        private List<Boid> Boids = new List<Boid>();
        private BoidState state = BoidState.Neutral;

        #region AudienceConsoleParameters
        private string inputBuffer = "";
        private List<string> logEntries = new List<string>();
        private const int MaxLogEntries = 7;
        #endregion
        
        public enum BoidState {
            Neutral, Friendly, Hostile, Curiosity, Excitement, Panic, Huddle, Dance, Competition
        }

        public void Start()
        {
            ScreenHeight = GetScreenHeight();
            ScreenWidth = GetScreenWidth();
            clickTarget = new Vector2(GetScreenWidth() / 2, GetScreenHeight() / 2);
            var random   = new Random();
            for (int i = 0; i < BoidCount; i++)
                Boids.Add(new Boid(ScreenWidth, ScreenHeight, random));
        }

        private async Task TestPrompt(string prompt)
        {
            PrintMessage($"Sending prompt to API {prompt}", ConsoleColor.Yellow);
            
            var retrievedResponse = await GetScenarioAsync(BuildPrompt(prompt));
            PrintMessage(retrievedResponse.scenario.ToString(), ConsoleColor.Green);
            state = retrievedResponse.state;
            PrintMessage($"current state {state}", ConsoleColor.Yellow);
        }

        public async Task<(BoidState state, string scenario)> GetScenarioAsync(string prompt)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestData = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "You generate structured JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonConvert.SerializeObject(requestData),
                System.Text.Encoding.UTF8, "application/json"));

            string jsonResponse = await response.Content.ReadAsStringAsync();
            PrintMessage(jsonResponse, ConsoleColor.Magenta);
            dynamic result = JsonConvert.DeserializeObject(jsonResponse);
            string content = result.choices[0].message.content;

            dynamic scenarioData = JsonConvert.DeserializeObject(content);

            return (scenarioData.state, scenarioData.scenario);
        }
        
        private string BuildPrompt(string promptScenario)
        {
            string prompt = @"
                You're assisting in controlling the behavior of simulated agents (boids) based on provided scenarios. Each scenario describes the interaction between boids and a central object.

                Analyze the provided scenario below, then select the most appropriate state from the following predefined list and respond exclusively with structured JSON as shown in the example.
                Make sure that you don't include JSON styling in the response (i.e. without ```json and other pretty annotation for the browser) such that JSON deserializer can work with the response.

                Predefined States:
                - Neutral: No clear reaction, they are gentle and calm.
                - Friendly: Boids clearly attracted and approach positively.
                - Hostile: Boids feel threatened and flee, boids keep their distance, avoiding close contact.
                - Curiosity: Boids cautiously approach, intrigued but careful.
                - Excitement: Boids behave energetically and excitedly.
                - Panic: Boids scatter chaotically, very frightened.
                - Huddle: Boids gather tightly around the object, sometimes to get warmer if it is cold.
                - Dance: Boids move rhythmically or synchronized around the object.
                - Competition: Boids move competitively, occasionally repelling each other due to limited resources.

                Example Response:
                {
                ""state"": ""Curiosity"",
                ""scenario"": ""The central object emits intriguing signals, prompting boids to cautiously approach and circle slowly.""
                }

                Provided Scenario:
                """ + promptScenario + "\"";

            return prompt;
        }

        public void Update()
        {
            ScreenHeight = GetScreenHeight();
            ScreenWidth = GetScreenWidth();

            if (IsMouseButtonPressed(MouseButton.Left))
                clickTarget = GetMousePosition();

            if ((Raylib.IsKeyDown(KeyboardKey.LeftControl) ||
                 Raylib.IsKeyDown(KeyboardKey.RightControl))
                && Raylib.IsKeyPressed(KeyboardKey.A))
                attractMode = !attractMode;
            
            DrawText($"State -> {state}", 25, 45, 20, Color.Yellow);

            foreach (var boid in Boids)
            {

                DrawCircle((int)clickTarget.X, (int)clickTarget.Y, 10, Color.Black);

                switch (state)
                {
                    case BoidState.Neutral:
                        boid.Flock(Boids);
                        break;
                    case BoidState.Friendly:
                        boid.ApplyForce(boid.SeekCircle(clickTarget, 100));
                        boid.Flock(Boids);
                        break;
                    case BoidState.Hostile:
                        boid.ApplyForce(boid.FleeCircle(clickTarget, configRadius, 100));
                        boid.Flock(Boids);
                        break;
                    case BoidState.Curiosity:
                        boid.Curiosity(clickTarget, configRadius, Boids);
                        break;
                    case BoidState.Excitement:
                        boid.Excitement(clickTarget, configRadius);
                        boid.Flock(Boids);
                        break;
                    case BoidState.Panic:
                        boid.Panic();
                        boid.Flock(Boids);
                        break;
                    case BoidState.Huddle:
                        boid.Huddle(clickTarget, Boids);
                        break;
                    case BoidState.Dance:
                        boid.Dance(Boids);
                        boid.Flock(Boids);
                        break;
                    case BoidState.Competition:
                        boid.Competition(clickTarget, Boids);
                        break;
                    default:
                        boid.Flock(Boids);
                        break;
                }

                boid.Update();
                boid.Borders(ScreenWidth, ScreenHeight);
                boid.Draw();
            }

            DrawAndHandleAudienceConsole();
        }

        public void DrawAndHandleAudienceConsole()
        {
            int key = Raylib.GetCharPressed();
            if (key > 0)
            {
                char c = (char)key;
                if (c >= 32)
                    inputBuffer += char.ConvertFromUtf32(c);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && inputBuffer.Length > 0)
                inputBuffer = inputBuffer[..^1];

            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (Raylib.IsKeyDown(KeyboardKey.LeftShift) ||
                    Raylib.IsKeyDown(KeyboardKey.RightShift))
                {
                    inputBuffer += "\n";
                }
                else
                {
                    logEntries.Insert(0, inputBuffer);
                    if (logEntries.Count > MaxLogEntries)
                        logEntries.RemoveAt(logEntries.Count - 1);
                        {
                            TestPrompt(inputBuffer);
                            inputBuffer = "";
                        }
                }
            }
            
            if ((Raylib.IsKeyDown(KeyboardKey.LeftControl) ||
                 Raylib.IsKeyDown(KeyboardKey.RightControl))
                && Raylib.IsKeyPressed(KeyboardKey.L))
            {
                logEntries.Clear();
            }

            for (int i = 0; i < logEntries.Count; i++)
            {
                Raylib.DrawText(
                    logEntries[i],
                    10,
                    100 + i * 16,
                    20,
                    Color.Black
                );
            }

            // Draw input box background
            int boxHeight = 60;
            int boxY = ScreenHeight - boxHeight - 10;
            Raylib.DrawRectangle(5, boxY, ScreenWidth - 10, boxHeight, new Color(0, 0, 0, 180));

            string[] lines = inputBuffer.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string textToDraw = lines[i] + (i == lines.Length - 1 ? "_" : "");
                Raylib.DrawText(
                    textToDraw,
                    15,
                    boxY + 5 + i * 20,
                    20,
                    Color.RayWhite
                );
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
        private Random random = new Random();

        public Boid(int screenWidth, int screenHeight, Random random)
        {
            Position = new Vector2(random.Next(screenWidth), random.Next(screenHeight));
            float angle = (float)(random.NextDouble() * Math.PI * 2);
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
            Vector2 toCenter = C - Position;
            float d = toCenter.Length();
            Vector2 boundary = C - Vector2.Normalize(toCenter) * R;

            return Seek(boundary);
        }

        public Vector2 FleeCircle(Vector2 C, float R, float panicRadius)
        {
            Vector2 toCenter = C - Position;
            float d = toCenter.Length();

            if (d < R + panicRadius)
            {
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


        #region Behaviours

        public void Curiosity(Vector2 center, float radius, List<Boid> boids)
        {
            Vector2 force = SeekCircle(center, radius);
            // wobble effect
            Vector2 toCenter = center - Position;
            Vector2 perp     = new Vector2(-toCenter.Y, toCenter.X);
            perp = Vector2.Normalize(perp) * MaxForce;
            // oscillate over time
            float phase = (float)(random.NextDouble() * Math.PI * 5);
            Vector2 wobble = perp * (float)Math.Sin(GetTime() * 0.5f + phase);
            Vector2 sep = Separate(boids)  * 5f;
            Vector2 coh = Cohesion(boids)  * 1.0f;
            ApplyForce(sep);
            ApplyForce(coh);
            ApplyForce(force + wobble);
        }

        public void Excitement(Vector2 center, float radius)
        {
            // strong pull to circle
            Vector2 radial = SeekCircle(center, radius) * 2.0f;
            // strong tangential spin
            Vector2 toCenter = Position - center;
            Vector2 tangent  = new Vector2(-toCenter.Y, toCenter.X);
            tangent = Vector2.Normalize(tangent) * MaxForce * 5f;
            ApplyForce(radial + tangent);
        }

        public void Panic()
        {
            // pick a random unit vector and slam into acceleration
            Vector2 rnd = new Vector2(
                (float)(random.NextDouble() * 2 - 1),
                (float)(random.NextDouble() * 2 - 1)
            );
            rnd = Vector2.Normalize(rnd) * MaxForce * 10f;
            ApplyForce(rnd);
        }

        public void Huddle(Vector2 center, List<Boid> boids)
        {
            Vector2 toCenter = Seek(center) * 2.0f;
            Vector2 coh = Cohesion(boids) * 2.5f;
            Vector2 ali = Align(boids) * 2;
            Vector2 sep = Separate(boids) * 25;
            ApplyForce(toCenter + coh + ali + sep);
        }

        public void Dance(List<Boid> boids)
        {
            // synchronize via alignment
            Vector2 alignForce = Align(boids) * 1.5f;
            // global rhythm from time
            float t = (float)Raylib_cs.Raylib.GetTime() * 2f;
            Vector2 beat = new Vector2(MathF.Cos(t), MathF.Sin(t));
            Vector2 groove = Vector2.Normalize(beat) * MaxForce * 2f;
            ApplyForce(alignForce + groove);
        }

        public void Competition(Vector2 center, List<Boid> boids)
        {
            ApplyForce(Seek(center));
            ApplyForce(Separate(boids) * 1.5f);
            if (random.NextDouble() < 0.05)
                ApplyForce(Separate(boids) * 5.0f);
        }

        #endregion
    }
}