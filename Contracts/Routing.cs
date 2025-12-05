using System;
using System.Collections.Generic;
using System.Text;

namespace Contract
{
    public static class Routing
    {
        public static class Menu
        {
            public const string Weekly = "api/menu/week";
            public const string Rename = "api/menu/rename";
            public const string Autocomplete = "api/menu/autocomplete";
        }

        public static class Ratings
        {
            public const string Top = "api/ratings/top";
            public const string Rate = "api/ratings/rate";
            public const string Get = "api/ratings/get";
        }
    }
}
