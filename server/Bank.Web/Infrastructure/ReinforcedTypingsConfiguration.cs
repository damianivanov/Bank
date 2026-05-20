using Reinforced.Typings;
using Reinforced.Typings.Fluent;
using System.Reflection;

namespace Bank.Web.Infrastructure;

public static class ReinforcedTypingsConfiguration
{
    public static void Configure(Reinforced.Typings.Fluent.ConfigurationBuilder builder)
    {
        builder.Global(config =>
        {
            config.ExportPureTypings(false);
            config.UseModules(useModules: true, discardNamespaces: false);
            config.CamelCaseForProperties(true);
        });

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var coreAssembly = loadedAssemblies.FirstOrDefault(assembly => "Bank.Core".Equals(assembly.GetName()?.Name));
        var dbAssembly = loadedAssemblies.FirstOrDefault(assembly => "Bank.DB".Equals(assembly.GetName()?.Name));
        var webAssembly = loadedAssemblies.FirstOrDefault(assembly => "Bank.Web".Equals(assembly.GetName()?.Name));

        TryLookupDocumentation(builder, coreAssembly, "Bank.Core.xml");
        TryLookupDocumentation(builder, dbAssembly, "Bank.DB.xml");
        TryLookupDocumentation(builder, webAssembly, "Bank.Web.xml");

        const string modelNamespace = "Bank.Core";
        var modelsToExport = coreAssembly == null
            ? []
            : coreAssembly
                .GetExportedTypes()
                .Where(type =>
                    (type.Namespace?.StartsWith($"{modelNamespace}.JsonModels") ?? false)
                    || (type.Namespace?.StartsWith($"{modelNamespace}.Common") ?? false))
                .ToList();

        const string entityNamespace = "Bank.DB.Entities";
        var entitiesToScan = dbAssembly == null
            ? []
            : dbAssembly
                .GetExportedTypes()
                .Where(type => type.Namespace?.StartsWith(entityNamespace) ?? false)
                .ToList();

        var enums = modelsToExport
            .Where(type => type.IsEnum)
            .Concat(modelsToExport.SelectMany(model =>
                model.GetProperties().SelectMany(property => GetEnumTypes(property.PropertyType))))
            .Concat(entitiesToScan.SelectMany(entity =>
                entity.GetProperties().SelectMany(property => GetEnumTypes(property.PropertyType))))
            .DistinctBy(type => type.FullName)
            .ToList();

        foreach (var enumType in enums)
        {
            builder.ExportAsEnums([enumType], config => config.OverrideNamespace("Enums"));
        }

        var nullabilityInfoContext = new NullabilityInfoContext();
        var dateTimeType = typeof(DateTime);
        var dateOnlyType = typeof(DateOnly);

        foreach (var model in modelsToExport.Where(model => !model.IsEnum))
        {
            builder.ExportAsInterfaces([model], config =>
            {
                config.AutoI(false);

                if (model.Name.StartsWith("CommonJsonModel"))
                {
                    config.DontIncludeToNamespace();
                    config.OverrideName("JsonData");
                }

                config.OverrideNamespace(model.Namespace?.Replace(modelNamespace + ".", string.Empty) ?? string.Empty);
                config.WithPublicProperties(propertyConfig =>
                {
                    var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyConfig.Member.PropertyType);
                    var nullabilityInfo = nullabilityInfoContext.Create(propertyConfig.Member);

                    if (nullabilityInfo.WriteState == NullabilityState.Nullable
                        || nullabilityInfo.ReadState == NullabilityState.Nullable
                        || nullableUnderlyingType != null)
                    {
                        propertyConfig.ForceNullable(true);
                    }

                    if (propertyConfig.Member.PropertyType == dateTimeType
                        || nullableUnderlyingType == dateTimeType
                        || propertyConfig.Member.PropertyType == dateOnlyType
                        || nullableUnderlyingType == dateOnlyType)
                    {
                        propertyConfig.Type<string>();
                    }
                });
            });
        }
    }

    private static void TryLookupDocumentation(
        Reinforced.Typings.Fluent.ConfigurationBuilder builder,
        Assembly? assembly,
        string documentationFileName)
    {
        if (assembly != null)
        {
            builder.TryLookupDocumentationForAssembly(assembly, documentationFileName);
        }
    }

    private static IEnumerable<Type> GetEnumTypes(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            type = nullableType;
        }

        if (type.IsEnum)
        {
            yield return type;
            yield break;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType == null)
            {
                yield break;
            }

            foreach (var enumType in GetEnumTypes(elementType))
            {
                yield return enumType;
            }

            yield break;
        }

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var enumType in GetEnumTypes(argument))
            {
                yield return enumType;
            }
        }
    }
}
