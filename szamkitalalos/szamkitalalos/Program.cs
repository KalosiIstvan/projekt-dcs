using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static Random rnd = new Random();
    static HashSet<string> usedWords = new HashSet<string>();
    static List<string> wordBank = new List<string>();
    static int roundCounter = 0;

    static void Main(string[] args)
    {
        while (true)
        {
            try
            {
                RunGame();
            }
            catch (RestartException)
            {
                Console.Clear();
                continue;
            }
            break;
        }
    }

    static void RunGame()
    {
        Console.Clear();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Szólánc TXT Edition ===\n");
        Console.ResetColor();

        LoadWordBank();
        usedWords.Clear();
        roundCounter = 0;

        Console.WriteLine("Válassz módot: 1) Egyjátékos (géppel)  2) Kétjátékos (helyi)");
        Console.Write("Mód (1 vagy 2): ");
        string mode = ReadWithRestart()?.Trim();

        if (mode == "2") TwoPlayerMode();
        else SinglePlayerMode();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nA játék véget ért.");
        Console.WriteLine("Nyomj 'R'-t az újrakezdéshez, vagy bármi mást a kilépéshez...");
        Console.ResetColor();

        string again = Console.ReadLine()?.Trim().ToLower();
        if (again == "r") throw new RestartException();
    }

    static void LoadWordBank()
    {
        try
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "magyar-szavak.txt");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Nem található a fájl: {filePath}");
                Environment.Exit(0);
            }

            wordBank = File.ReadAllLines(filePath)
                           .Select(w => NormalizeWord(w))
                           .Where(w => !string.IsNullOrEmpty(w))
                           .ToList();

            if (wordBank.Count == 0)
            {
                Console.WriteLine("A fájl üres vagy minden sor érvénytelen.");
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hiba a fájl betöltése közben: " + ex.Message);
            Environment.Exit(0);
        }
    }

    // ==================== KÉTJÁTÉKOS ====================
    static void TwoPlayerMode()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Kétjátékos mód. Felváltva adtok szavakat. Nyomj 'R'-t a resethez.");
        Console.ResetColor();

        string lastWord = null;
        int currentPlayer = 1;
        int[] points = { 0, 0, 0 };
        int[] errorCount = { 0, 0, 0 }; // hibaszámláló minden játékosnak
        roundCounter = 0;

        while (true)
        {
            Console.ForegroundColor = currentPlayer == 1 ? ConsoleColor.Magenta : ConsoleColor.Blue;
            Console.WriteLine($"\nJátékos {currentPlayer}, írj egy szót (10 mp):");
            Console.ResetColor();

            string input = ReadWithRestart(10000);
            if (input == null) { Console.WriteLine("Idő lejárt! Vesztettél."); break; }

            string w = NormalizeWord(input);

            if (string.IsNullOrEmpty(w) || w.Length < 2)
                continue; // Nem számít hibának, nem ír ki semmit

            // Hibás szó ellenőrzés
            if (!wordBank.Contains(w) || (lastWord != null && GetFirstLetter(w) != GetLastLetter(lastWord)) || usedWords.Contains(w))
            {
                errorCount[currentPlayer]++;
                if (errorCount[currentPlayer] >= 3)
                {
                    Console.WriteLine($"Játékos {currentPlayer} 3 hibát elkövetett. Játékos {(currentPlayer == 1 ? 2 : 1)} automatikusan nyert!");
                    break;
                }
                continue;
            }

            // Hibátlan szó
            errorCount[currentPlayer] = 0;
            usedWords.Add(w);
            points[currentPlayer] += w.Length;
            lastWord = w;
            currentPlayer = currentPlayer == 1 ? 2 : 1;
            roundCounter++;
            DisplayStats(points, errorCount);
        }
    }

    // ==================== EGYJÁTÉKOS ====================
    static void SinglePlayerMode()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Egyjátékos mód. Te és a gép váltjátok egymást. Nyomj 'R'-t a resethez.");
        Console.ResetColor();

        string lastWord = null;
        int playerPoints = 0;
        int computerPoints = 0;
        int playerErrors = 0;
        roundCounter = 0;

        Console.Write("Kezdeni akarsz? (i/n) [i = te]: ");
        string start = ReadWithRestart()?.Trim().ToLower();
        bool playerTurn = start == "i" || string.IsNullOrEmpty(start);

        while (true)
        {
            if (playerTurn)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(lastWord == null ? "Kezdj egy szóval:" :
                    $"A gép szava: {lastWord}. Te jössz, írj egy szót ami '{GetLastLetter(lastWord)}' betűvel kezdődik (10 mp):");
                Console.ResetColor();

                string input = ReadWithRestart(10000);
                if (input == null) { Console.WriteLine("Idő lejárt! Vesztettél."); break; }

                string w = NormalizeWord(input);

                if (string.IsNullOrEmpty(w) || w.Length < 2)
                    continue; // nem számít hibának

                if (!wordBank.Contains(w) || (lastWord != null && GetFirstLetter(w) != GetLastLetter(lastWord)) || usedWords.Contains(w))
                {
                    playerErrors++;
                    if (playerErrors >= 3)
                    {
                        Console.WriteLine("3 hibát elkövettél. A gép automatikusan nyert!");
                        break;
                    }
                    continue;
                }

                playerErrors = 0;
                usedWords.Add(w);
                lastWord = w;
                playerPoints += w.Length;
                playerTurn = false;
                roundCounter++;
            }
            else
            {
                string choice = ComputerChoose(lastWord == null ? "" : GetLastLetter(lastWord));
                if (choice == null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("A gép nem talál megfelelő szót. Te nyertél!\n");
                    Console.ResetColor();
                    break;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"A gép azt mondja: {choice}");
                Console.ResetColor();

                usedWords.Add(choice);
                lastWord = choice;
                computerPoints += choice.Length;
                playerTurn = true;
                roundCounter++;
            }

            DisplayStats(new int[] { 0, playerPoints, computerPoints }, new int[] { 0, playerErrors, 0 });
        }
    }

    static string ComputerChoose(string startLetter)
    {
        var candidates = wordBank.Where(w => !usedWords.Contains(w)).ToList();
        if (!string.IsNullOrEmpty(startLetter))
            candidates = candidates.Where(w => GetFirstLetter(w) == startLetter).ToList();
        if (candidates.Count == 0) return null;
        return candidates[rnd.Next(candidates.Count)];
    }

    static string NormalizeWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim().ToLowerInvariant();
        var m = Regex.Match(input, "[a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ]+(?:-[a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ]+)?");
        return m.Success ? m.Value : null;
    }

    static string GetFirstLetter(string word)
    {
        string[] digraphs = { "cs", "sz", "ny", "gy", "ty", "ly" };
        foreach (var d in digraphs)
            if (word.StartsWith(d)) return d;
        return word[0].ToString();
    }

    static string GetLastLetter(string word)
    {
        string[] digraphs = { "cs", "sz", "ny", "gy", "ty", "ly" };
        foreach (var d in digraphs)
            if (word.EndsWith(d)) return d;
        return word[word.Length - 1].ToString();
    }

    static void DisplayStats(int[] points, int[] errors)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        string stats = $"Pontok: J1={points[1]}, J2={points[2]}";
        if (errors[1] > 0) stats += $" | Hibák: J1={errors[1]}";
        Console.WriteLine(stats);
        Console.ResetColor();
    }

    static string ReadWithRestart(int timeoutMs = 0)
    {
        string input = null;
        Task t = Task.Run(() => input = Console.ReadLine());
        if (timeoutMs > 0 && !t.Wait(timeoutMs)) return null;
        else t.Wait();

        input = input.Trim();
        if (input.Equals("r", StringComparison.OrdinalIgnoreCase))
            throw new RestartException();

        return input;
    }

    class RestartException : Exception { }
}