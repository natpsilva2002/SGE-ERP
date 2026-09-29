namespace SGE.API.Services;

public static class PostgresConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var explicitEnvironmentConnection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
        if (!string.IsNullOrWhiteSpace(explicitEnvironmentConnection))
            return explicitEnvironmentConnection;

        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl) && Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            var credentials = uri.UserInfo.Split(':', 2);
            return new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = Uri.UnescapeDataString(credentials[0]),
                Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                SslMode = Npgsql.SslMode.Prefer
            }.ConnectionString;
        }

        var host = configuration["PGHOST"];
        var user = configuration["PGUSER"];
        var password = configuration["PGPASSWORD"];
        var database = configuration["PGDATABASE"];
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(user) &&
            !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(database))
        {
            return new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(configuration["PGPORT"], out var pgPort) ? pgPort : 5432,
                Username = user,
                Password = password,
                Database = database,
                SslMode = Npgsql.SslMode.Prefer
            }.ConnectionString;
        }

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection, DATABASE_URL ou as variaveis PostgreSQL PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE.");
    }
}
