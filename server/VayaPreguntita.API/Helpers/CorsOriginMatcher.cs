using System.Text.RegularExpressions;

namespace VayaPreguntita.API.Helpers;

// Decides which browser origins may call the API: the exact `AllowedOrigins` (production) plus
// `AllowedOriginPatterns`, where each `*` stands for one run of letters, digits and dashes. That
// is enough for Vercel preview URLs (preguntitas-git-<branch>-<team>.vercel.app) and can never
// match a dot, a slash or a port, so a pattern pinned to our own team suffix can't be spoofed by
// another domain.
public sealed class CorsOriginMatcher
{
    private readonly HashSet<string> _exact;
    private readonly Regex[] _patterns;

    public CorsOriginMatcher(IEnumerable<string>? exactOrigins, IEnumerable<string>? patterns)
    {
        _exact = new HashSet<string>(
            (exactOrigins ?? []).Select(Normalize).Where(o => o.Length > 0),
            StringComparer.OrdinalIgnoreCase
        );
        _patterns = (patterns ?? [])
            .Select(Normalize)
            .Where(p => p.Length > 0)
            .Select(p => new Regex(
                "^" + string.Join("[a-z0-9-]+", p.Split('*').Select(Regex.Escape)) + "$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(50)
            ))
            .ToArray();
    }

    public bool HasAny => _exact.Count > 0 || _patterns.Length > 0;

    public bool IsAllowed(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;
        var o = Normalize(origin);
        return _exact.Contains(o) || _patterns.Any(p => p.IsMatch(o));
    }

    private static string Normalize(string value) => value.Trim().TrimEnd('/');
}
