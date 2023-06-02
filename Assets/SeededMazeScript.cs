using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class SeededMazeScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public TextMesh[] displays;

    int[,] maze;
    bool activated;
    string seed;
    int currentX;
    int currentY;
    int goalX;
    int goalY;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += Activate;
    }

    void Start()
    {
        currentX = UnityEngine.Random.Range(-9, 10);
        currentY = UnityEngine.Random.Range(-9, 10);
        for (int i = 0; i < 24; i++)
            seed += UnityEngine.Random.Range(0, 10);
        while (!CheckSeedValidity())
        {
            seed = "";
            for (int i = 0; i < 24; i++)
                seed += UnityEngine.Random.Range(0, 10);
        }
        Debug.LogFormat("[Seeded Maze #{0}] Seed: {1}", moduleId, seed);
        Debug.LogFormat("[Seeded Maze #{0}] Maze:", moduleId);
        for (int i = 0; i < 19; i++)
        {
            string build = "";
            for (int j = 0; j < 19; j++)
            {
                if (maze[j, i] == -1)
                    build += "x";
                else
                    build += maze[j, i].ToString("X");
            }
            Debug.LogFormat("[Seeded Maze #{0}] {1}", moduleId, build);
        }
        string build2 = "";
        int[] serialDirs = new int[6];
        for (int i = 0; i < serialDirs.Length; i++)
        {
            if ("0123456789".Contains(bomb.GetSerialNumber()[i]))
                serialDirs[i] = int.Parse(bomb.GetSerialNumber()[i].ToString()) % 4;
            else
                serialDirs[i] = (bomb.GetSerialNumber()[i] - 64) % 4;
            if (serialDirs[i] == 0)
                build2 += "U";
            else if (serialDirs[i] == 1)
                build2 += "R";
            else if (serialDirs[i] == 2)
                build2 += "D";
            else
                build2 += "L";
        }
        int times = 2 * bomb.GetBatteryCount() + 5;
        Debug.LogFormat("[Seeded Maze #{0}] You must move {1} times using the sequence {2}", moduleId, times, build2);
        int tempX = 9;
        int tempY = 9;
        int index = 0;
        for (int i = 0; i < times; i++)
        {
            if (serialDirs[index] == 0 && tempY != 0 && maze[tempX, tempY - 1] != -1)
                tempY--;
            else if (serialDirs[index] == 1 && tempX != 18 && maze[tempX + 1, tempY] != -1)
                tempX++;
            else if (serialDirs[index] == 2 && tempY != 18 && maze[tempX, tempY + 1] != -1)
                tempY++;
            else if (serialDirs[index] == 3 && tempX != 0 && maze[tempX - 1, tempY] != -1)
                tempX--;
            index++;
            if (index == 6)
                index = 0;
        }
        goalX = tempX - 9;
        goalY = (tempY - 9) * -1;
        Debug.LogFormat("[Seeded Maze #{0}] The coordinate you end up at after performing the moves is ({1}, {2})", moduleId, goalX, goalY);
    }

    void Activate()
    {
        displays[0].text = seed.Insert(12, "\n");
        displays[1].text = currentX.ToString();
        displays[2].text = currentY.ToString();
        activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && activated != false)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int index = Array.IndexOf(buttons, pressed);
            if (index == 1)
            {
                currentX++;
                if (currentX == 10)
                    currentX = -9;
                displays[1].text = currentX.ToString();
            }
            else if (index == 2)
            {
                currentY++;
                if (currentY == 10)
                    currentY = -9;
                displays[2].text = currentY.ToString();
            }
            else
            {
                if (currentX == goalX && currentY == goalY)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    audio.PlaySoundAtTransform("success", transform);
                    Debug.LogFormat("[Seeded Maze #{0}] Submitted ({1}, {2}), module solved", moduleId, currentX, currentY);
                    displays[1].text = "G";
                    displays[2].text = "G";
                    StartCoroutine(SolveAnimation());
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Seeded Maze #{0}] Submitted ({1}, {2}), strike", moduleId, currentX, currentY);
                }
            }
        }
    }

    bool CheckSeedValidity()
    {
        bool done = false;
        maze = new int[19, 19];
        for (int i = 0; i < 19; i++)
            for (int j = 0; j < 19; j++)
                maze[i, j] = -1;
        int startX = 9;
        int startY = 9;
        int seedCt = 0;
        Queue<VisitedCell> addedConnections = new Queue<VisitedCell>();
        addedConnections.Enqueue(new VisitedCell { x = startX, y = startY, gen = 0 });
        while (addedConnections.Count > 0)
        {
            VisitedCell next = addedConnections.Dequeue();
            if (next.x < 0 || next.x > 18 || next.y < 0 || next.y > 18)
                return false;
            if (maze[next.x, next.y] == -1)
            {
                maze[next.x, next.y] = next.gen;
                if (!done)
                {
                    switch (seed[seedCt])
                    {
                        case '0': break;
                        case '1': addedConnections.Enqueue(new VisitedCell { x = next.x - 1, y = next.y, gen = next.gen + 1 }); break;
                        case '2': addedConnections.Enqueue(new VisitedCell { x = next.x + 1, y = next.y, gen = next.gen + 1 }); break;
                        case '3': addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y - 1, gen = next.gen + 1 }); break;
                        case '4': addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y + 1, gen = next.gen + 1 }); break;
                        case '5': addedConnections.Enqueue(new VisitedCell { x = next.x + 1, y = next.y, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x - 1, y = next.y, gen = next.gen + 1 }); break;
                        case '6': addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y - 1, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x - 1, y = next.y, gen = next.gen + 1 }); break;
                        case '7': addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y - 1, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y + 1, gen = next.gen + 1 }); break;
                        case '8': addedConnections.Enqueue(new VisitedCell { x = next.x + 1, y = next.y, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y + 1, gen = next.gen + 1 }); break;
                        default: addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y - 1, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x + 1, y = next.y, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x, y = next.y + 1, gen = next.gen + 1 }); addedConnections.Enqueue(new VisitedCell { x = next.x - 1, y = next.y, gen = next.gen + 1 }); break;
                    }
                    seedCt++;
                    if (seedCt == seed.Length)
                        done = true;
                }
            }
        }
        if (done)
            return true;
        return false;
    }

    class VisitedCell
    {
        public int x;
        public int y;
        public int gen;
    }

    IEnumerator SolveAnimation()
    {
        int ct = 0;
        while (true)
        {
            string builder = "";
            for (int i = 0; i < 24; i++)
            {
                if (i == ct)
                    builder += "<color=yellow>" + seed[i] + "</color>";
                else
                    builder += seed[i];
                if (i == 11)
                    builder += "\n";
            }
            displays[0].text = builder;
            ct++;
            if (ct == 24)
                ct = 0;
            yield return new WaitForSeconds(.1f);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <x> <y> [Sets the small displays to the specified x and y coordinate and submits]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a coordinate's x and y to submit!";
            else if (parameters.Length == 2)
                yield return "sendtochaterror Please specify a coordinate's y to submit!";
            else if (parameters.Length > 3)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                int temp1 = -1;
                int temp2 = -1;
                if (!int.TryParse(parameters[1], out temp1))
                {
                    yield return "sendtochaterror!f The specified coordinate x '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp1 < -9 || temp1 > 9)
                {
                    yield return "sendtochaterror The specified coordinate x '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (!int.TryParse(parameters[2], out temp2))
                {
                    yield return "sendtochaterror!f The specified coordinate y '" + parameters[2] + "' is invalid!";
                    yield break;
                }
                if (temp2 < -9 || temp2 > 9)
                {
                    yield return "sendtochaterror The specified coordinate y '" + parameters[2] + "' is invalid!";
                    yield break;
                }
                yield return null;
                while (currentX != temp1)
                {
                    buttons[1].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
                while (currentY != temp2)
                {
                    buttons[2].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
                buttons[0].OnInteract();
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!activated) yield return true;
        while (currentX != goalX)
        {
            buttons[1].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        while (currentY != goalY)
        {
            buttons[2].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        buttons[0].OnInteract();
    }
}