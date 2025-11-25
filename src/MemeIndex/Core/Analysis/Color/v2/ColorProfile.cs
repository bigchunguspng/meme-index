using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Analysis.Color.v2;

/// Oklch based color profile.
public static class ColorProfile
{
    // HUE

    /// 0 = Red, 1 = Orange, …
    public static int GetHueIndex(double oklch_H)
    {
        if (double.IsNaN(oklch_H)) return 0;

        var hue = (oklch_H.RoundInt() - OFF + 360) % 360; // Red = 0°
        for (var i = 0; i < N_of_HUES - 1; i++)
        {
            if (hue < _hue_borders[i]) return i;
        }

        return N_of_HUES - 1;
    }

    private const int
        OFF       = 15, // Red color offset, degrees.
        N_of_HUES = 11;

    // Hue borders in degrees. Values are shifted so Red starts at 0°.
    private static readonly int[] _hue_borders =
    [
        035 - OFF,  75 - OFF, 115 - OFF, 135 - OFF,  // Red     Orange  Yellow  Lime
        160 - OFF, 225 - OFF, 255 - OFF, 275 - OFF,  // Green   Cyan    Sky     Blue
        310 - OFF, 340 - OFF, 360,                   // Violet  Magenta Pink
    ];

    // BUCKETS

    public static bool IsSaturated(Oklch oklch)
    {
        return oklch.C > 0.02;
    }

    /// 0..3 - pale dark, pale light, saturated dark, saturated light.
    public static int GetBucketIndex_Saturated(Oklch oklch)
    {
        var saturated = oklch.C > 0.1 ? 2 : 0; // pale
        var light     = oklch.L > 0.5 ? 1 : 0; // dark
        return light | saturated;
    }

    /// 0..4 - black, 3 grays, white.
    public static int GetBucketIndex_Grayscale(Oklch oklch)
    {
        var ryuzaki = (oklch.L * 100.0).RoundInt();
        for (var i = 0; i < N_of_SHADES - 1; i++)
        {
            if (ryuzaki < _grayscale_borders[i]) return i;
        }

        return N_of_SHADES - 1;
    }

    private const int
        N_of_SHADES = 5;

    private static readonly int[] _grayscale_borders =
    [
        15, 30, 60, 90, // Black, 3 x Gray, White
    ];

    // PALETTE

    public static Rgb24 GetColor_Grayscale(int bucket) => bucket switch
    {
        0 => new Oklch(0.00, 0, 0).ToRgb24(),
        1 => new Oklch(0.25, 0, 0).ToRgb24(),
        2 => new Oklch(0.45, 0, 0).ToRgb24(),
        3 => new Oklch(0.75, 0, 0).ToRgb24(),
        4 => new Oklch(1.00, 0, 0).ToRgb24(),
    };

    public static Rgb24 GetColor_Saturated(int hue_ix, int bucket) => bucket switch
    {
        0 => new Oklch(0.35, 0.10, (_hue_borders[hue_ix] + _hue_borders[(hue_ix + 1) % N_of_HUES]) / 2.0).ToRgb24(),
        1 => new Oklch(0.75, 0.10, (_hue_borders[hue_ix] + _hue_borders[(hue_ix + 1) % N_of_HUES]) / 2.0).ToRgb24(),
        _ => Rgba32.TryParseHex(_hex[2 * hue_ix + bucket % 2], out var c) ? c.Rgb : default, // saturated
    };

    private static readonly string[] _hex =
    [
        "880a00", "fc1447", "851e00", "f85900", // R,O
        "646400", "f1d400", "5f7000", "daf500", // Y,L
        "164300", "7de700", "007f65", "00f4ae", // G,C
        "0057a0", "00a6e8", "0010b7", "7891fc", // S,B
        "570086", "a084fc", "640069", "f64afc", // V,M
        "950043", "fc7aa6",                       // P
    ];
    public const string // Generator: https://patorjk.com/software/taag, Font: Letter.
        PROFILE_TEXT_X1 =
            """
            ####  ##### ####  
            #   # #     #   # 
            ####  ####  #   # 
            #  #  #     #   # 
            #   # ##### ####  
            """,
        PROFILE_TEXT_X1B =
            """
            #   # ##### #     #      ###  #   # 
             # #  #     #     #     #   # #   # 
              #   ####  #     #     #   # # # # 
              #   #     #     #     #   # # # # 
              #   ##### ##### #####  ###   # #  
            """,
        PROFILE_TEXT_X2 =
            """
             ###  ####    #   #   #  ###  ##### 
            #   # #   #  # #  ##  # #     #     
            #   # ####  ##### # # # # ##  ####  
            #   # #  #  #   # #  ## #   # #     
             ###  #   # #   # #   #  ###  ##### 
            """,
        PROFILE_TEXT_X2B =
            """
            #     ### #   # ##### 
            #      #  ## ## #     
            #      #  # # # ####  
            #      #  #   # #     
            ##### ### #   # ##### 
            """,
        PROFILE_TEXT_X3 =
            """
             ###  ####  ##### ##### #   # 
            #     #   # #     #     ##  # 
            # ##  ####  ####  ####  # # # 
            #   # #  #  #     #     #  ## 
             ###  #   # ##### ##### #   # 
            """,
        PROFILE_TEXT_X3B =
            """
             #### #   # #   # 
            #     #  #   # #  
             ###  ###     #   
                # #  #    #   
            ####  #   #   #   
            """,
        PROFILE_TEXT_X4 =
            """
             ###  #   #   #   #   # 
            #   #  # #   # #  ##  # 
            #       #   ##### # # # 
            #   #   #   #   # #  ## 
             ###    #   #   # #   # 
            """,
        PROFILE_TEXT_X4B =
            """
            ####  #     #   # ##### 
            #   # #     #   # #     
            ####  #     #   # ####  
            #   # #     #   # #     
            ####  #####  ###  ##### 
            """,
        PROFILE_TEXT_X5 =
            """
            #   # ###  ###  #     ##### ##### 
            #   #  #  #   # #     #       #   
            #   #  #  #   # #     ####    #   
             # #   #  #   # #     #       #   
              #   ###  ###  ##### #####   #   
            """,
        PROFILE_TEXT_X5B =
            """
            ####  ### #   # #   # 
            #   #  #  ##  # #  #  
            ####   #  # # # ###   
            #      #  #  ## #  #  
            #     ### #   # #   # 
            """,
        PROFILE_TEXT_X6 =
            """
            #   #   #    ###  ##### #   # #####   #   
            ## ##  # #  #     #     ##  #   #    # #  
            # # # ##### # ##  ####  # # #   #   ##### 
            #   # #   # #   # #     #  ##   #   #   # 
            #   # #   #  ###  ##### #   #   #   #   # 
            """;
}