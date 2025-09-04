using NullWizard.Core.Models;

namespace NullWizard.Rules;

/// <summary>
/// Null handling rules for popular legacy packages and frameworks
/// </summary>
public static class LegacyPackageRules
{
    /// <summary>
    /// Gets null handling rules for popular legacy packages
    /// </summary>
    /// <param name="packageName">The package name</param>
    /// <returns>List of applicable null handling patterns</returns>
    public static List<NullPattern> GetRulesForPackage(string packageName)
    {
        return packageName.ToLowerInvariant() switch
        {
            "avalonia" => GetAvaloniaRules(),
            "avaloniaui" => GetAvaloniaRules(),
            "newtonsoft.json" => GetNewtonsoftJsonRules(),
            "json.net" => GetNewtonsoftJsonRules(),
            "system.data.sqlclient" => GetSqlClientRules(),
            "microsoft.sqlserver.client" => GetSqlClientRules(),
            "system.net.http" => GetHttpClientRules(),
            "restsharp" => GetRestSharpRules(),
            "nlog" => GetNLogRules(),
            "log4net" => GetLog4NetRules(),
            "autofac" => GetAutofacRules(),
            "castle.windsor" => GetWindsorRules(),
            "nhibernate" => GetNHibernateRules(),
            "entityframework" => GetEntityFrameworkRules(),
            "system.web" => GetSystemWebRules(),
            "system.web.mvc" => GetMvcRules(),
            "system.web.api" => GetWebApiRules(),
            "system.windows.forms" => GetWinFormsRules(),
            "system.windows.presentation" => GetWpfRules(),
            "xamarin.forms" => GetXamarinRules(),
            "mono.android" => GetMonoAndroidRules(),
            "mono.touch" => GetMonoTouchRules(),
            _ => GetDefaultPackageRules()
        };
    }

