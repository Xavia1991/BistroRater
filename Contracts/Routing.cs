using System;
using System.Collections.Generic;
using System.Text;

namespace Contract
{
    public static class Routing
    {
        public static class Menu
        {
            public const string Top = "GetTopMenus";
            public const string Weekly = "GetWeeklyMenus";
            public const string Rename = "RenameMenu";
            public const string Autocomplete = "AutocompleteMenus";
        }

        public static class Ratings
        {
            public const string Rate = "RateMeal";
            public const string Get = "GetRatings";
        }
    }
}
