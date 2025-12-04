using System;
using System.Collections.Generic;
using System.Text;

namespace Contract.Model.Requests
{
    public record RenameMenuRequest(int DailyMealId, string NewDescription);

    public record RateMealRequest(int DailyMealId, int Stars);
}
