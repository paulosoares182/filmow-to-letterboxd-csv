using AngleSharp;
using AngleSharp.Dom;
using System.Text.RegularExpressions;

namespace FilmowToLetterboxdCSV
{
    internal static partial class Helper
    {
        internal async static Task<IEnumerable<(string links, string rating)>> ExtractMoviesAndRating(this IBrowsingContext context, string user)
        {
            if (string.IsNullOrWhiteSpace(user)) return [];
            
            int page = 1;
            IHtmlCollection<IElement>? items;

            List<(string link, string rating)> result = [];

            do
            {
                Console.WriteLine($"\nObtendo filmes e notas da página {page}\n");

                var document = await context.OpenAsync($"https://filmow.com/usuario/{user}/filmes/ja-vi/?order=older&pagina={page}");

                items = document.QuerySelectorAll("ul#movies-list > li.movie_list_item > span > a[data-movie-pk]:nth-child(1)");
                var ratings = document.QuerySelectorAll("div.user-rating");

                for (int i = 0; i < items.Length; i++)
                {
                    var title = ratings[i].QuerySelector("span.stars")?.GetAttribute("title") ?? string.Empty;
                    var match = RegexRating().Match(title);
                    var rating = match.Success ? match.Groups[1].Value : string.Empty;
                    var href = items[i].GetAttribute("href")!;

                    Console.WriteLine($"[Nota {rating}]  \t{href}");

                    result.Add((href, rating));
                }

                page++;

                Console.WriteLine($"\nTotal: {items.Length}");

            } while (items?.Length > 0);

            return result;
        }

        internal async static Task<IEnumerable<Movie>> ExtractMovieDeatils(this IBrowsingContext context, IEnumerable<(string link, string rating)> links)
        {
            List<Movie> result = [];

            foreach (var (link, rating) in links)
            {
                Console.WriteLine($"\n{link}");

                using var document = await context.OpenAsync($"https://filmow.com{link}");

                var title = document.QuerySelectorAll("div.movie-other-titles > ul > li")?
                    .Select(li =>
                    {
                        if (li.QuerySelector("em")?.TextContent == "Estados Unidos da América")
                        {
                            var text = li.QuerySelector("strong")?.TextContent ?? string.Empty;
                            return RegexIsASCII().IsMatch(text) ? text : null;
                        }

                        return null;
                    })
                    .FirstOrDefault(title => !string.IsNullOrEmpty(title));

                if (string.IsNullOrEmpty(title))
                {
                    var text = document.QuerySelector("div.movie-title > div > h2.movie-original-title")?.TextContent ?? string.Empty;
                    title = RegexIsASCII().IsMatch(text) ? text : null;
                }

                if (string.IsNullOrEmpty(title))
                    title = document.QuerySelector("div.movie-title > h1")?.TextContent;

                if (string.IsNullOrEmpty(title)) continue;

                var year = document.QuerySelector("small.release")!.TextContent;

                var director = document.QuerySelector("div.directors > span > a > span[itemprop='name']")?.TextContent ?? string.Empty;

                result.Add(new Movie(title, director, year, rating));

                if (title.Contains(","))
                    title = $"\"{title}\"";

                Console.WriteLine($"{title} ({year}) - {director}");
            }

            return result;
        }

        [GeneratedRegex(@"([\d\.]+) estrelas")]
        private static partial Regex RegexRating();
        [GeneratedRegex(@"[\x00-\x7F]+")]
        private static partial Regex RegexIsASCII();
    }
}
