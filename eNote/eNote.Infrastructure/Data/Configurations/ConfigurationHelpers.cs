using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public static class ConfigurationHelpers
    {
        public static PropertyBuilder<string> HasStringConfig(this PropertyBuilder<string> propertyBuilder, int? maxLength = null, bool isRequired = false)
        {
            if (isRequired)
                propertyBuilder.IsRequired();
            if (maxLength.HasValue)
                propertyBuilder.HasMaxLength(maxLength.Value);
            return propertyBuilder;
        }

        public static PropertyBuilder<decimal> HasDecimalConfig(this PropertyBuilder<decimal> propertyBuilder, int precision = 8, int scale = 2)
        {
            return propertyBuilder.HasColumnType($"decimal({precision},{scale})");
        }

        public static PropertyBuilder<decimal> HasDecimalPrecision(this PropertyBuilder<decimal> propertyBuilder, int precision = 18, int scale = 2)
        {
            return propertyBuilder.HasPrecision(precision, scale);
        }

        public static PropertyBuilder<bool> HasDefaultFalse(this PropertyBuilder<bool> propertyBuilder)
        {
            return propertyBuilder.HasDefaultValue(false);
        }

        public static PropertyBuilder<DateTime> HasDefaultSqlNow(this PropertyBuilder<DateTime> propertyBuilder)
        {
            return propertyBuilder.HasDefaultValueSql("GETUTCDATE()");
        }

        public static IndexBuilder HasUniqueIndex(this EntityTypeBuilder builder, string propertyName)
        {
            return builder.HasIndex(propertyName).IsUnique();
        }

        public static IndexBuilder HasUniqueIndex(this EntityTypeBuilder builder, params string[] propertyNames)
        {
            return builder.HasIndex(propertyNames).IsUnique();
        }

        public static PropertyBuilder<T> HasEnumConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : struct, Enum
        {
            return propertyBuilder.HasConversion<int>();
        }
    }
}
