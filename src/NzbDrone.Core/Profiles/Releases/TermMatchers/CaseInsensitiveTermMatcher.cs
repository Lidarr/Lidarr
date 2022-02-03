namespace NzbDrone.Core.Profiles.Releases.TermMatchers;

public sealed class CaseInsensitiveTermMatcher : ITermMatcher
{
    private readonly string _term;

    public CaseInsensitiveTermMatcher(string term)
    {
        _term = term.ToLowerInvariant();
    }

    public bool IsMatch(string value)
    {
        return value.ToLowerInvariant().Contains(_term);
    }

    public string MatchingTerm(string value)
    {
        if (value.ToLowerInvariant().Contains(_term))
        {
            return _term;
        }

        return null;
    }
}
