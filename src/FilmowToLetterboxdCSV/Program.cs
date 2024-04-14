using AngleSharp;
using FilmowToLetterboxdCSV;
using System.Diagnostics;
using System.Text;

Console.WriteLine("Digite o nome do usuário:");
var user = Console.ReadLine()?.Trim();

if (user is null)
    return;

var config = Configuration.Default.WithDefaultLoader();
using var context = BrowsingContext.New(config);

Stopwatch stopwatch = Stopwatch.StartNew();

var links = await context.ExtractMoviesAndRating(user);
var movies = await context.ExtractMovieDeatils(links);

StringBuilder sb = new();
sb.AppendLine("Title,Directors,Year,Rating");

foreach (var movie in movies)
    sb.AppendLine($"{movie.Title},{movie.Director},{movie.Year},{movie.Rating}");

File.WriteAllText("./watched.csv", sb.ToString().TrimEnd('\n'));

Console.WriteLine($"Arquivo salvo em: {Path.Combine(Directory.GetCurrentDirectory(), "watched.csv")}");

stopwatch.Stop();
var total = stopwatch.Elapsed.TotalMinutes > 1 
    ? stopwatch.Elapsed.TotalMinutes 
    : stopwatch.Elapsed.Seconds;

Console.WriteLine($"Tempo Total: {Math.Round(total, 2)}");