using Microsoft.Data.Sqlite;
using System;

using System.IO;

public class ProfileDatabase
{
    private const string profilesFileName = "profiles.db";
    private static string profilesFilePath;

    static ProfileDatabase()
    {
        profilesFilePath = GetProfilesFilePath();
    }

    private static string GetProfilesFilePath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profilesDirectoryPath = Path.Combine(appDataPath, "VkLogs", "profiles");
        string profilesFilePath = Path.Combine(profilesDirectoryPath, profilesFileName);

        try
        {
            if (!Directory.Exists(profilesDirectoryPath))
            {
                Directory.CreateDirectory(profilesDirectoryPath);
            }

            if (!File.Exists(profilesFilePath))
            {
                using (var connection = new SqliteConnection($"Data Source={profilesFilePath}"))
                {
                    connection.Open();
                    using (var command = new SqliteCommand("CREATE TABLE Profiles (ProfileID TEXT PRIMARY KEY);", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при создании файла профилей: {ex.Message}");
        }

        return profilesFilePath;
    }


    public static void AddProfile(string profileId)
    {
        using (var connection = new SqliteConnection($"Data Source={profilesFilePath}"))
        {
            connection.Open();
            using (var command = new SqliteCommand($"INSERT INTO Profiles (ProfileID) VALUES('{profileId}');", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public static bool IsProfileUse(string profileId)
    {
        using (var connection = new SqliteConnection($"Data Source={profilesFilePath}"))
        {
            connection.Open();
            using (var command = new SqliteCommand($"SELECT COUNT(*) FROM Profiles WHERE ProfileID='{profileId}';", connection))
            {
                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }

    public static void RemoveProfile(string profileId)
    {
        using (var connection = new SqliteConnection($"Data Source={profilesFilePath}"))
        {
            connection.Open();
            using (var command = new SqliteCommand($"DELETE FROM Profiles WHERE ProfileID='{profileId}';", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
