// Runs the corpus through everything and prints what came out.
//
// It exits non-zero when a claim stops holding, so it is a check and not a
// demonstration: the README quotes these numbers, CI runs this, and a figure
// that stops being true stops the build.
using SupportConsole.Frames;
using SupportConsole.Measure;

Console.WriteLine();
Console.WriteLine($"Eleven frames, drawn. {Corpus.SignIn.Count} of them are sign-in screens.");
Console.WriteLine();

foreach (var one in Corpus.All)
{
    Console.WriteLine($"  {one.Name,-24} {one.Story}");
}

var claims = Claims.All();

foreach (var claim in claims)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine(claim.Holds ? "HOLDS   " + claim.Title : "BROKEN  " + claim.Title);
    Console.WriteLine(new string('=', 78));
    Console.WriteLine();

    foreach (var line in claim.Lines) Console.WriteLine(line.Length == 0 ? string.Empty : "  " + line);
}

Console.WriteLine();

var broken = claims.Where(claim => !claim.Holds).ToList();

if (broken.Count == 0)
{
    Console.WriteLine("All three claims hold.");
    return 0;
}

foreach (var claim in broken) Console.WriteLine($"BROKEN: {claim.Title}");

return 1;
