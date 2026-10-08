using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rickipedia.Api
{
    public sealed class NamedResource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public sealed class Character
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Species { get; set; } = "";
        public string Type { get; set; } = "";
        public string Gender { get; set; } = "";
        public NamedResource Origin { get; set; } = new NamedResource();
        public NamedResource Location { get; set; } = new NamedResource();
        public string Image { get; set; } = "";
        public List<string> Episode { get; set; } = new List<string>();
        public DateTime Created { get; set; }
    }

    public sealed class PageInfo
    {
        public int Count { get; set; }
        public int Pages { get; set; }
    }

    public sealed class CharacterPage
    {
        public static readonly CharacterPage Empty = new CharacterPage();

        public PageInfo Info { get; set; } = new PageInfo();
        public List<Character> Results { get; set; } = new List<Character>();
    }

    /// <summary>The Rick and Morty API (https://rickandmortyapi.com) için küçük istemci.</summary>
    public sealed class RickAndMortyClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://rickandmortyapi.com/api/"),
            Timeout = TimeSpan.FromSeconds(20),
        };

        private readonly ConcurrentDictionary<string, Task<Image>> _images = new ConcurrentDictionary<string, Task<Image>>();

        /// <summary>Filtrelere uyan karakterlerin ilk sayfası (20 kayıt). Sonuç yoksa API 404 döner; boş sayfa olarak ele alınır.</summary>
        public async Task<CharacterPage> GetCharactersAsync(string name, string status, string species, string gender, CancellationToken cancellationToken)
        {
            var query = new List<string>();
            void Add(string key, string value)
            {
                if (!string.IsNullOrWhiteSpace(value)) query.Add(key + "=" + Uri.EscapeDataString(value.Trim()));
            }
            Add("name", name);
            Add("status", status);
            Add("species", species);
            Add("gender", gender);

            var url = "character/" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            using (var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return CharacterPage.Empty;
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<CharacterPage>(json, JsonOptions) ?? CharacterPage.Empty;
            }
        }

        /// <summary>
        /// Avatarı indirir; aynı adres için tek indirme yapılır. Geçici hatalarda (API hız sınırı gibi) iki kez daha
        /// denenir; yine başarısız olursa önbellekten çıkarılır, böylece sonraki istek yeniden dener.
        /// </summary>
        public async Task<Image> GetImageAsync(string url)
        {
            var task = _images.GetOrAdd(url, DownloadAsync);
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                _images.TryRemove(url, out _);
                throw;
            }
        }

        private async Task<Image> DownloadAsync(string url)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    var bytes = await _http.GetByteArrayAsync(url).ConfigureAwait(false);
                    using (var stream = new MemoryStream(bytes))
                    using (var image = Image.FromStream(stream))
                        return new Bitmap(image); // akıştan bağımsız kopya
                }
                catch (HttpRequestException) when (attempt < 3)
                {
                    await Task.Delay(400 * attempt).ConfigureAwait(false);
                }
            }
        }

        public int LoadedImageCount => _images.Values.Count(t => t.Status == TaskStatus.RanToCompletion);
    }
}
