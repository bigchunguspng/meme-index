namespace MemeIndex.Core.Analysis.Color.v2;

/// Oklch based color profile.
public static class ColorProfile
{
    /// 0 = Red, 1 = Orange, …
    public static int GetHueIndex(double oklch_H)
    {
        if (double.IsNaN(oklch_H)) return 0;

        var hue = (oklch_H.RoundInt() - OFF + 360) % 360; // Red = 0°
        for (var i = 0; i < N_of_HUES; i++)
        {
            if (hue < _hue_borders[i + 1]) return i;
        }

        return 0;
    }

    private const int
        OFF       = 15, // Red color offset, degrees.
        N_of_HUES = 11;

    // Hue borders in degrees. Values are shifted so Red starts at 0°.
    private static readonly int[] _hue_borders =
    [
        OFF - OFF,  35 - OFF,  75 - OFF, 115 - OFF,  // Red     Orange  Yellow  Lime
        135 - OFF, 160 - OFF, 225 - OFF, 255 - OFF,  // Green   Cyan    Sky     Blue
        275 - OFF, 310 - OFF, 340 - OFF,       360,  // Violet  Magenta Pink
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