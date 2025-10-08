namespace LemmikkiAPI;

using System.Data;
using Microsoft.Data.Sqlite;
public record OmistajaJaLemmikki (string oNimi, string puhelinnumero, string lNimi, string laji);
public record Puhelinpaivitys (string puhelinnumero);

public class LemmikkiDB
{
    private string _connectionString = "Data Source = lemmikki.db";

    public LemmikkiDB()
    {
        //Luodaan yhteys tietokantaan.
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            //Luodaan taulut, jos niitä ei vielä ole

            //Taulu Omistajia sarakkeet id, nimi, puhelin
            var commandForOwnerTableCreation = connection.CreateCommand();
            commandForOwnerTableCreation.CommandText = "CREATE TABLE IF NOT EXISTS Omistajat (id INTEGER PRIMARY KEY, nimi TEXT, puhelinnumero TEXT)";
            commandForOwnerTableCreation.ExecuteNonQuery();

            //Taulu Lemmikit sarakkeet id, nimi, laji, omistajan_id
            var commandForPetTableCreation = connection.CreateCommand();
            commandForPetTableCreation.CommandText = "CREATE TABLE IF NOT EXISTS Lemmikit (id INTEGER PRIMARY KEY, nimi TEXT, laji TEXT, omistajan_id INTEGER)";
            commandForPetTableCreation.ExecuteNonQuery();
        }
    }

    public void LisaaOmistaja(string oNimi, string puhelinnumero)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            //Tarkistetaan onko tuote jo tietokannassa
            var commandForCheck = connection.CreateCommand();
            commandForCheck.CommandText = "SELECT id From Omistajat WHERE nimi = @Nimi";
            commandForCheck.Parameters.AddWithValue("Nimi", oNimi);
            object? id = commandForCheck.ExecuteScalar();

            if (id != null)
            {
                Console.WriteLine("Omistaja on jo tietokannassa.");
                return;
            }

            //Lisätään Omistaja ja lemmikki tietokantaan
            var commandForInsertOwner = connection.CreateCommand();
            commandForInsertOwner.CommandText = "INSERT INTO Omistajat (nimi, puhelinnumero) VALUES (@Nimi, @Puhelinnumero)";
            commandForInsertOwner.Parameters.AddWithValue("Nimi", oNimi);
            commandForInsertOwner.Parameters.AddWithValue("Puhelinnumero", puhelinnumero);
            commandForInsertOwner.ExecuteNonQuery();

            // get the id of the newly inserted owner. this part was causing me issues.
            var commandForGetId = connection.CreateCommand();
            commandForGetId.CommandText = "SELECT last_insert_rowid()";
            long ownerId = (long)(commandForGetId.ExecuteScalar()!);
        }
    }

    public void LisaaLemmikki(string lNimi, string laji, string oNimi)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var getOwnerIdCommand = connection.CreateCommand();
            getOwnerIdCommand.CommandText = "SELECT id FROM Omistajat WHERE nimi = $OmistajanNimi";
            getOwnerIdCommand.Parameters.AddWithValue("OmistajanNimi", oNimi);

            var result = getOwnerIdCommand.ExecuteScalar();
            int ownerId = Convert.ToInt32(result);

            using (var transaction = connection.BeginTransaction())
            {
                var insertPetCommand = connection.CreateCommand();
                insertPetCommand.CommandText = "INSERT INTO Lemmikit (nimi, laji, omistajan_id) VALUES ($Nimi, $Laji, $Omistajan_id)";
                insertPetCommand.Parameters.AddWithValue("Nimi", lNimi);
                insertPetCommand.Parameters.AddWithValue("Laji", laji);
                insertPetCommand.Parameters.AddWithValue("Omistajan_id", ownerId);
                insertPetCommand.ExecuteNonQuery();

                transaction.Commit();
            }
        }

    }

    public List<OmistajaJaLemmikki> GetOmistajaJaLemmikki()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var selectOwnersCmd = connection.CreateCommand();
            selectOwnersCmd.CommandText = @"
            SELECT Omistajat.nimi, Omistajat.puhelinnumero, Lemmikit.nimi, Lemmikit.laji
            FROM Omistajat
            JOIN Lemmikit ON Lemmikit.omistajan_id = Omistajat.id";

            using (var reader = selectOwnersCmd.ExecuteReader())
            {
                List<OmistajaJaLemmikki> omistajaJaLemmikkit = new List<OmistajaJaLemmikki>();

                while (reader.Read())
                {
                    omistajaJaLemmikkit.Add(new OmistajaJaLemmikki(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                }
                return omistajaJaLemmikkit;
            }
        }
    }

    public void PaivitaPuhelinnumero(string nimi, string puhelinnumero)
    {
        //
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            //Tarkistetaan onko tuote jo tietokannassa
            var commandForCheck = connection.CreateCommand();
            commandForCheck.CommandText = "SELECT id From Omistajat WHERE nimi = @Nimi";
            commandForCheck.Parameters.AddWithValue("Nimi", nimi);
            object? id = commandForCheck.ExecuteScalar();

            if (id != null)
            {
                //Päivitetään omistajan puhelinnumero
                var commandForUpdate = connection.CreateCommand();
                commandForUpdate.CommandText = "UPDATE Omistajat SET puhelinnumero = @Puhelinnumero WHERE id = @Id";
                commandForUpdate.Parameters.AddWithValue("Puhelinnumero", puhelinnumero);
                commandForUpdate.Parameters.AddWithValue("Id", (long)id);
                commandForUpdate.ExecuteNonQuery();
                return;
            }
            Console.WriteLine("Tämä henkilö ei ole tietokannassa.");
        }
    }

    public string EtsiOmistajanPuhelinnumero(string haettavanNimi)
    {
        //Luodaan yhteys tietokantaan
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var commandForSearch = connection.CreateCommand();
            commandForSearch.CommandText = "SELECT o.nimi, o.puhelinnumero From Omistajat o JOIN Lemmikit l ON o.id = l.omistajan_id WHERE l.nimi = @Nimi";
            commandForSearch.Parameters.AddWithValue("Nimi", haettavanNimi);
            using (var reader = commandForSearch.ExecuteReader())
            {
                string puhelinnumero = "";

                while (reader.Read())
                {
                    puhelinnumero += $"Omistajan nimi: {reader.GetString(0)}, Puhelinnumero: {reader.GetString(1)}";
                }

                if (puhelinnumero == "")
                {
                    return "puhelinnumeroa ei löytynyt.";
                }
                return puhelinnumero;
            }
        }
    }

    public Dictionary<string, string> HaeOmistaja()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Haetaan omistaja ja lemmikki tietokannasta.
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT nimi, puhelinnumero FROM Omistajat";

            var omistajat = new Dictionary<string, string>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    omistajat.Add(reader.GetString(0), reader.GetString(1));
                }
            }
            return omistajat;
        }
    }

    public Dictionary<string, string> HaeLemmikki()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Haetaan omistaja ja lemmikki tietokannasta.
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT nimi, laji FROM Lemmikit";

            var lemmikit = new Dictionary<string, string>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lemmikit.Add(reader.GetString(0), reader.GetString(1));
                }
            }
            return lemmikit;
        }
    }
}