    private static List<NullPattern> GetAvaloniaRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "avalonia_datacontext",
                Description = "Avalonia DataContext null handling",
                Context = NullContext.PropertySetter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "DataContext = viewModel ?? new DefaultViewModel();",
                FixPattern = "DataContext = viewModel ?? new DefaultViewModel();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "avalonia_binding",
                Description = "Avalonia binding null handling",
                Context = NullContext.PropertySetter,
                Strategy = NullStrategy.NullConditional,
                CodePattern = "Binding = new Binding(\"PropertyName\") { FallbackValue = \"Default\" };",
                FixPattern = "Binding = new Binding(\"PropertyName\") { FallbackValue = \"Default\" };",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "avalonia_control_creation",
                Description = "Avalonia control creation with null safety",
                Context = NullContext.ConstructorParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (parent == null) throw new ArgumentNullException(nameof(parent));",
                FixPattern = "if (parent == null) throw new ArgumentNullException(nameof(parent));",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetNewtonsoftJsonRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "newtonsoft_deserialization",
                Description = "Newtonsoft.Json deserialization with null handling",
                Context = NullContext.JsonDeserialization,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var result = JsonConvert.DeserializeObject<MyType>(json) ?? new MyType();",
                FixPattern = "var result = JsonConvert.DeserializeObject<MyType>(json) ?? new MyType();",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "newtonsoft_jtoken",
                Description = "Newtonsoft.Json JToken null handling",
                Context = NullContext.JsonDeserialization,
                Strategy = NullStrategy.NullConditional,
                CodePattern = "var value = token?[\"property\"]?.ToString() ?? string.Empty;",
                FixPattern = "var value = token?[\"property\"]?.ToString() ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "newtonsoft_serialization",
                Description = "Newtonsoft.Json serialization with null handling",
                Context = NullContext.ReturnValue,
                Strategy = NullStrategy.UseDefault,
                CodePattern = "return JsonConvert.SerializeObject(obj ?? new object());",
                FixPattern = "return JsonConvert.SerializeObject(obj ?? new object());",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetSqlClientRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "sqlclient_reader",
                Description = "SqlClient SqlDataReader null handling",
                Context = NullContext.DatabaseResult,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var value = reader[\"ColumnName\"] as string ?? string.Empty;",
                FixPattern = "var value = reader[\"ColumnName\"] as string ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "sqlclient_connection",
                Description = "SqlClient connection null handling",
                Context = NullContext.ConstructorParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));",
                FixPattern = "if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "sqlclient_command",
                Description = "SqlClient command parameter null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "command.Parameters.AddWithValue(\"@param\", value ?? DBNull.Value);",
                FixPattern = "command.Parameters.AddWithValue(\"@param\", value ?? DBNull.Value);",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetHttpClientRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "httpclient_response",
                Description = "HttpClient response null handling",
                Context = NullContext.ApiResponse,
                Strategy = NullStrategy.NullConditional,
                CodePattern = "var content = response?.Content?.ReadAsStringAsync().Result ?? string.Empty;",
                FixPattern = "var content = response?.Content?.ReadAsStringAsync().Result ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "httpclient_request",
                Description = "HttpClient request null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (url == null) throw new ArgumentNullException(nameof(url));",
                FixPattern = "if (url == null) throw new ArgumentNullException(nameof(url));",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetRestSharpRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "restsharp_response",
                Description = "RestSharp response null handling",
                Context = NullContext.ApiResponse,
                Strategy = NullStrategy.NullConditional,
                CodePattern = "var data = response?.Data ?? new ResponseData();",
                FixPattern = "var data = response?.Data ?? new ResponseData();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "restsharp_request",
                Description = "RestSharp request parameter null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "request.AddParameter(\"param\", value ?? string.Empty);",
                FixPattern = "request.AddParameter(\"param\", value ?? string.Empty);",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetNLogRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "nlog_logger",
                Description = "NLog logger null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var logger = LogManager.GetCurrentClassLogger() ?? LogManager.GetLogger(\"Default\");",
                FixPattern = "var logger = LogManager.GetCurrentClassLogger() ?? LogManager.GetLogger(\"Default\");",
                OccurrenceCount = 1,
                Confidence = 0.8,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "nlog_message",
                Description = "NLog message null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "logger.Info(message ?? \"No message provided\");",
                FixPattern = "logger.Info(message ?? \"No message provided\");",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetLog4NetRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "log4net_logger",
                Description = "log4net logger null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var logger = LogManager.GetLogger(typeof(MyClass)) ?? LogManager.GetLogger(\"Default\");",
                FixPattern = "var logger = LogManager.GetLogger(typeof(MyClass)) ?? LogManager.GetLogger(\"Default\");",
                OccurrenceCount = 1,
                Confidence = 0.8,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetAutofacRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "autofac_resolve",
                Description = "Autofac container resolve null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var service = container.Resolve<IService>() ?? new DefaultService();",
                FixPattern = "var service = container.Resolve<IService>() ?? new DefaultService();",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetWindsorRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "windsor_resolve",
                Description = "Castle Windsor container resolve null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var service = container.Resolve<IService>() ?? new DefaultService();",
                FixPattern = "var service = container.Resolve<IService>() ?? new DefaultService();",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetNHibernateRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "nhibernate_session",
                Description = "NHibernate session null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (session == null) throw new ArgumentNullException(nameof(session));",
                FixPattern = "if (session == null) throw new ArgumentNullException(nameof(session));",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "nhibernate_query",
                Description = "NHibernate query result null handling",
                Context = NullContext.DatabaseResult,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var result = session.Query<MyEntity>().FirstOrDefault() ?? new MyEntity();",
                FixPattern = "var result = session.Query<MyEntity>().FirstOrDefault() ?? new MyEntity();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetEntityFrameworkRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "ef_context",
                Description = "Entity Framework context null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (context == null) throw new ArgumentNullException(nameof(context));",
                FixPattern = "if (context == null) throw new ArgumentNullException(nameof(context));",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "ef_query",
                Description = "Entity Framework query result null handling",
                Context = NullContext.DatabaseResult,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var entity = context.Entities.FirstOrDefault() ?? new Entity();",
                FixPattern = "var entity = context.Entities.FirstOrDefault() ?? new Entity();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetSystemWebRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "systemweb_request",
                Description = "System.Web request null handling",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var value = Request[\"param\"] ?? string.Empty;",
                FixPattern = "var value = Request[\"param\"] ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "systemweb_session",
                Description = "System.Web session null handling",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var value = Session[\"key\"] as string ?? string.Empty;",
                FixPattern = "var value = Session[\"key\"] as string ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetMvcRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "mvc_model",
                Description = "ASP.NET MVC model null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var model = model ?? new DefaultModel();",
                FixPattern = "var model = model ?? new DefaultModel();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            },
            new()
            {
                PatternId = "mvc_viewdata",
                Description = "ASP.NET MVC ViewData null handling",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var data = ViewData[\"key\"] ?? string.Empty;",
                FixPattern = "var data = ViewData[\"key\"] ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetWebApiRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "webapi_actionresult",
                Description = "ASP.NET Web API ActionResult null handling",
                Context = NullContext.ReturnValue,
                Strategy = NullStrategy.UseDefault,
                CodePattern = "return Ok(result ?? new object());",
                FixPattern = "return Ok(result ?? new object());",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetWinFormsRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "winforms_control",
                Description = "Windows Forms control null handling",
                Context = NullContext.PropertySetter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "textBox.Text = value ?? string.Empty;",
                FixPattern = "textBox.Text = value ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetWpfRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "wpf_datacontext",
                Description = "WPF DataContext null handling",
                Context = NullContext.PropertySetter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "DataContext = viewModel ?? new DefaultViewModel();",
                FixPattern = "DataContext = viewModel ?? new DefaultViewModel();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetXamarinRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "xamarin_page",
                Description = "Xamarin.Forms page null handling",
                Context = NullContext.PropertySetter,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "Content = content ?? new Label { Text = \"No content\" };",
                FixPattern = "Content = content ?? new Label { Text = \"No content\" };",
                OccurrenceCount = 1,
                Confidence = 0.85,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetMonoAndroidRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "monoandroid_activity",
                Description = "Mono.Android activity null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (bundle == null) throw new ArgumentNullException(nameof(bundle));",
                FixPattern = "if (bundle == null) throw new ArgumentNullException(nameof(bundle));",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetMonoTouchRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "monotouch_viewcontroller",
                Description = "MonoTouch view controller null handling",
                Context = NullContext.MethodParameter,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (nibName == null) throw new ArgumentNullException(nameof(nibName));",
                FixPattern = "if (nibName == null) throw new ArgumentNullException(nameof(nibName));",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }

    private static List<NullPattern> GetDefaultPackageRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "default_package_null_coalescing",
                Description = "Default package null-coalescing pattern",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var result = input ?? defaultValue;",
                FixPattern = "var result = input ?? defaultValue;",
                OccurrenceCount = 1,
                Confidence = 0.8,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }
}
