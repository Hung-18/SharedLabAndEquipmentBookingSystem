using System;
using System.Linq;

namespace Infrastructure.Configurations
{
    internal static class ConfigurationHelpers
    {
        internal static string EnumCheck<TEnum>(string columnName)
            where TEnum : struct, Enum
        {
            var values = string.Join(", ", Enum.GetNames<TEnum>().Select(x => $"'{x}'"));
            return $"[{columnName}] IN ({values})";
        }

        internal const string BookingItemResourceCheck = @"
(
    ([ResourceType] IN ('LabRoom', 'Lab') AND [LabId] IS NOT NULL AND [EquipmentId] IS NULL)
    OR
    ([ResourceType] = 'Equipment' AND [LabId] IS NULL AND [EquipmentId] IS NOT NULL)
)";

        internal const string SingleResourceCheck = @"
(
    ([LabId] IS NOT NULL AND [EquipmentId] IS NULL)
    OR
    ([LabId] IS NULL AND [EquipmentId] IS NOT NULL)
)";
    }
}
