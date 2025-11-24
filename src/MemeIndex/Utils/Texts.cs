namespace MemeIndex.Utils;

public static class Texts
{
    public const string HELP =
        """
        USAGE:
           MemeIndex
              Run in normal mode (web server).
           MemeIndex [OPTIONS]...
              Run in other modes.
        OPTIONS:
           -t  --test                  Execute whatever is in the test method.
           -d  --demo    IMAGE-PATH... Analyze images, print result to console, save HSL profile.
           -D  --demo-list     FILE    Same as above, image paths are taken from FILE.
           -p  --profile IMAGE-PATH... Save image profiles (all kinds).
           -P  --profile-list  FILE    Same as above, image paths are taken from FILE.
           -?  --help                  Show this screen.
        """;
}