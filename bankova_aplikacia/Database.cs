using Microsoft.Data.Sqlite;

namespace bankova_aplikacia
{
    class Database
    {
        private static string connectionString = "Data Source=banka.db";

        public static void Init()
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = @"CREATE TABLE IF NOT EXISTS Pouzivatelia (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Meno TEXT NOT NULL,
                    Gmail TEXT NOT NULL UNIQUE,
                    Heslo TEXT NOT NULL
                )";
                new SqliteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        public static bool Registruj(string meno, string gmail, string heslo)
        {
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO Pouzivatelia (Meno, Gmail, Heslo) VALUES (@meno, @gmail, @heslo)";
                    var cmd = new SqliteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@meno", meno);
                    cmd.Parameters.AddWithValue("@gmail", gmail);
                    cmd.Parameters.AddWithValue("@heslo", heslo);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Prihlas(string gmail, string heslo)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM Pouzivatelia WHERE Gmail=@gmail AND Heslo=@heslo";
                var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@gmail", gmail);
                cmd.Parameters.AddWithValue("@heslo", heslo);
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }
    }
}